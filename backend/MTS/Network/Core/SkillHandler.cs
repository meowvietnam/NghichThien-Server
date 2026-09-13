using MTS.Exceptions;
using System.Diagnostics;
using MTS.Network.Models;
using Network;
using Network.Extensions;

#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match constraint type.

namespace MTS.Network.Core;

public class SkillHandler : IClientHandler
{
    private Client client;

    private static MessageTag[] handler =
    [
        MessageTag.LearnNgoDao, MessageTag.EquipThanThong, MessageTag.UnequipThanThong,
        MessageTag.EquipCongPhap, MessageTag.UnequipCongPhap, MessageTag.TuLuyen, MessageTag.LinhNgo,
        MessageTag.DotPhaCongPhap, MessageTag.UseDanRequest, MessageTag.ResetNgoDaoRequest
    ];

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
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.LearnNgoDao(idNgoDao);
                }
            }
            catch (InvalidException e)
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
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.EquipThanThong(request);
                }
            }
            catch (InvalidException e)
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
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.UnequipThanThong((ushort)id);
                }
            }
            catch (InvalidException e)
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
            EquipCongPhapResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.EquipCongPhap(request);
                }
            }
            catch (InvalidException e)
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
            UnequipCongPhapResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.UnequipCongPhap(slot);
                }
            }
            catch (InvalidException e)
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
        else if (tag == MessageTag.TuLuyen)
        {
            var time = reader.Read<TimeSpan>();
            TuLuyenResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.TuLuyen(time);
                }
            }
            catch (InvalidException e)
            {
                result = new TuLuyenResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.TuLuyenResult, result);
            }
        }
        else if (tag == MessageTag.LinhNgo)
        {
            var request = reader.Read<LinhNgo>();
            LinhNgoResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.LinhNgo(request);
                }
            }
            catch (InvalidException e)
            {
                result = new LinhNgoResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.LinhNgoResult, result);
            }
        }
        else if (tag == MessageTag.DotPhaCongPhap)
        {
            var request = reader.Read<int>();
            DotPhaCongPhapResult result = null!;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.DotPhaCongPhap(request);
                }
            }
            catch (InvalidException e)
            {
                result = new DotPhaCongPhapResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.DotPhaCongPhapResult, result);
            }
        }    
        else if (tag == MessageTag.UseDanRequest)
        {
            var request = reader.MemRead<UseDanRequest>();
            UseDanResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.UseDan(request);
                }
            }
            catch (InvalidException e)
            {
                result = new UseDanResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.UseDanRequest, result);
            }
        }
        else if (tag == MessageTag.ResetNgoDaoRequest)
        {
            ResetNgoDaoResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.ResetNgoDao();
                }
            }
            catch (InvalidException e)
            {
                result = new ResetNgoDaoResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ResetNgoDaoRequest, result);
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
