using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class SkillHandler : IClientHandler
{
    private Client client;
    private static MessageTag[] handler = { MessageTag.LearnNgoDao };
    public MessageTag[] HandleTag => handler;
    public SkillHandler(Client client)
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
        if (tag == MessageTag.LearnNgoDao)
        {
            var idNgoDao = reader.Read<int>();
            LearnNgoDaoResult result = null;
            try
            {
                var (ngoDao,buff) = client.CharacterData.LearnNgoDao(idNgoDao);
                result = new LearnNgoDaoResult()
                {
                    Success = true,
                    Buff = buff,
                    NgoDao = ngoDao
                };
            }
            catch (Exception e)
            {
                result = new LearnNgoDaoResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.LearnNgoDaoResult, result);
            }
        }
    }

    public void OnLeave()
    {
    }


    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}