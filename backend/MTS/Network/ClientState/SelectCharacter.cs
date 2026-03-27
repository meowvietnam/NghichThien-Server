using MTS.Network.Models;
using Network;
using Network.Extensions;
namespace MTS.Network.ClientState
{
    public class SelectCharacter : IClientState
    {
        private Client client;
        private const string LimitCharacter = "Số lượng nhân vật đã đạt giới hạn!";
        private const string CharacterNotFound = "Nhân vật không tồn tại!";
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
                        Message = LimitCharacter,
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
                        throw new(CharacterNotFound);
                    }
                    character = await client.Select(character);
                    var result = new SelectCharacterResult()
                    {
                        Success = true,
                        CharacterData = character,
                    };
                    client.Send(MessageTag.SelectCharacterResult, result);
                    client.ChangeState(new AllMaps(client));
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