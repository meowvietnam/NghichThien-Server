using System.ComponentModel.DataAnnotations;
using MTS.Game.Auction;

namespace MTS.Models;

public class AuctionHistory
{
    [Key]
    public int Id { get; set; }
    public AuctionSession[] Sessions { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}