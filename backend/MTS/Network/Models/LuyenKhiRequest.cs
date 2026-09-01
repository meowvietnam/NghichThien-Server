using MTS.Game.LuyenKhi;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class LuyenKhiRequest : IMemorySerializable<LuyenKhiRequest>
{
    public ArtifactType SelectedType { get; set; }
    public ushort[] MaterialIds { get; set; }

    public void Deserialize(IReader reader, ref LuyenKhiRequest value)
    {
        value.SelectedType = reader.Read<ArtifactType>();
        value.MaterialIds = reader.ReadArray<ushort>();
    }
    public int ByteSize()
    {
        return SelectedType.ByteSize() + MaterialIds.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(SelectedType);
        writer.WriteArray(MaterialIds);
    }
}