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
    private bool isLockMail;

    public AllMaps(Client client)
    {
        this.client = client;
    }

    public void OnEnter()
    {
    }

    private CharacterSnapshot[] GetCharsOnlineThisNode()
    {
        var currentNodeId = client.CharacterData.IdCurrentNode;
        var charId = client.CharacterData.Id;
        var charsOnline = TcpServer.Instance.GetCharsOnline()
            .Where(x => x.IdCurrentNode == currentNodeId && x.Id != charId)
            .Select(x => new CharacterSnapshot(x))
            .Take(5)
            .ToArray();
        return charsOnline;
    }

    public async void Handle(Message<MessageTag> message)
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

                var stats = charData.Stats;
                var dayUsed = (int)Math.Round(32f * path.Count / stats.TocDoBay, MidpointRounding.AwayFromZero);
                var newAge = stats.TuoiTac + new TimeSpan(dayUsed, 0, 0, 0);
                stats.TuoiTac = newAge;
                charData.IdCurrentNode = id;
                charData.CurrentEnergy = newEnergy;
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
        else if (tag == MessageTag.GetMails)
        {
            try
            {
                var mails = await client.GetMails();
                var result = new GetMailsResult()
                {
                    Success = true,
                    Mails = mails
                };
                client.Send(MessageTag.GetMailsResult, result);
            }
            catch (Exception e)
            {
                var result = new GetMailsResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.GetMailsResult, result);
            }
        }
        else if (tag == MessageTag.ReadMail)
        {
            var id = reader.Read<int>();
            var result = client.ReadMail(id);
            if (result == id)
            {
                client.Send(MessageTag.MarkMailRead, id);
            }
        }
        else if (tag == MessageTag.RemoveMail)
        {
            var id = reader.Read<int>();
            var result = client.RemoveMail(id);
            if (result == id)
            {
                client.Send(MessageTag.RemoveMailId, id);
            }
        }
        else if (tag == MessageTag.RemoveAllMails)
        {
            var result = client.RemoveAllMails();
            client.Send(MessageTag.RemoveAllMailsResult, result);
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.AllMaps;
}