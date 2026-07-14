using MTS.Models;

namespace MTS.Api.Models;

public class Character
{
    public int CharId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int MoneyVip { get; set; }
    public int LinhThach { get; set; }
    public bool IsInfinityEnergy {get; set;}
    public bool IsNoAds { get; set; }
    public Character(CharacterData characterData)
    {
        CharId = characterData.Id;
        Name = characterData.Name;
        Level = characterData.Progression.Level;
        MoneyVip = characterData.MoneyVip;
        LinhThach = characterData.Stats.LinhThach;
        IsInfinityEnergy = characterData.IsInfiniteEnergy;
        IsNoAds = characterData.IsNoAds;
    }
}