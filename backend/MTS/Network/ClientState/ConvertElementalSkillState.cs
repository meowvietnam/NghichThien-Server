using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class ConvertElementalSkillState : IClientState
{
    private readonly Client client;

    public ConvertElementalSkillState(Client client)
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
        if (tag == MessageTag.ConvertElementalSkillRequest)
        {
            var request = reader.MemRead<ConvertElementalSkill>();
            ConvertElementalSkillResult result = null!;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.ConvertElementalSkill(request);
                }
            }
            catch (InvalidException e)
            {
                result = new ConvertElementalSkillResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ConvertElementalSkillRequest, result);
            }
        }
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        
    }
    public void OnLeave()
    {
    }
    public MessageState State => MessageState.ConvertElementalSkill;
}