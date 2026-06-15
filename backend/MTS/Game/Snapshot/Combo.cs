using MemoryPack;
using MTS.TT;
using MTS.CP;
using MTS.ND;
namespace MTS.Game.Snapshot
{
    [MemoryPackable]
    public partial class Combo
    {
        public ushort[] ThanThongIds { get; set; }
        public ushort[] CongPhapIds { get; set; }
        public ushort[] NgoDaoIds { get; set; }
        [MemoryPackIgnore]
        private ThanThong[] thanThongs;
        [MemoryPackIgnore]
        public ThanThong[] ThanThongs
        {
            get
            {
                if (thanThongs == null)
                {
                    thanThongs = GameConfig.NDCPTTManager.ThanThongs
                        .Where(x => ThanThongIds.Contains((ushort)x.Id)).ToArray();
                }
                return thanThongs;
            }
        }
        [MemoryPackIgnore]
        private CongPhap[] congPhaps;
        [MemoryPackIgnore]
        public CongPhap[] CongPhaps
        {
            get
            {
                if (congPhaps == null)
                {
                    congPhaps = GameConfig.NDCPTTManager.CongPhaps
                        .Where(x => CongPhapIds.Contains((ushort)x.Id)).ToArray();
                }
                return congPhaps;
            }
        }
        [MemoryPackIgnore]
        private NgoDao[] ngoDaos;
        [MemoryPackIgnore]
        public NgoDao[] NgoDaos
        {
            get
            {
                if (ngoDaos == null)
                {
                    ngoDaos = GameConfig.NDCPTTManager.NgoDaos
                        .Where(x => NgoDaoIds.Contains((ushort)x.Id)).ToArray();
                }
                return ngoDaos;
            }
        }
    }
}