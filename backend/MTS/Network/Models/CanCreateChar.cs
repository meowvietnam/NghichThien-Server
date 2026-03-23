using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class CanCreateChar : IMemorySerializable<CanCreateChar>
    {
        public bool CanCreate { get; set; }
        public string Message { get; set; }

        public void Deserialize(IReader reader, ref CanCreateChar value)
        {
            value.CanCreate = reader.Read<bool>();
            if (!value.CanCreate)
            {
                value.Message = reader.Read();
            }
        }

        public int ByteSize()
        {
            if (CanCreate)
            {
                return CanCreate.ByteSize();
            }
            else
            {
                return CanCreate.ByteSize() + Message.ByteSize();
            }
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(CanCreate);
            if (!CanCreate)
            {
                writer.Write(Message);
            }
        }
    }
}