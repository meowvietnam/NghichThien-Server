using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MTS.Converter;

public class Json
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new ContractResolver(),
        Formatting = Formatting.Indented,
    };

    public static T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
    public static string Serialize<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Settings);
    }
}

public class ContractResolver : DefaultContractResolver
{
    protected override List<MemberInfo> GetSerializableMembers(Type objectType)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        return objectType
            .GetMembers(flags)
            .Where(m =>
                m.MemberType == MemberTypes.Field ||
                m.MemberType == MemberTypes.Property)
            .ToList();
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (!property.Writable)
        {
            if (member is PropertyInfo prop)
            {
                var hasPrivateSetter = prop.GetSetMethod(true) != null;
                property.Writable = hasPrivateSetter;
            }
        }
        property.Readable = true;
        return property;
    }
}