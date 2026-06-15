using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.Game;
using MTS.Network.ClientState;
using Network;
using Network.Extensions;

namespace MTS.Models;

[MemoryPackable]
public partial class CongPhap : IEquatable<CongPhap>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public ushort Id { get; set; }
    public ushort Level { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public InventoryManager Inventory { get; set; }


    public override int GetHashCode()
    {
        return Id;
    }

    public bool Equals(CongPhap? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public CP.CongPhap GetCongPhap()
    {
        return GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == Id);
    }
}