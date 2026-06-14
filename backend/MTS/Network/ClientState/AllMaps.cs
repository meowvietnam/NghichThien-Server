using MTS.Game;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class AllMaps : IClientState
{
    private Client client;
    private const string NotEnoughEnergy = "Không đủ năng lượng";
    private bool isMoving;

    public AllMaps(Client client)
    {
        this.client = client;
    }

    public void OnEnter()
    {
    }

    private CharacterSnapshot[] GetCharsOnlineThisNode()
    {
        var progression = client.CharacterData.Progression;
        var currentNodeId = progression.IdCurrentNode;
        var charId = client.CharacterData.Id;
        var charsOnline = TcpServer.Instance.GetCharsOnline()
            .Where(x => x.Progression.IdCurrentNode == currentNodeId && x.Id != charId)
            .Select(x => new CharacterSnapshot(x))
            .Take(5)
            .ToArray();
        return charsOnline;
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        var charData = client.CharacterData;
        if (tag == MessageTag.MoveToMap)
        {
            if (isMoving)
            {
                return;
            }
            isMoving = true;
            var id = reader.Read<int>();
            try
            {
                var currentMapId = charData.Progression.IdCurrentNode;
                var path = GameConfig.MapManager.FindPath(currentMapId, id);
                var energy = charData.IsInfiniteEnergy ? 0 : path.Count;
                var newEnergy = charData.Progression.CurrentEnergy - energy;
                if (newEnergy < 0)
                {
                    throw new Exception($"{NotEnoughEnergy}: {energy}");
                }
                var stats = charData.Stats;
                var dayUsed = (int)Math.Round(32f * path.Count / stats.TocDoBay, MidpointRounding.AwayFromZero);
                var newAge = stats.TuoiTac + new TimeSpan(dayUsed, 0, 0, 0);
                stats.SetTuoiTac(newAge);
                charData.Progression.SetIdCurrentNode(id);
                charData.Progression.SetCurrentEnergy(newEnergy);
                var charsOnline = GetCharsOnlineThisNode();
                var pathIds = path.Select(x => x.Id).ToArray();
                var result = new MoveToMapResult()
                {
                    Success = true,
                    Energy = newEnergy,
                    Path = pathIds,
                    CharactersOnline = charsOnline,
                    NewAge = newAge
                };
                client.Send(MessageTag.MoveToMapResult, result);
            }
            catch (Exception e)
            {
                var result = new MoveToMapResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.MoveToMapResult, result);
            }
            finally
            {
                isMoving = false;
            }
        }
        else if (tag == MessageTag.GetPlayerOnlineThisNode)
        {
            try
            {
                var charsOnline = GetCharsOnlineThisNode();
                var result = new GetCharacterOnlineThisNodeResult()
                {
                    Success = true,
                    CharactersOnline = charsOnline
                };
                client.Send(MessageTag.GetPlayerOnlineThisNodeResult, result);
            }
            catch (Exception e)
            {
                var result = new GetCharacterOnlineThisNodeResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.GetPlayerOnlineThisNodeResult, result);
            }
        }
        else if(tag == MessageTag.WatchAdsForEnergy)
        {
            WatchAdsForEnergyResult result = null;
            try
            {
                lock (charData)
                {
                    charData.Progression.SetCurrentEnergy(100);
                    result = new WatchAdsForEnergyResult
                    {
                        Success = true,
                        NewEnergy = 100
                    };
                }
            }
            catch (Exception e)
            {
                result = new WatchAdsForEnergyResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.WatchAdsForEnergyResult, result);
            }
        }
    }
    public void OnLeave()
    {
    }
    public MessageState State => MessageState.AllMaps;
}