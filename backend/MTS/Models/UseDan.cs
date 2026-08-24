using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class UseDan
{
    
    public int? Id { get; set; }
    public ushort DanId { get; set; }
    public int Count { get; set; }
    public int CharId { get; set; }
}