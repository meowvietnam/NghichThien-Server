using Microsoft.EntityFrameworkCore;
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
            MessageTag.ResultCombat, MessageTag.LoadCharCombat,
            MessageTag.LoadCharCombatArena
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
                        };
                        ;
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
                        var enemyData = LoadEnemyCombat().GetAwaiter().GetResult();
                        var enemy = enemyData == null ? null : new CharacterSnapshot(enemyData);
                        result = new LoadCharCombatResult()
                        {
                            Success = true,
                            Client = new CharacterSnapshot(charData),
                            Enemy = enemy
                        };
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

        private async Task<CharacterData> LoadEnemyCombat()
        {
            var point = client.CharacterData.ArenaManager.Points;
            var level = client.CharacterData.Progression.Level;
            var charId = client.CharacterData.Id;
            var min = point - 500;
            var max = point + 500;
            await using var db = await Program.CreateDbAsync();
            var matchPointPlayers =
                await db.ArenaManagers
                    .Where(x => x.Points >= min && x.Points <= max && x.CharId != charId)
                    .OrderBy(_ => EF.Functions.Random())
                    .Select(x => x.CharId)
                    .Take(10)
                    .ToListAsync();
            var matchPlayerProgression =
                await db.Progressions
                    .AsNoTracking()
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.EquipmentManager)
                    .ThenInclude(x => x.ThanThongEquips)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.EquipmentManager)
                    .ThenInclude(x => x.DanCombatUseds)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .ThenInclude(x => x.VuKhis)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .ThenInclude(x => x.PhongCus)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .ThenInclude(x => x.TrangSucs)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.InventoryManager)
                    .ThenInclude(x => x.NgoDaos)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.Stats)
                    .Include(x => x.CharacterData)
                    .ThenInclude(x => x.Appearance)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => matchPointPlayers.Contains(x.CharId) && x.Level >= level);
            if (matchPlayerProgression == null)
            {
                return null;
            }
            return matchPlayerProgression.CharacterData;
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