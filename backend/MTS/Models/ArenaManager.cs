using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using Newtonsoft.Json;

namespace MTS.Models;
[MemoryPackable]
public partial class ArenaManager
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public int CurrentArenaCount { get; private set; }
    public int Points { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }

    [System.Text.Json.Serialization.JsonIgnore] [JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private bool dirty;

    public bool IsDefault()
    {
        return CharId == 0;
    }
    public void SetCurrentArenaCount(int count)
    {
        if (CurrentArenaCount != count)
        {
            CurrentArenaCount = count;
            dirty = true;
        }
    }
    public void SetPoints(int points)
    {
        if (Points != points)
        {
            Points = points;
            dirty = true;
        }
    }
    public async Task SaveChanges(AppDbContext db)
    {
        if (dirty)
        {
            await db.ArenaManagers
                .Where(x => x.CharId == CharId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.Points, Points)
                        .SetProperty(x => x.CurrentArenaCount, CurrentArenaCount));
            dirty = false;
        }
    }
    
}