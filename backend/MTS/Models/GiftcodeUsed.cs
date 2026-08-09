using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class GiftcodeUsed
{
    public int CharId { get; set; }
    public int CodeId { get; set; }
    [ForeignKey(nameof(CodeId))]
    public Giftcode Giftcode { get; set; }
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }
}