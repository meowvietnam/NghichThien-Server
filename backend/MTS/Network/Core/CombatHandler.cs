using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class CombatHandler : IClientHandler
    {
        private Client client;
        private static MessageTag[] handler =
        [
              MessageTag.WinCombat
        ];
        public CombatHandler(Client client)
        {
            this.client = client;
        }
        public MessageTag[] HandleTag => handler;
        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            if (tag == MessageTag.WinCombat)
            {
                var request = reader.Read<WinCombat>();
                WinCombatResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.WinCombat(request);
                    }
                }
                catch (Exception e)
                {
                    result = new WinCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.WinCombatResult, result);
                }
            }
        }

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        public void OnEnter()
        {
        }

        public void OnLeave()
        {
        }
    }
}
