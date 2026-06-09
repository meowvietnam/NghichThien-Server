using MTS.Game;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Core;
using MTS.Network.Models;
using Network;
using Network.Extensions;
namespace MTS.Network.ClientState
{
    public class SelectCharacter : IClientState
    {
        private Client client;
        public SelectCharacter(Client client)
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
            if (tag == MessageTag.GetCharacters)
            {
                var chars = client.UserData.CharacterDatas;
                client.Send(MessageTag.GetCharactersResult, chars);
            }
            else if (tag == MessageTag.CanCreateCharacter)
            {
                var charCount = client.UserData.CharacterDatas.Count;
                if (charCount >= 1)
                {
                    var result = new CanCreateChar()
                    {
                        CanCreate = false,
                        Message = Strings.LimitCharacter,
                    };
                    client.Send(MessageTag.CanCreateCharacterResult, result);
                }
                else
                {
                    var result = new CanCreateChar()
                    {
                        CanCreate = true,
                    };
                    client.Send(MessageTag.CanCreateCharacterResult, result);
                    client.ChangeState(new CreateCharacter(client));
                }
            }
            else if (tag == MessageTag.SelectCharacter)
            {
                var id = reader.Read<int>();
                try
                {
                    var character = client.UserData.CharacterDatas.FirstOrDefault(x => x.Id == id);
                    if (character == null)
                    {
                        throw new(Strings.CharacterNotFound);
                    }
                    character = await client.Select(character);
                    var result = new SelectCharacterResult()
                    {
                        Success = true,
                        CharacterData = character,
                    };
                    client.Send(MessageTag.SelectCharacterResult, result);
                    var herbs = GameConfig.LuyenDanResources.HerbDatas;
                    var herb  = herbs[Random.Shared.Next(herbs.Count)];
                    var rewards = new List<ItemRef>
                    {
                        new()
                        {
                            Id = (ushort)herb.Id,
                            Type = ItemType.Herb,
                            Count = 1,
                        }
                    };
                    await client.MailManager.SendMail(new Mail()
                    {
                        Title = "Test mail",
                        Content = "Test mail Content",
                        Rewards =  rewards,
                    });

                    client.OnSelectedCharacter();
                }
                catch (Exception e)
                {
                    var result = new SelectCharacterResult()
                    {
                        Success = false,
                        Message = e.Message,
                    };
                    client.Send(MessageTag.SelectCharacterResult, result);
                }
            }
        }

        public void OnLeave()
        {
        }

        public MessageState State => MessageState.SelectCharacter;
    }
}