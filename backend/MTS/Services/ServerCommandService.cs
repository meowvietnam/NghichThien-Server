using System.Diagnostics;
using MTS.Network;
using Spectre.Console;

namespace MTS.Services;

public class ServerCommandService : BackgroundService
{
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<ServerCommandService> logger;
    private readonly DateTime startedAt = DateTime.UtcNow;
    private readonly CancellationTokenSource shutdownTokenSource = new();

    public ServerCommandService(IHostApplicationLifetime lifetime, ILogger<ServerCommandService> logger)
    {
        this.lifetime = lifetime;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var linkedTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, shutdownTokenSource.Token);
        var commandToken = linkedTokenSource.Token;

        if (Console.IsInputRedirected)
        {
            ServerConsole.WriteInfo("Server command input is redirected; command loop disabled.");
            return;
        }

        PrintHeader();
        while (!commandToken.IsCancellationRequested)
        {
            try
            {
                var command = await Task.Run(ServerConsole.ReadCommand, commandToken);
                await ExecuteCommandAsync(command.Trim(), commandToken);
            }
            catch (OperationCanceledException) when (commandToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        shutdownTokenSource.Cancel();
        logger.LogInformation("Server command service stop requested.");
        return Task.CompletedTask;
    }

    private async Task ExecuteCommandAsync(string command, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        switch (parts[0].ToLowerInvariant())
        {
            case "help":
            case "?":
                PrintHelp();
                break;
            case "status":
                PrintStatus();
                break;
            case "online":
                PrintOnlinePlayers();
                break;
            case "save":
                await SaveClientsAsync();
                break;
            case "gc":
                RunGc();
                break;
            case "clear":
            case "cls":
                Console.Clear();
                PrintHeader();
                break;
            case "stop":
            case "exit":
            case "quit":
                StopServer();
                break;
            default:
                ServerConsole.WriteWarning($"Unknown command: {command}");
                PrintHelp();
                break;
        }
    }

    private void PrintHeader()
    {
        ServerConsole.WriteTitle("MTS Server Command Console");
        ServerConsole.WriteInfo("Type 'help' to list available commands.");
    }

    private static void PrintHelp()
    {
        var rows = new[]
        {
            ("help", "Show available commands"),
            ("status", "Show uptime, memory, and connection counters"),
            ("online", "List online selected characters"),
            ("save", "Save all online clients"),
            ("gc", "Force garbage collection and print reclaimed memory"),
            ("clear", "Clear console"),
            ("stop", "Stop the server gracefully"),
        };

        ServerConsole.WriteTable("Commands", ["Command", "Description"], rows);
    }

    private void PrintStatus()
    {
        var server = TcpServer.Instance;
        var onlineClients = server?.GetClientsOnline().Length ?? 0;
        var selectedCharacters = server?.GetCharsOnline().Length ?? 0;
        var disconnectedClients = server?.GetClientsDisconnected().Length ?? 0;
        var memory = GC.GetTotalMemory(false) / 1024d / 1024d;
        var uptime = DateTime.UtcNow - startedAt;
        var process = Process.GetCurrentProcess();

        ServerConsole.WriteTable("Server Status", ["Metric", "Value"],
        [
            ("Uptime", uptime.ToString(@"dd\.hh\:mm\:ss")),
            ("Online clients", onlineClients.ToString()),
            ("Selected characters", selectedCharacters.ToString()),
            ("Disconnected cache", disconnectedClients.ToString()),
            ("Managed memory", $"{memory:N2} MB"),
            ("Threads", process.Threads.Count.ToString()),
        ]);
    }

    private static void PrintOnlinePlayers()
    {
        var characters = TcpServer.Instance?
            .GetCharsOnline()
            .OrderBy(x => x.Id)
            .Take(50)
            .Select(x => (x.Id.ToString(), x.Name, x.Progression.Level.ToString()))
            .ToArray() ?? [];

        if (characters.Length == 0)
        {
            ServerConsole.WriteInfo("No selected characters are online.");
            return;
        }

        ServerConsole.WriteTable("Online Characters", ["Id", "Name", "Level"], characters);
    }

    private async Task SaveClientsAsync()
    {
        if (TcpServer.Instance == null)
        {
            ServerConsole.WriteWarning("TCP server is not ready.");
            return;
        }

        try
        {
            ServerConsole.WriteInfo("Saving online clients...");
            await TcpServer.Instance.SaveClients();
            ServerConsole.WriteSuccess("Online clients saved.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save clients from server command");
            ServerConsole.WriteError("Failed to save online clients. Check log file for details.");
        }
    }

    private static void RunGc()
    {
        var before = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var after = GC.GetTotalMemory(true);
        var reclaimed = Math.Max(0, before - after) / 1024d / 1024d;
        ServerConsole.WriteSuccess($"GC completed. Reclaimed about {reclaimed:N2} MB.");
    }

    private void StopServer()
    {
        ServerConsole.WriteWarning("Stopping server...");
        lifetime.StopApplication();
    }
}

public static class ServerConsole
{
    public static void WriteTitle(string message)
    {
        AnsiConsole.MarkupLine("[bold cyan]== {0} ==[/]", Markup.Escape(message));
    }

    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine("[cyan]info[/] {0}", Markup.Escape(message));
    }

    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine("[green]ok[/] {0}", Markup.Escape(message));
    }

    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine("[yellow]warn[/] {0}", Markup.Escape(message));
    }

    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine("[red]error[/] {0}", Markup.Escape(message));
    }

    public static void WriteTable(string title, string[] headers, IEnumerable<(string First, string Second)> rows)
    {
        WriteTable(title, headers, rows.Select(x => new[] { x.First, x.Second }));
    }

    public static void WriteTable(string title, string[] headers, IEnumerable<(string First, string Second, string Third)> rows)
    {
        WriteTable(title, headers, rows.Select(x => new[] { x.First, x.Second, x.Third }));
    }

    private static void WriteTable(string title, string[] headers, IEnumerable<string[]> rows)
    {
        var table = new Table()
            .Title($"[bold cyan]{Markup.Escape(title)}[/]")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);
        foreach (var header in headers)
        {
            table.AddColumn(new TableColumn($"[bold]{Markup.Escape(header)}[/]"));
        }

        foreach (var row in rows)
        {
            table.AddRow(row.Select(Markup.Escape).ToArray());
        }

        AnsiConsole.Write(table);
    }

    public static string ReadCommand()
    {
        return AnsiConsole.Ask<string>("[grey]mts>[/]");
    }
}
