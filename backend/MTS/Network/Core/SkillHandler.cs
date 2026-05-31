using MTS.Network.Models;
using Network;
using Network.Extensions;
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.

namespace MTS.Network.Core;

public class SkillHandler : IClientHandler
{
    private Client client;

    private static MessageTag[] handler =
        { MessageTag.LearnNgoDao, MessageTag.EquipThanThong, MessageTag.UnequipThanThong,  MessageTag.EquipCongPhap, MessageTag.UnequipCongPhap };

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
                result = client.CharacterData.LearnNgoDao(idNgoDao);
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
        else if (tag == MessageTag.EquipThanThong)
        {
            var request = reader.Read<EquipThanThong>();
            EquipThanThongResult result = null!;
            try
            {
                result = client.CharacterData.EquipThanThong(request);
            }
            catch (Exception e)
            {
                result = new EquipThanThongResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.EquipThanThongResult, result);
            }
        }
        else if (tag == MessageTag.UnequipThanThong)
        {
            var id = reader.Read<int>();
            UnequipThanThongResult result = null!;
            try
            {
                result = client.CharacterData.UnequipThanThong((ushort)id);
            }
            catch (Exception e)
            {
                result = new UnequipThanThongResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.UnequipThanThongResult, result);
            }
        }
        else if (tag == MessageTag.EquipCongPhap)
        {
            var request = reader.Read<EquipCongPhap>();
            var charData = client.CharacterData;
            EquipCongPhapResult result = null;
            try
            {
                result = charData.EquipCongPhap(request);
            }
            catch (Exception e)
            {
                result = new EquipCongPhapResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.EquipCongPhapResult, result);
            }
        }
        else if (tag == MessageTag.UnequipCongPhap)
        {
            var slot = reader.Read<ETypeSlotCP>();
            var charData = client.CharacterData;
            UnequipCongPhapResult result = null;
            try
            {
                result = charData.UnequipCongPhap(slot);
            }
            catch (Exception e)
            {
                result = new UnequipCongPhapResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.UnequipCongPhapResult, result);
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