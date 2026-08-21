using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class MonsterKillHandler : IClientHandler
{
    private Client client;

    public MonsterKillHandler(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
        
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader= message.Reader;
        if (tag == MessageTag.GetMonsterKillRequest)
        {
            var monsterKill = client.CharacterData.MonsterKill;
            client.MemSend(MessageTag.GetMonsterKillRequest, monsterKill);
        }
        else if (tag == MessageTag.ClaimMonsterKillRewardRequest)
        {
            ClaimMonsterKillRewardResult result = null;
            var id = reader.MemRead<long>();
            try
            {
                result = client.CharacterData.MonsterKill.Claim(id).GetAwaiter().GetResult();
            }
            catch (InvalidException e)
            {
                result = new ClaimMonsterKillRewardResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ClaimMonsterKillRewardRequest, result);
            }
        }
    }
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    private static MessageTag[] handlers = [MessageTag.GetMonsterKillRequest, MessageTag.ClaimMonsterKillRewardRequest];
    public MessageTag[] HandleTag => handlers;

}