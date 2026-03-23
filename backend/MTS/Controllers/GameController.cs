using System.Diagnostics;
using JetBrains.Profiler.Api;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MTS.Game;
using MTS.Game.CreateCharacter;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;

namespace MTS.Controllers;

[Route("/game")]
public class GameController : ControllerBase
{
}