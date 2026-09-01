using MTS.Exceptions;
using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class LuyenKhi : IClientState
{
    private Client client;
    private bool isLuyenKhi;
    private readonly object luyenKhiLock = new();

    public LuyenKhi(Client client)
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
        if (tag == MessageTag.LuyenKhi)
        {
            lock (luyenKhiLock)
            {
                if (isLuyenKhi) return;
                isLuyenKhi = true;
            }
            try
            {
                var request = reader.Read<LuyenKhiRequest>();
                var inventory = await client.GetInventory();
                LuyenKhiResult result;
                lock (client.CharacterLock)
                {
                    result = inventory.LuyenKhi(request);
                }

                client.Send(MessageTag.LuyenKhiResult, result);
            }
            catch (InvalidException e)
            {
                client.Send(MessageTag.LuyenKhiResult, new LuyenKhiResult
                {
                    Success = false,
                    Message = e.Message,
                });
            }
            finally
            {
                lock (luyenKhiLock)
                {
                    isLuyenKhi = false;
                }
            }
        }

        if (tag == MessageTag.SetEquipmentName)
        {
            var request = reader.Read<SetEquipmentName>();
            var inventory = await client.GetInventory();
            SetEquipmentNameResult result;
            lock (client.CharacterLock)
            {
                result = inventory.SetEquipmentName(request);
            }

            client.Send(MessageTag.SetEquipmentNameResult, result);
        }
        else if (tag == MessageTag.RandomLuyenKhiRequest)
        {
            var request = reader.MemRead<RandomLuyenKhiRequest>();
            lock (client.CharacterLock)
            {
                var result = RandomLuyenKhi(request);
                client.MemSend(MessageTag.RandomLuyenKhiRequest, result);
            }
        }
    }

    private RandomLuyenKhiResult RandomLuyenKhi(RandomLuyenKhiRequest request)
    {
        var inventory = client.CharacterData.InventoryManager;
        var requestCount = request.Count;
        var type = request.SelectedType;
        requestCount = Math.Clamp(requestCount, 1, 100);
        var tiers = request.Tiers;
        var actualCount = 0;
        var materialResources = LuyenKhiResources.Instance.Materials
            .Where(x => tiers.Contains(x.Tier))
            .Select(x => x.Id)
            .ToHashSet();
        lock (inventory.SyncRoot)
        {
            try
            {
                for (int i = 0; i < requestCount; i++)
                {
                    var materials = inventory.Materials
                        .Where(x => materialResources.Contains(x.Id))
                        .OrderBy(x => Random.Shared.Next())
                        .ToArray();
                    var selectMaterials = new List<ItemRef>();
                    var count = 0;
                    for (int j = 0; j < materials.Length; j++)
                    {
                        var material = materials[j];
                        var target = Math.Min(3, (int)material.Count);
                        var take = Random.Shared.Next(1, target + 1);
                        take = Math.Min(10 - count, take);
                        count += take;
                        selectMaterials.Add(new ItemRef()
                        {
                            Id = material.Id,
                            Count = (ushort)take,
                            Type = ItemType.Material
                        });
                        if (count == 10)
                        {
                            break;
                        }
                    }
                    if (count != 10)
                    {
                        break;
                    }

                    var ids = selectMaterials
                        .SelectMany(x => Enumerable.Repeat(x.Id, x.Count))
                        .ToArray();
                    var luyenKhiRequest = new LuyenKhiRequest()
                    {
                        MaterialIds = ids,
                        SelectedType = type
                    };
                    inventory.LuyenKhi(luyenKhiRequest);
                    actualCount++;
                }
            }
            catch (InvalidException e)
            {
            }
        }

        return new RandomLuyenKhiResult()
        {
            Success = actualCount > 0,
            Message = $"Bạn đã luyện thành công {actualCount} lần",
            TuoiTac = client.CharacterData.Stats.TuoiTac
        };
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.LuyenKhi;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}