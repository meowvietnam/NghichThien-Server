using MTS.Models;

namespace MTS.Network.Models;

public class BuySeedResult : Result
{
    public Seed Seed { get; set; }
    public int NewLinhThach { get; set; }
}