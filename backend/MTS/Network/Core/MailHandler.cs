using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class MailHandler : IClientHandler
{
    private Client client;
    public MailHandler(Client client)
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
        if (tag == MessageTag.GetMails)
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
        else if (tag == MessageTag.ClaimMail)
        {
            var id = reader.Read<int>();
            var result = await client.ClaimMail(id);
            if (result == null) return;
            client.Send(MessageTag.ClaimMailResult, result);
        }
        else if (tag == MessageTag.ClaimAllMails)
        {
            var result = await client.ClaimAllMails();
            if (result == null) return;
            client.Send(MessageTag.ClaimAllMailsResult, result);
        }
    }

    public void OnLeave()
    {
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    private static readonly MessageTag[] serverHandler =
    {
        MessageTag.GetMails, MessageTag.ReadMail, MessageTag.RemoveMail,
        MessageTag.RemoveAllMails,
        MessageTag.ClaimMail, MessageTag.ClaimAllMails,
    };

    public MessageTag[] HandleTag => serverHandler;
}