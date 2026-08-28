namespace MTS.Models;

public class DongPhuHistory
{
    public int Id { get; set; }
    public int CharId { get; set; }
    public string Log { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
}