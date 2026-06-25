using MTS.Network.Models;

namespace MTS.Network.Core
{
    public class SocialHandler : IClientHandler
    {
        private Client client;
        private static MessageTag[] handler =
        [
              MessageTag.ThinhGiaoKyNang , MessageTag.TangQuaNpc
        ];
        public SocialHandler(Client client)
        {
            this.client = client;
        }
        public MessageTag[] HandleTag => handler;

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        public void OnEnter()
        {
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            if (tag == MessageTag.ThinhGiaoKyNang)
            {
                var request = reader.Read<ThinhGiaoKyNang>();
                ThinhGiaoKyNangResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.ThinhGiaoKyNang(request);
                    }
                }
                catch (Exception e)
                {
                    result = new ThinhGiaoKyNangResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.ThinhGiaoKyNangResult, result);
                }
            }
            else if (tag == MessageTag.TangQuaNpc)
            {
                var request = reader.Read<TangQuaNpc>();
                TangQuaNpcResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.TangQuaNpc(request);
                    }
                }
                catch (Exception e)
                {

                    result = new TangQuaNpcResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.TangQuaNpcResult, result);
                }
            }    
            
        }

        public void OnLeave()
        {
        }
    }

}
