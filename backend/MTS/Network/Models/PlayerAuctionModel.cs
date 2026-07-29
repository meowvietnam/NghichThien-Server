using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    [Serializable]
    public partial class PlayerAuctionModel
    {
        public int PlayerId { get; set; }
        public int CharId { get; set; }
        public string Name { get; set; }
        public Gender Gender { get; set; }
        public Appearance Appearance { get; set; }
        public PlayerAuctionStatus Status { get; set; }
        public int Amount { get; set; }
        [MemoryPackIgnore]
        public DateTime JoinedAt { get; set; }
        [MemoryPackConstructor]
        public PlayerAuctionModel()
        {
            
        }

        public PlayerAuctionModel(CharacterData charData, int playerId)
        {
            CharId = charData.Id;
            PlayerId = playerId;
            Status = PlayerAuctionStatus.Guest;
            Name = charData.Name;
            Gender = charData.Gender;
            Appearance = charData.Appearance;
            Amount = 0;
            JoinedAt = DateTime.UtcNow;
        }
        public PlayerAuctionModel Clone()
        {
            return new PlayerAuctionModel()
            {
                CharId = CharId,
                PlayerId = PlayerId,
                Status = Status,
                Name = Name,
                Gender = Gender,
                Appearance = Appearance,
                Amount = Amount,
                JoinedAt = JoinedAt
            };
        }
        public void ResetNewSession()
        {
            Amount = 0;
            Status = PlayerAuctionStatus.Guest;
        }
    }
}