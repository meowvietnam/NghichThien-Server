using MTS.Game.MainQuest;
using MTS.Game.Resources;
using Network;
using Network.Extensions;

namespace MTS.Game.Managers
{
    public class MainQuestManagerModel : IMemorySerializable<MainQuestManagerModel>
    {
        public List<MainQuestResource> MainQuestResource { get; set; }

        public int ByteSize()
        {
            return MainQuestResource.ByteSize();
        }

        public void Deserialize(IReader reader, ref MainQuestManagerModel value)
        {
            value.MainQuestResource = reader.ReadArray<MainQuestResource>().ToList();
        }

        public void Serialize(IWriter writer)
        {
            writer.WriteArray(MainQuestResource);
        }
    }
}
