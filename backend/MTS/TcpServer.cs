using MTS.Converter;
using MTS.Data;
using MTS.Game;
using MTS.Game.CreateCharacter;
using MTS.Models;
using Network;
using Network.Extensions;
using NetworkServer;

namespace MTS;

public class TcpServer : BaseTcpServer
{
    public readonly object DbLock = new object();
    public readonly AppDbContext Db;
 
    public TcpServer(int port, AppDbContext db) : base(port)
    {
        Db = db;
    }

    protected override void OnRun()
    {
        Console.WriteLine($"TcpServer is running at port: {Port}");
        GameConfig.Init();
    }
}