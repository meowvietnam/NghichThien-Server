using MTS.Game;
using MTS.Game.Rank;
using MTS.Network.Models;
using MTS.Services;

namespace MTS.Network.Core
{
    public class RankHandler : IClientHandler
    {
        private readonly Client client;

        public RankHandler(Client client)
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
            if (tag == MessageTag.GetRankRequest)
            {
                var rankType = reader.Read<RankType>();
                GetRankResult result = null;
                try
                {
                    var charData = client.CharacterData;
                    lock (charData)
                    {
                        IRank rank = null;
                        if (rankType == RankType.RankLevel)
                        {
                            var tops = TuViRankService.Tops;
                            var time = TuViRankService.GetDelayToGrantRewards();
                            rank = new RankLevel(tops, time);
                        }
                        else if (rankType == RankType.RankTongNap)
                        {
                            var tops = RechargeRankService.Tops;
                            var time = RechargeRankService.GetDelayToGrantRewards();
                            rank = new RankTongNap(tops, time, charData.GetTongNap().GetAwaiter().GetResult());
                        }
                        else if (rankType == RankType.RankTongTieu)
                        {
                            var tops = MoneyVipSpendingRankService.Tops;
                            var time = MoneyVipSpendingRankService.GetDelayToGrantRewards();
                            rank = new RankTongTieu(tops, time, charData.GetTongTieu().GetAwaiter().GetResult());
                        }
                        else if(rankType == RankType.RankArena)
                        {
                            var tops = ArenaRankService.Tops;
                            var time = ArenaRankService.GetDelayToGrantRewards();
                            rank = new RankArena(tops, time,GameConfig.TypeRankArenaWithValue, GameConfig.TypeRankArenaWithValue);
                        }
                        result = new GetRankResult()
                        {
                            Success = rank != null,
                            Rank = rank
                        };
                    }
                }
                catch (Exception e)
                {
                    result = new GetRankResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.GetRankRequest, result);
                }
            }
        }

        public void OnLeave()
        {
        }

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        private static MessageTag[] handler = { MessageTag.GetRankRequest };
        public MessageTag[] HandleTag => handler;
    }
}