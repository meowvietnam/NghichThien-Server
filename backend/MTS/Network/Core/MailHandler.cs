using MTS.Network.Models;


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
            var request = reader.Read<GetMails>();
            GetMailsResult result = null;
            try
            {
                var mailManager = client.MailManager;
                lock (client.MailLock)
                {
                    lock (client.CharacterLock)
                    {
                        result = mailManager.GetMails(request.StartId, request.PageSize)
                            .GetAwaiter()
                            .GetResult();
                    }
                }
            }
            catch (Exception e)
            {
                result = new GetMailsResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.GetMailsResult, result);
            }
        }
        else if (tag == MessageTag.ReadMail)
        {
            var mailManager = client.MailManager;
            lock (client.MailLock)
            {
                var id = reader.Read<long>();
                var result = mailManager.ReadMail(id).GetAwaiter().GetResult();
                if (result == id)
                {
                    client.Send(MessageTag.MarkMailRead, id);
                }
            }
        }
        else if (tag == MessageTag.RemoveMail)
        {
            var mailManager = client.MailManager;
            lock (client.MailLock)
            {
                var id = reader.Read<long>();
                var result = mailManager.RemoveMail(id).GetAwaiter().GetResult();
                if (result == id)
                {
                    client.Send(MessageTag.RemoveMailId, id);
                }
            }
        }
        else if (tag == MessageTag.RemoveAllMails)
        {
            var mailManager = client.MailManager;
            lock (client.MailLock)
            {
                var result = mailManager.RemoveAllMails().GetAwaiter().GetResult();
                client.Send(MessageTag.RemoveAllMailsResult, result);
            }
        }
        else if (tag == MessageTag.ClaimMail)
        {
            var id = reader.Read<long>();
            var mailManager = client.MailManager;
            lock (client.MailLock)
            {
                lock (client.CharacterLock)
                {
                    var result = mailManager.ClaimMail(id).GetAwaiter().GetResult();
                    if (result == null) return;
                    client.Send(MessageTag.ClaimMailResult, result);
                }
            }
        }
        else if (tag == MessageTag.ClaimAllMails)
        {
            var mailManager = client.MailManager;
            lock (client.MailLock)
            {
                lock (client.CharacterLock)
                {
                    var result = mailManager.ClaimAllMails().GetAwaiter().GetResult();
                    if (result == null) return;
                    client.Send(MessageTag.ClaimAllMailsResult, result);
                }
            }
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