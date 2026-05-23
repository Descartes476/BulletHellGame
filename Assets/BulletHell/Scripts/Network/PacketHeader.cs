using System.IO;

namespace BulletHell.Network
{
    public readonly struct PacketHeader
    {
        public const int HeaderSize = 10; // Magic0(1) + Magic1(1) + Version(1) + Type(1) + Sequence(2) + ClientId(4)
        public const byte MagicValue0 = (byte)'D';
        public const byte MagicValue1 = (byte)'K';
        public const byte ProtocolVersion = 1;

        public readonly byte Magic0;
        public readonly byte Magic1;
        public readonly byte Version;
        public readonly NetMessageType Type;
        public readonly ushort Sequence;
        public readonly int ClientId;

        public bool IsValid =>
            Magic0 == MagicValue0 &&
            Magic1 == MagicValue1 &&
            Version == ProtocolVersion &&
            IsKnownType(Type);

        public PacketHeader(NetMessageType type, ushort sequence, int clientId)
        {
            Magic0 = MagicValue0;
            Magic1 = MagicValue1;
            Version = ProtocolVersion;
            Type = type;
            Sequence = sequence;
            ClientId = clientId;
        }

        private PacketHeader(byte magic0, byte magic1, byte version, NetMessageType type, ushort sequence, int clientId)
        {
            Magic0 = magic0;
            Magic1 = magic1;
            Version = version;
            Type = type;
            Sequence = sequence;
            ClientId = clientId;
        }

        public static void Write(BinaryWriter writer, in PacketHeader header)
        {
            writer.Write(header.Magic0);
            writer.Write(header.Magic1);
            writer.Write(header.Version);
            writer.Write((byte)header.Type);
            writer.Write(header.Sequence);
            writer.Write(header.ClientId);
        }

        public static bool TryRead(BinaryReader reader, out PacketHeader header)
        {
            header = default;
            if (reader.BaseStream.Length - reader.BaseStream.Position < HeaderSize)
                return false;
            byte magic0 = reader.ReadByte();
            byte magic1 = reader.ReadByte();
            byte version = reader.ReadByte();
            byte type = reader.ReadByte();
            ushort sequence = reader.ReadUInt16();
            int clientId = reader.ReadInt32();

            header = new PacketHeader(magic0, magic1, version, (NetMessageType)type, sequence, clientId);
            return header.IsValid;
        }

        private static bool IsKnownType(NetMessageType type)
        {
            switch (type)
            {
                case NetMessageType.Hello:
                case NetMessageType.Welcome:
                case NetMessageType.Input:
                case NetMessageType.Frame:
                case NetMessageType.HashReport:
                    return true;
                default:
                    return false;
            }
        }
    }
}
