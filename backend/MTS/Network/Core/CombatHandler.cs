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
              MessageTag.ResultCombat , MessageTag.LoadCharCombat , MessageTag.LoadCharCombatArena
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
            if (tag == MessageTag.ResultCombat)
            {
                var request = reader.Read<ResultCombat>();
                ResultCombatResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.ResultCombat(request);
                    }
                }
                catch (Exception e)
                {
                    result = new ResultCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.ResultCombatResult, result);
                }
            }
            else if (tag == MessageTag.LoadCharCombat)
            {
                LoadCharCombatResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = new LoadCharCombatResult()
                        {
                            Success = true,
                            Client = new CharacterSnapshot(charData)
                        }; ;
                        charData.EquipmentManager.ClearDanCombat();

                    }
                }
                catch (Exception e)
                {
                    result = new LoadCharCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.LoadCharCombatResult, result);
                }
            }
            else if (tag == MessageTag.LoadCharCombatArena)
            {
                LoadCharCombatResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = new LoadCharCombatResult()
                        {
                            Success = true,
                            Client = new CharacterSnapshot(charData),
                            Enemy = new CharacterSnapshot(charData) //hiện tại test sau load tìm ra user khác có cùng hạng point nếu k tìm ra thì tìm trên nếu k ra thì tìm dưới k tìm được thì trả về null
                        }; ;
                    }
                }
                catch (Exception e)
                {
                    result = new LoadCharCombatResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.LoadCharCombatArenaResult, result);
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
