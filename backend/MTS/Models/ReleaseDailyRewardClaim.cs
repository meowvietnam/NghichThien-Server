using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MTS.Models;

public class ReleaseDailyRewardClaim
{
    public int CharId { get; set; }
    public int RewardDay { get; set; }
    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }
}
