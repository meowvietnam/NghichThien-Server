using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MTS.Utils;

namespace MTS.Data;

public sealed class AppDbContextMergeOptions
{
    public bool DryRun { get; init; } = true;
    public bool UseInsertIgnore { get; init; } = true;
    public int CommandTimeoutSeconds { get; init; } = 300;
    public MergeConflictPolicy ConflictPolicy { get; init; } = MergeConflictPolicy.RenameKnownUniqueValues;
    public string SourceSuffix { get; init; } = "B";
    public HashSet<string> SkipTables { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "__EFMigrationsHistory"
    };
}

public enum MergeConflictPolicy
{
    SkipDuplicates,
    RenameKnownUniqueValues
}

public sealed class AppDbContextMergeResult
{
    public List<TableMergeResult> Tables { get; } = new();
    public int SourceRows => Tables.Sum(x => x.SourceRows);
    public int InsertedRows => Tables.Sum(x => x.InsertedRows);
    public int SkippedRows => Tables.Sum(x => x.SkippedRows);
    public int FailedRows => Tables.Sum(x => x.FailedRows);
}

public sealed class TableMergeResult
{
    public string TableName { get; init; } = string.Empty;
    public int SourceRows { get; set; }
    public int InsertedRows { get; set; }
    public int SkippedRows { get; set; }
    public int SkippedDuplicateRows { get; set; }
    public int SkippedMissingParentRows { get; set; }
    public int FailedRows { get; set; }
    public List<string> Errors { get; } = new();
}

public sealed class AppDbContextMerge
{
    private readonly AppDbContext source;
    private readonly AppDbContext target;
    private readonly AppDbContextMergeOptions options;
    private readonly Dictionary<KeyMapId, Dictionary<ValueTupleKey, object?[]>> keyMaps = new();

    public AppDbContextMerge(AppDbContext source, AppDbContext target, AppDbContextMergeOptions? options = null)
    {
        this.source = source;
        this.target = target;
        this.options = options ?? new AppDbContextMergeOptions();
    }

    public static async Task<AppDbContextMergeResult> Merge(
        string sourceConnectionString,
        string targetConnectionString,
        AppDbContextMergeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var source = AppDbContext.FromConnection(sourceConnectionString);
        await using var target = AppDbContext.FromConnection(targetConnectionString);
        return await new AppDbContextMerge(source, target, options).Merge(cancellationToken);
    }

    public static Task<AppDbContextMergeResult> MergeApply(
        string sourceConnectionString,
        string targetConnectionString,
        string sourceSuffix = "B",
        CancellationToken cancellationToken = default)
    {
        return Merge(
            sourceConnectionString,
            targetConnectionString,
            new AppDbContextMergeOptions
            {
                DryRun = false,
                SourceSuffix = sourceSuffix
            },
            cancellationToken);
    }

