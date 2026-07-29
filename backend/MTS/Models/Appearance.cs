using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Models;
[MemoryPackable]
public partial class Appearance
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public Dictionary<string, string> Skins { get; set; } = new();
    public Dictionary<string, Color> Colors { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public CharacterData CharacterData { get; set; }
}