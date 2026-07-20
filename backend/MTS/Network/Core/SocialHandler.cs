using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.Core
{
    public class SocialHandler : IClientHandler
    {
        private Client client;
        private static MessageTag[] handler =
        [
              MessageTag.ThinhGiaoKyNang , MessageTag.TangQuaNpc , MessageTag.Gacha
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
            if (tag == MessageTag.ThinhGiaoKyNang)
            {
                var request = reader.Read<ThinhGiaoKyNang>();
                ThinhGiaoKyNangResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.ThinhGiaoKyNang(request);
                    }
                }
                catch (InvalidException e)
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
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.TangQuaNpc(request);
                    }
                }
                catch (InvalidException e)
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
            else if (tag == MessageTag.Gacha)
            {
                var request = reader.Read<GachaModel>();
                GachaResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.Gacha(request);
                    }
                }
                catch (InvalidException e)
                {

                    result = new GachaResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.GachaResult, result);
                }
            }
        }

        public void OnLeave()
        {
        }
    }

}
