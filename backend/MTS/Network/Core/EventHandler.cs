using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class EventHandler : IClientHandler
{
    private Client client;

    public EventHandler(Client client)
    {
        this.client = client;
    }

    public void OnEnter()
    {
    }

    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        var eventManager = client.CharacterData.EventManager;
        if (tag == MessageTag.HandleEventRequest)
        {
            var request = reader.MemRead<HandleEventRequest>();
            await using var db = await Program.CreateDbAsync();
            lock (client.CharacterData)
            {
                try
                {
                    var result = eventManager.HandleEvent(request.EventId, request.Data, db).GetAwaiter().GetResult();
                    client.MemSend(MessageTag.HandleEventRequest, result);
                }
                catch (InvalidException e)
                {
                }
            }
        }
        else if (tag == MessageTag.GetAllActiveEvents)
        {
            var events = eventManager.GetActiveEvents()
                .Where(x => !(x.GameEvent is RechargeEvent1))
                .Select(x => x.Id).ToArray();
            client.MemSend(MessageTag.GetAllActiveEvents, events);
        }
        else if (tag == MessageTag.GetEventRequest)
        {
            var id = reader.MemRead<ushort>();
            GetEventResult result = null;
            try
            {
                var @event = eventManager.GetEvent(id);
                result = new GetEventResult()
                {
                    Success = true,
                    Event = @event
                };
            }
            catch (InvalidException e)
            {
                result = new GetEventResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.GetEventRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handler = new[]
        { MessageTag.HandleEventRequest, MessageTag.GetAllActiveEvents, MessageTag.GetEventRequest };

    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}