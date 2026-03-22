using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class StepConfig : IMemorySerializable<StepConfig>
{
    public List<StepData> StepDatas { get; private set; }
    public int ByteSize()
    {
        return StepDatas.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(StepDatas);
    }

    public void Deserialize(IReader reader, ref StepConfig value)
    {
        var count = reader.Read<int>();
        var stepDatas = new List<StepData>(count);
        value.StepDatas = stepDatas;
        for (int i = 0; i < count; i++)
        {
            var stepType = reader.Read<StepType>();
            if (stepType == StepType.Selection)
            {
                var step = reader.Read<SelectionStepData>();
                stepDatas.Add(step);
            }
            else if (stepType == StepType.Elemental)
            {
                var step = reader.Read<ElementalStepData>();
                stepDatas.Add(step);
            }
        }
    }
}