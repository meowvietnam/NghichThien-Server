using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class StatModifier : IMemorySerializable<StatModifier>
{
    public StatModifier(StatType type, Action action, float value, int cost)
    {
        Type = type;
        Action = action;
        Value = value;
        Cost = cost;
    }

    public StatModifier()
    {
    }
    public StatType Type { get; private set; }
    public Action Action { get; private set; }
    public float Value { get; set; }
    public int Cost { get; private set; }
    public LinhCan LinhCan => (LinhCan)((int)Value);
    public Ability Ability => (Ability)((int)Value);
    public bool IsLinhCan()
    {
        return Type == StatType.LinhCan;
    }
    public bool IsAbility()
    {
        return Type == StatType.Ability;
    }
    public int ByteSize()
    {
        return Type.ByteSize() + Action.ByteSize() + Value.ByteSize() + Cost.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Type);
        writer.Write(Action);
        writer.Write(Value);
        writer.Write(Cost);
    }

    public void Deserialize(IReader reader, ref StatModifier value)
    {
        value.Type = reader.Read<StatType>();
        value.Action = reader.Read<Action>();
        value.Value = reader.Read<float>();
        value.Cost = reader.Read<int>();
    }
}