    public async Task<AppDbContextMergeResult> Merge(CancellationToken cancellationToken = default)
    {
        source.Database.SetCommandTimeout(options.CommandTimeoutSeconds);
        target.Database.SetCommandTimeout(options.CommandTimeoutSeconds);

        await source.Database.OpenConnectionAsync(cancellationToken);
        await target.Database.OpenConnectionAsync(cancellationToken);

        var plans = BuildPlans();
        var result = new AppDbContextMergeResult();

        if (options.DryRun)
        {
            foreach (var plan in plans)
            {
                result.Tables.Add(new TableMergeResult
                {
                    TableName = plan.TableName,
                    SourceRows = await CountRows(source.Database.GetDbConnection(), plan, cancellationToken)
                });
            }

            return result;
        }

        await using var transaction = await target.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var plan in plans)
            {
                result.Tables.Add(await MergeTable(plan, cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    private async Task<TableMergeResult> MergeTable(TablePlan plan, CancellationToken cancellationToken)
    {
        var result = new TableMergeResult { TableName = plan.TableName };
        await using var readCommand = source.Database.GetDbConnection().CreateCommand();
        readCommand.CommandText = $"SELECT {string.Join(", ", plan.Columns.Select(x => Quote(x.ColumnName)))} FROM {Quote(plan.TableName)}";
        readCommand.CommandTimeout = options.CommandTimeoutSeconds;

        await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.SourceRows++;
            var row = ReadRow(reader, plan);
            var originalRow = new Dictionary<IProperty, object?>(row);

            if (!TryApplyForeignKeyMaps(plan, row, originalRow))
            {
                result.SkippedRows++;
                result.SkippedMissingParentRows++;
                continue;
            }

            try
            {
                await ApplyConflictPolicy(plan, row, cancellationToken);
                var affected = await InsertRow(plan, row, cancellationToken);
                if (affected <= 0)
                {
                    result.SkippedRows++;
                    result.SkippedDuplicateRows++;
                    continue;
                }

                result.InsertedRows++;
                RecordKeyMaps(plan, originalRow, row);
            }
            catch (DbException exception)
            {
                result.FailedRows++;
                if (result.Errors.Count < 20)
                {
                    result.Errors.Add(exception.Message);
                }
            }
        }

        return result;
    }

    private Dictionary<IProperty, object?> ReadRow(DbDataReader reader, TablePlan plan)
    {
        var row = new Dictionary<IProperty, object?>(plan.Columns.Count);
        for (var i = 0; i < plan.Columns.Count; i++)
        {
            row[plan.Columns[i].Property] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return row;
    }

    private bool TryApplyForeignKeyMaps(
        TablePlan plan,
        Dictionary<IProperty, object?> row,
        IReadOnlyDictionary<IProperty, object?> originalRow)
    {
        foreach (var foreignKey in plan.ForeignKeys)
        {
            var oldValues = foreignKey.Properties.Select(x => Normalize(originalRow.TryGetValue(x, out var value) ? value : null)).ToArray();
            if (oldValues.All(x => x == null))
            {
                continue;
            }

            var principalMapId = new KeyMapId(foreignKey.PrincipalEntityType.GetTableName()!, foreignKey.PrincipalKey.Properties.Select(x => x.Name).ToArray());
            if (keyMaps.TryGetValue(principalMapId, out var tableMap) &&
                tableMap.TryGetValue(new ValueTupleKey(oldValues), out var newValues))
            {
                for (var i = 0; i < foreignKey.Properties.Count; i++)
                {
                    row[foreignKey.Properties[i]] = newValues[i];
                }
                continue;
            }

            if (foreignKey.IsRequired)
            {
                return false;
            }

            foreach (var property in foreignKey.Properties)
            {
                row[property] = null;
            }
        }

        return true;
    }

    private async Task ApplyConflictPolicy(
        TablePlan plan,
        Dictionary<IProperty, object?> row,
        CancellationToken cancellationToken)
    {
        if (options.ConflictPolicy != MergeConflictPolicy.RenameKnownUniqueValues)
        {
            return;
        }

        if (IsTable(plan, "Characters"))
        {
            await MakeStringColumnUnique(plan, row, "Name", maxLength: 50, cancellationToken);
        }
        else if (IsTable(plan, "Clans"))
        {
            var name = await MakeStringColumnUnique(plan, row, "Name", maxLength: 15, cancellationToken);
            if (name != null && TryGetProperty(plan, "NormalizeName", out var normalizeNameProperty))
            {
                var normalized = Utils.Utils.NormalizeSearchText(name);
                row[normalizeNameProperty] = await MakeUniqueValue(
                    plan,
                    normalizeNameProperty,
                    normalized,
                    maxLength: 15,
                    cancellationToken);
            }

            if (TryGetProperty(plan, "NodeId", out var nodeIdProperty) &&
                row.TryGetValue(nodeIdProperty, out var nodeIdValue) &&
                Convert.ToInt64(Normalize(nodeIdValue) ?? 0) != 0 &&
                await Exists(plan, nodeIdProperty, nodeIdValue, cancellationToken))
            {
                row[nodeIdProperty] = await NextHiddenClanNodeId(cancellationToken);
            }
        }
        else if (IsTable(plan, "GuestAccounts") || IsTable(plan, "LocalAccounts"))
        {
            await MakeStringColumnUnique(plan, row, "Username", maxLength: 50, cancellationToken);
        }
        else if (IsTable(plan, "GoogleAccounts"))
        {
            await MakeEmailColumnUnique(plan, row, "Email", maxLength: 255, cancellationToken);
        }
        else if (IsTable(plan, "GiftCodes"))
        {
            await MakeStringColumnUnique(plan, row, "Code", maxLength: 64, cancellationToken);
        }
    }

    private async Task<string?> MakeStringColumnUnique(
        TablePlan plan,
        Dictionary<IProperty, object?> row,
        string propertyName,
        int maxLength,
        CancellationToken cancellationToken)
    {
        if (!TryGetProperty(plan, propertyName, out var property) ||
            !row.TryGetValue(property, out var value) ||
            value is null or DBNull)
        {
            return null;
        }

        var uniqueValue = await MakeUniqueValue(plan, property, value.ToString() ?? string.Empty, maxLength, cancellationToken);
        row[property] = uniqueValue;
        return uniqueValue;
    }

    private async Task MakeEmailColumnUnique(
        TablePlan plan,
        Dictionary<IProperty, object?> row,
        string propertyName,
        int maxLength,
        CancellationToken cancellationToken)
    {
        if (!TryGetProperty(plan, propertyName, out var property) ||
            !row.TryGetValue(property, out var value) ||
            value is null or DBNull)
        {
            return;
        }

        var email = value.ToString() ?? string.Empty;
        if (!await Exists(plan, property, email, cancellationToken))
        {
            return;
        }

        var atIndex = email.IndexOf('@');
        var local = atIndex > 0 ? email[..atIndex] : email;
        var domain = atIndex > 0 ? email[atIndex..] : string.Empty;
        var suffix = CleanSuffix(options.SourceSuffix);

        for (var i = 1; i < 10_000; i++)
        {
            var marker = i == 1 ? $"+{suffix}" : $"+{suffix}{i}";
            var maxLocalLength = Math.Max(1, maxLength - domain.Length - marker.Length);
            var candidate = $"{TrimToLength(local, maxLocalLength)}{marker}{domain}";
            if (!await Exists(plan, property, candidate, cancellationToken))
            {
                row[property] = candidate;
                return;
            }
        }
    }

    private async Task<string> MakeUniqueValue(
        TablePlan plan,
        IProperty property,
        string value,
        int maxLength,
        CancellationToken cancellationToken)
    {
        value = TrimToLength(value, maxLength);
        if (!await Exists(plan, property, value, cancellationToken))
        {
            return value;
        }

        var suffix = CleanSuffix(options.SourceSuffix);
        for (var i = 1; i < 10_000; i++)
        {
            var marker = i == 1 ? $"_{suffix}" : $"_{suffix}_{i}";
            var candidate = $"{TrimToLength(value, Math.Max(1, maxLength - marker.Length))}{marker}";
            if (!await Exists(plan, property, candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Cannot create unique value for {plan.TableName}.{property.Name}");
    }

    private async Task<int> NextHiddenClanNodeId(CancellationToken cancellationToken)
    {
        for (var nodeId = -1; nodeId > int.MinValue; nodeId--)
        {
            await using var command = target.Database.GetDbConnection().CreateCommand();
            command.Transaction = target.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = "SELECT 1 FROM `Clans` WHERE `NodeId` = @nodeId LIMIT 1";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@nodeId";
            parameter.Value = nodeId;
            command.Parameters.Add(parameter);
            if (await command.ExecuteScalarAsync(cancellationToken) == null)
            {
                return nodeId;
            }
        }

        throw new InvalidOperationException("Cannot allocate hidden clan node id.");
    }

    private async Task<bool> Exists(TablePlan plan, IProperty property, object? value, CancellationToken cancellationToken)
    {
        var column = plan.Columns.First(x => x.Property == property);
        await using var command = target.Database.GetDbConnection().CreateCommand();
        command.Transaction = target.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandTimeout = options.CommandTimeoutSeconds;
        command.CommandText = $"SELECT 1 FROM {Quote(plan.TableName)} WHERE {Quote(column.ColumnName)} = @value LIMIT 1";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@value";
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync(cancellationToken) != null;
    }

    private async Task<int> InsertRow(TablePlan plan, Dictionary<IProperty, object?> row, CancellationToken cancellationToken)
    {
        await using var command = target.Database.GetDbConnection().CreateCommand();
        command.Transaction = target.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandTimeout = options.CommandTimeoutSeconds;

        var insertColumns = plan.Columns
            .Where(x => !x.IsIdentity)
            .ToArray();
        for (var i = 0; i < insertColumns.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@p{i}";
            parameter.Value = row[insertColumns[i].Property] ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        var insertVerb = options.UseInsertIgnore ? "INSERT IGNORE" : "INSERT";
        command.CommandText =
            $"{insertVerb} INTO {Quote(plan.TableName)} ({string.Join(", ", insertColumns.Select(x => Quote(x.ColumnName)))}) " +
            $"VALUES ({string.Join(", ", insertColumns.Select((_, i) => $"@p{i}"))})";

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected > 0 && plan.IdentityColumn != null)
        {
            row[plan.IdentityColumn.Property] = await ReadLastInsertId(cancellationToken);
        }

        return affected;
    }

    private async Task<long> ReadLastInsertId(CancellationToken cancellationToken)
    {
        await using var command = target.Database.GetDbConnection().CreateCommand();
        command.Transaction = target.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT LAST_INSERT_ID()";
        command.CommandTimeout = options.CommandTimeoutSeconds;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value);
    }

    private void RecordKeyMaps(
        TablePlan plan,
        IReadOnlyDictionary<IProperty, object?> originalRow,
        IReadOnlyDictionary<IProperty, object?> insertedRow)
    {
        foreach (var key in plan.Keys)
        {
            var mapId = new KeyMapId(plan.TableName, key.Properties.Select(x => x.Name).ToArray());
            var oldKey = new ValueTupleKey(key.Properties.Select(x => Normalize(originalRow[x])).ToArray());
            var newKey = key.Properties.Select(x => Normalize(insertedRow[x])).ToArray();
            if (!keyMaps.TryGetValue(mapId, out var map))
            {
                map = new Dictionary<ValueTupleKey, object?[]>();
                keyMaps[mapId] = map;
            }

            map[oldKey] = newKey;
        }
    }

    private List<TablePlan> BuildPlans()
    {
        var storeObjectIds = new ConcurrentDictionary<IEntityType, StoreObjectIdentifier>();
        var plans = target.Model.GetEntityTypes()
            .Where(x => x.BaseType == null && !string.IsNullOrWhiteSpace(x.GetTableName()))
            .Select(entity =>
            {
                var tableName = entity.GetTableName()!;
                var storeObject = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
                storeObjectIds[entity] = storeObject;
                var columns = entity.GetProperties()
                    .Where(x => x.GetColumnName(storeObject) != null)
                    .Select(x => new ColumnPlan(
                        x,
                        x.GetColumnName(storeObject)!,
                        IsIdentityColumn(entity, x)))
                    .ToList();

                return new TablePlan(
                    entity,
                    tableName,
                    columns,
                    columns.FirstOrDefault(x => x.IsIdentity),
                    entity.GetKeys().ToArray(),
                    entity.GetForeignKeys().Where(x => !x.IsOwnership).ToArray());
            })
            .Where(x => !options.SkipTables.Contains(x.TableName))
            .ToList();

        return TopologicalSort(plans);
    }

    private static bool IsIdentityColumn(IEntityType entityType, IProperty property)
    {
        var primaryKey = entityType.FindPrimaryKey();
        if (primaryKey?.Properties.Count != 1 || primaryKey.Properties[0] != property)
        {
            return false;
        }

        if (property.ValueGenerated == ValueGenerated.OnAdd)
        {
            return true;
        }

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
               (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(uint) || clrType == typeof(ulong));
    }

    private static List<TablePlan> TopologicalSort(IReadOnlyCollection<TablePlan> plans)
    {
        var byEntity = plans.ToDictionary(x => x.EntityType);
        var byName = plans.ToDictionary(x => x.TableName, StringComparer.OrdinalIgnoreCase);
        var remaining = plans.ToDictionary(x => x.TableName, StringComparer.OrdinalIgnoreCase);
        var sorted = new List<TablePlan>();

        while (remaining.Count > 0)
        {
            var ready = remaining.Values
                .Where(plan => plan.ForeignKeys.All(fk =>
                    !byEntity.TryGetValue(fk.PrincipalEntityType, out var principal) ||
                    principal.TableName == plan.TableName ||
                    !remaining.ContainsKey(principal.TableName)))
                .OrderBy(x => x.TableName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ready.Count == 0)
            {
                ready = remaining.Values.OrderBy(x => x.TableName, StringComparer.OrdinalIgnoreCase).Take(1).ToList();
            }

            foreach (var plan in ready)
            {
                sorted.Add(plan);
                remaining.Remove(plan.TableName);
            }
        }

        return sorted.Where(x => byName.ContainsKey(x.TableName)).ToList();
    }

    private static async Task<int> CountRows(DbConnection connection, TablePlan plan, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {Quote(plan.TableName)}";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static string Quote(string identifier)
    {
        return $"`{identifier.Replace("`", "``")}`";
    }

    private static bool IsTable(TablePlan plan, string tableName)
    {
        return string.Equals(plan.TableName, tableName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetProperty(TablePlan plan, string propertyName, out IProperty property)
    {
        property = plan.Columns
            .Select(x => x.Property)
            .FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase))!;
        return property != null;
    }

    private static string CleanSuffix(string suffix)
    {
        var value = new string((suffix ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return string.IsNullOrWhiteSpace(value) ? "B" : value;
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (maxLength <= 0) return string.Empty;
        value ??= string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static object? Normalize(object? value)
    {
        return value switch
        {
            null or DBNull => null,
            byte valueByte => Convert.ToInt64(valueByte),
            sbyte valueSByte => Convert.ToInt64(valueSByte),
            short valueShort => Convert.ToInt64(valueShort),
            ushort valueUShort => Convert.ToInt64(valueUShort),
            int valueInt => Convert.ToInt64(valueInt),
            uint valueUInt => Convert.ToInt64(valueUInt),
            long valueLong => valueLong,
            ulong valueULong => Convert.ToInt64(valueULong),
            _ => value
        };
    }

    private sealed record TablePlan(
        IEntityType EntityType,
        string TableName,
        List<ColumnPlan> Columns,
        ColumnPlan? IdentityColumn,
        IReadOnlyList<IKey> Keys,
        IReadOnlyList<IForeignKey> ForeignKeys);

    private sealed record ColumnPlan(IProperty Property, string ColumnName, bool IsIdentity);

    private readonly record struct KeyMapId(string TableName, string[] PropertyNames)
    {
        public bool Equals(KeyMapId other)
        {
            return string.Equals(TableName, other.TableName, StringComparison.OrdinalIgnoreCase) &&
                   PropertyNames.SequenceEqual(other.PropertyNames, StringComparer.Ordinal);
        }

        public override int GetHashCode()
        {
            var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(TableName);
            foreach (var propertyName in PropertyNames)
            {
                hash = HashCode.Combine(hash, propertyName);
            }

            return hash;
        }
    }

    private readonly record struct ValueTupleKey(object?[] Values)
    {
        public bool Equals(ValueTupleKey other)
        {
            return Values.SequenceEqual(other.Values);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var value in Values)
            {
                hash = HashCode.Combine(hash, value);
            }

            return hash;
        }
    }
}
