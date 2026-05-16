using MTS.ND;

namespace MTS.Network.Models;

public struct NgoDaoBuff
{
    public NgoDaoBuffStats.EBuffStats BuffStat { get; set; } =  NgoDaoBuffStats.EBuffStats.None;
    public int BuffValue { get; set; } = 0;
    public NgoDaoBuff()
    {
    }
}