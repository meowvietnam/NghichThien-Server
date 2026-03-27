using MTS.Game.CreateCharacter;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;
namespace MTS.Network.ClientState;

public class CreateCharacter : IClientState
{
    private Client client;
    private CreateCharacterService service;
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
            client.ChangeState(new SelectCharacter(client));
        }
        else if (tag == MessageTag.SetCharName)
        {
            var name = reader.Read();
            try
            {
                await service.SetName(name);
                var result = new SetCharNameResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.SetCharNameResult,result);
            }
            catch (Exception ex)
            {
                var result = new SetCharNameResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.SetCharNameResult,result);
            }
        }
        else if (tag == MessageTag.SetSkins)
        {
            var request = reader.Read<SetSkinRequest>();
            try
            {
                service.SetSkin(request);
                var result = new SetSkinResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.SetSkinsResult,result);
            }
            catch (Exception ex)
            {
                var result = new SetSkinResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.SetSkinsResult,result);
            }
        }
        else if (tag == MessageTag.ApplyStats)
        {
            var models = reader.ReadArray<SelectionModel>().ToList();
            try
            {
                service.ApplyStats(models);
                await service.Completed(client.UserData);
                var result = new ApplyStatsResult()
                {
                    Success = true,
                };
                client.Send(MessageTag.ApplyStatsResult,result);
                client.ChangeState(new SelectCharacter(client));
            }
            catch (Exception ex)
            {
                var result = new ApplyStatsResult()
                {
                    Success = false,
                    Message = ex.Message
                };
                client.Send(MessageTag.ApplyStatsResult,result);
            }
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.CreateCharacter;
}