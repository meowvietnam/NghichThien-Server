using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class SetEquipmentName : IMemorySerializable<SetEquipmentName>
    {
        public ushort SelfId { get; set; }
        public ushort Id { get; set; }
        public string Name { get; set; }
        public void Deserialize(IReader reader, ref SetEquipmentName value)
        {
            value.SelfId = reader.Read<ushort>();
            value.Id = reader.Read<ushort>();
            value.Name = reader.Read();
        }

        public int ByteSize()
        {
            return SelfId.ByteSize() + Id.ByteSize() + Name.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(SelfId);
            writer.Write(Id);
            writer.Write(Name);
        }
    }
}