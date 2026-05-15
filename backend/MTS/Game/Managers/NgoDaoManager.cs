using MTS.Game.Resources;
using Network;
using Network.Extensions;

namespace MTS.Game.Managers;

public class NgoDaoManager : IMemorySerializable<NgoDaoManager>
{
    public List<NgoDaoResource> NgoDaoResources { get; set; }

    public void Deserialize(IReader reader, ref NgoDaoManager value)
    {
        value.NgoDaoResources = reader.ReadArray<NgoDaoResource>().ToList();
    }

    public int ByteSize()
    {
        return NgoDaoResources.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(NgoDaoResources);
    }
}