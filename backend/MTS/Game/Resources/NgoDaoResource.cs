using MTS.Game;
using MTS.ND;
using Network;
using Network.Extensions;

namespace MTS.Game.Resources;

public class NgoDaoResource : BaseData, IMemorySerializable<NgoDaoResource>
{
    public List<List<NgoDao>> NgoDaos { get; set; }
    public ETypeSkill ETypeSkill { get; set; }

    public static int[] CamNgoRequire =
    [
        1000,
        8000,
        30000,
        70000,
        150000
    ];

    public override int ByteSize()
    {
        return base.ByteSize() + NgoDaos.Count.ByteSize()
                               + NgoDaos.Sum(x => x.Select(y => y.Id).ByteSize())
                               + ETypeSkill.ByteSize();
    }

    public void Deserialize(IReader reader, ref NgoDaoResource value)
    {
        BaseData baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        var ngoDaos = GameConfig.NDCPTTManager.NgoDaos;
        var count = reader.Read<int>();
        value.NgoDaos = Enumerable.Range(0, count).Select(_ =>
        {
            return reader.ReadArray<int>().Select(y =>
            {
                return ngoDaos.First(z => z.Id == y);
            }).ToList();
        }).ToList();
        value.ETypeSkill = reader.Read<ETypeSkill>();
    }

    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.Write(NgoDaos.Count);
        foreach (var ngoDaoList in NgoDaos)
        {
            writer.Write(ngoDaoList.Count);
            writer.WriteArray(ngoDaoList.Select(x => x.Id));
        }
        writer.Write(ETypeSkill);
    }
}