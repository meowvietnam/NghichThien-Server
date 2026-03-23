using MTS.Network.Models;
using Network;
using Network.Extensions;
namespace MTS.Network.ClientState
{
    public class SelectCharacter : IClientState
    {
        private Client client;
        private const string LimitCharacter = "Số lượng nhân vật đã đạt giới hạn!";
        public SelectCharacter(Client client)
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
            if (tag == MessageTag.GetCharacters)
            {
                var chars = client.UserData.CharacterDatas;
                client.Send(MessageTag.GetCharactersResult, chars);
            }
            else if (tag == MessageTag.CanCreateCharacter)
            {
                var charCount = client.UserData.CharacterDatas.Count;
                if (charCount >= 3)
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
        }

        public void OnLeave()
        {
        }

        public MessageState State => MessageState.SelectCharacter;
    }
}