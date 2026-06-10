using MTS.Game.Resources;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Game.MainQuest
{
    public class MainQuestResource : BaseData, IMemorySerializable<MainQuestResource>
    {
        public int levelRequire = 1;
        public RewardSnapshot rewardRef;

        public void Deserialize(IReader reader, ref MainQuestResource value)
        {
            BaseData baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.levelRequire = reader.Read<int>();
            value.rewardRef = reader.Read<RewardSnapshot>();
        }
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + levelRequire.ByteSize() + rewardRef.ByteSize();
        }
        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.Write(levelRequire);
            writer.Write(rewardRef);
        }
    }
}
