using MemoryPack;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class GetMailsResult 
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<Mail> Mails { get; set; }
    public bool HasNext { get; set; }
}