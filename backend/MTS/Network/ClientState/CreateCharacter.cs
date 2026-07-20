using MTS.Exceptions;
using MTS.Game.CreateCharacter;
using MTS.Models;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class CreateCharacter : IClientState
{
    private Client client;
    private CreateCharacterService service;
    private readonly object serviceLock = new();
    private bool isCompleting;

    public CreateCharacter(Client client)
    {
        this.client = client;
        service = new CreateCharacterService();
    }

    public void OnEnter()
    {
    }

    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.BackToSelectChar)
        {
            client.Send(MessageTag.BackToSelectCharOk);
            lock (client.StateLock)
            {
                client.ChangeState(new SelectCharacter(client));
            }
        }
        else if (tag == MessageTag.SetCharName)
        {
            var name = reader.Read();
            try
            {
                lock (serviceLock)
                {
                    service.SetName(name).GetAwaiter().GetResult();
                }
                var result = new SetCharNameResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.SetCharNameResult, result);
            }
            catch (InvalidException ex)
            {
                var result = new SetCharNameResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.SetCharNameResult, result);
            }
        }
        else if (tag == MessageTag.SetSkins)
        {
            var request = reader.Read<SetSkinRequest>();
            try
            {
                lock (serviceLock)
                {
                    service.SetSkin(request);
                }
                var result = new SetSkinResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.SetSkinsResult, result);
            }
            catch (InvalidException ex)
            {
                var result = new SetSkinResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.SetSkinsResult, result);
            }
        }
        else if (tag == MessageTag.ApplyStats)
        {
            var models = reader.ReadArray<SelectionModel>().ToList();
            try
            {
                UserData userData;
                lock (serviceLock)
                {
                    if (isCompleting)
                    {
                        throw new InvalidException("Đang tạo nhân vật, vui lòng chờ");
                    }
                    service.ApplyStats(models);
                    isCompleting = true;
                }
                lock (client.StateLock)
                {
                    userData = client.UserData;
                }
                await service.Completed(userData);
                var result = new ApplyStatsResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.ApplyStatsResult, result);
                lock (client.StateLock)
                {
                    client.ChangeState(new SelectCharacter(client));
                }
            }
            catch (InvalidException ex)
            {
                lock (serviceLock)
                {
                    isCompleting = false;
                }
                var result = new ApplyStatsResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.ApplyStatsResult, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.CreateCharacter;
}
