using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class LuyenDanRequest : IMemorySerializable<LuyenDanRequest>
{
    public class Herbs : IMemorySerializable<Herbs>
    {
        public int Id { get; private set; }
        public int Count { get; private set; }

        public Herbs()
        {
        }
        public bool IsDefault()
        {
            return Id == 0;
        }

        public int ByteSize()
        {
            return Id.ByteSize() + Count.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Id);
            writer.Write(Count);
        }

        public void Deserialize(IReader reader, ref Herbs value)
        {
            value.Id = reader.Read<int>();
            value.Count = reader.Read<int>();
        }
    }
    public LuyenDanRequest()
    {
    }
    public LuyenDanRequest(Herbs chuDuoc, Herbs phuDuoc1, Herbs phuDuoc2, Herbs duocDan, int furnaceId, int count)
    {
        ChuDuoc = chuDuoc;
        PhuDuoc1 = phuDuoc1;
        PhuDuoc2 = phuDuoc2;
        DuocDan = duocDan;
        FurnaceId = furnaceId;
        Count = count;
    }
    public Herbs ChuDuoc { get; private set; }
    public Herbs PhuDuoc1 { get; private set; }
    public Herbs PhuDuoc2 { get; private set; }
    public Herbs DuocDan { get; private set; }
    public int FurnaceId { get; private set; }
    public int Count { get; private set; }

    public int ByteSize()
    {
        return ChuDuoc.ByteSize() + PhuDuoc1.ByteSize() + PhuDuoc2.ByteSize() + DuocDan.ByteSize() +
               FurnaceId.ByteSize() + Count.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(ChuDuoc);
        writer.Write(PhuDuoc1);
        writer.Write(PhuDuoc2);
        writer.Write(DuocDan);
        writer.Write(FurnaceId);
        writer.Write(Count);
    }
    public void Deserialize(IReader reader, ref LuyenDanRequest value)
    {
        value.ChuDuoc = reader.Read<Herbs>();
        value.PhuDuoc1 = reader.Read<Herbs>();
        value.PhuDuoc2 = reader.Read<Herbs>();
        value.DuocDan = reader.Read<Herbs>();
        value.FurnaceId = reader.Read<int>();
        value.Count = reader.Read<int>();
    }
}