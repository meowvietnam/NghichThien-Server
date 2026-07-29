using MTS.Exceptions;
using MTS.Game.Auction;
using MTS.Localization;
using MTS.Network.ClientState;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class SceneManager : IClientHandler
{
    private Client client;
    private const string AllMaps = "AllMaps";
    private const string LuyenDan = "LuyenDan";
    private const string LuyenKhi = "LuyenKhi";
    private const string ConvertElementalSkill = "ConvertElementalSkill";
    private const string Auction = "Auction";
    public SceneManager(Client client)
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
        if (tag == MessageTag.ChangeScene)
        {
            var sceneName = reader.Read();
            var result = new ChangeSceneResult();
            try
            {
                switch (sceneName)
                {
                    case AllMaps:
                        lock (client.StateLock)
                        {
                            client.ChangeState(new AllMaps(client));
                        }
                        result.Success = true;
                        result.SceneName = sceneName;
                        break;
                    case LuyenDan:
                        lock (client.StateLock)
                        {
                            client.ChangeState(new LuyenDan(client));
                        }
                        result.Success = true;
                        result.SceneName = sceneName;
                        break;
                    case LuyenKhi:
                        result.Success = true;
                        result.SceneName = sceneName;
                        lock (client.StateLock)
                        {
                            client.ChangeState(new LuyenKhi(client));
                        }
                        break;
                    case ConvertElementalSkill:
                        result.Success = true;
                        result.SceneName = sceneName;
                        lock (client.StateLock)
                        {
                            client.ChangeState(new ConvertElementalSkillState(client));
                        }
                        break;
                    case Auction:
                        var auctionManager = AuctionManager.Instance;
                        if (auctionManager != null)
                        {
                            var isOpen = auctionManager.ThisState == AuctionManager.State.Started;
                            if (isOpen)
                            {
                                result.Success = true;
                                result.SceneName = sceneName;
                                lock (client.StateLock)
                                {
                                    client.ChangeState(new AuctionState(client));
                                }
                            }
                            else
                            {
                                result.Success = false;
                                result.Message = Strings.AuctionNotOpen;
                            }
                        }
                        else
                        {
                            result.Success = false;
                            result.Message = Strings.AuctionServiceNotReady;
                        }
                        break;
                    default:
                        result.Success = true;
                        result.SceneName = sceneName;
                        break;
                }
            }
            catch (InvalidException e)
            {
                result.Success = false;
                result.Message = e.Message;
            }
            client.Send(MessageTag.ChangeSceneResult, result);
        }
    }

    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    private static readonly MessageTag[] handlers = [MessageTag.ChangeScene];
    public MessageTag[] HandleTag => handlers;
}
