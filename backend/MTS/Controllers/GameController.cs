using System.Diagnostics;
using JetBrains.Profiler.Api;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MTS.Game;
using MTS.Game.CreateCharacter;
using MTS.Network;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;

namespace MTS.Controllers;

[Route("/game")]
public class GameController : ControllerBase
{
    [HttpGet("clients-online")]
    public IActionResult GetClientOnline()
    {
        var server = TcpServer.Instance;
        var clients = server.GetClientsOnline();
        var clientsResult = clients.Select(x => new Api.Models.Client(x)).ToArray();
        return Ok(clientsResult);
    }

    [HttpGet("clients-disconnected")]
    public IActionResult GetClientsDisconnected()
    {
        var server = TcpServer.Instance;
        var clients = server.GetClientsDisconnected();
        var clientsResult = clients.Select(x => new Api.Models.Client(x)).ToArray();
        return Ok(clientsResult);
    }
}