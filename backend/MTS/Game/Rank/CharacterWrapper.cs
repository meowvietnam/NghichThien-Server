using MemoryPack;
using MTS.Models;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class CharacterWrapper
{
    [MemoryPackIgnore]
    public CharacterData Character { get; private set; }
    public byte[] CharacterBytes { get; private set; }

    public CharacterWrapper(CharacterData character)
    {
        Character = character;
        var writer = new Writer(character.ByteSize());
        writer.Write(Character);
        CharacterBytes = writer.Buffer;
    }
    [MemoryPackConstructor]
    public CharacterWrapper(byte[] characterBytes)
    {
        CharacterBytes = characterBytes;
        var reader = new Reader(characterBytes);
        Character = reader.Read<CharacterData>();
    }
}