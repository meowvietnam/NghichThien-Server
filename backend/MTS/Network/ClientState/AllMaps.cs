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

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
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
                var charData = client.CharacterData;
                var currentMapId = charData.IdCurrentNode;
                var path = GameConfig.MapManager.FindPath(currentMapId, id);
                var energy = client.UserData.IsInfiniteEnergy ? 0 : path.Count;
                var newEnergy = charData.CurrentEnergy - energy;
                if (newEnergy < 0)
                {
                    throw new Exception($"{NotEnoughEnergy}: {energy}");
                }

                charData.IdCurrentNode = id;
                charData.CurrentEnergy = newEnergy;
                var charsOnline = TcpServer.Instance.GetCharsOnline().Select(x => new CharacterSnapshot(x)).ToArray();
                var pathIds = path.Select(x => x.Id).ToArray();
                var result = new MoveToMapResult()
                {
                    Success = true,
                    Energy = newEnergy,
                    Path = pathIds,
                    CharactersOnline = charsOnline
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
                var charsOnline = TcpServer.Instance.GetCharsOnline().Select(x => new CharacterSnapshot(x)).ToArray();
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
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.AllMaps;
}