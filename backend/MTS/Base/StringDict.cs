using Network;
using Network.Extensions;

namespace MTS.Base
{
    [Serializable]
    public class StringDict : IMemorySerializable<StringDict>
    {
        public List<string> Dict { get; private set; }

        public int Add(string value)
        {
            var result = Dict.IndexOf(value);
            if (result == -1)
            {
                var index = Dict.Count;
                Dict.Add(value);
                return index;
            }
            return result;
        }
        public string this[int key] => Dict[key];

        public void Deserialize(IReader reader, ref StringDict value)
        {
            value.Dict = reader.ReadArray().ToList();
        }

        public int ByteSize()
        {
            return Dict.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.WriteArray(Dict);
        }
    }
}