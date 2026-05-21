using System.IO;
using BulletHell.Network;
using BulletHell.Simulation.Core;

public static class PacketCodec
{
    private const int InputFrameSize = sizeof(int) + sizeof(sbyte) + sizeof(sbyte) + sizeof(short) + sizeof(short) + sizeof(byte);

    public static byte[] EncodeHello(ushort seq, int clientId, uint clientRandom)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        PacketHeader.Write(writer, new PacketHeader(NetMessageType.Hello, seq, clientId));
        writer.Write(clientRandom);
        return ms.ToArray();
    }

    public static byte[] EncodeWelcome(ushort seq, int clientId, byte playerId, byte playerCount, int startTick, uint seed)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        PacketHeader.Write(writer, new PacketHeader(NetMessageType.Welcome, seq, clientId));
        writer.Write(playerId);
        writer.Write(playerCount);
        writer.Write(startTick);
        writer.Write(seed);
        return ms.ToArray();
    }

    public static byte[] EncodeInput(ushort seq, int clientId, byte playerId, InputFrame input)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        PacketHeader.Write(writer, new PacketHeader(NetMessageType.Input, seq, clientId));
        writer.Write(playerId);
        InputFrameSerializer.Write(writer, input);
        return ms.ToArray();
    }

    public static byte[] EncodeFrame(ushort seq, int clientId, int tick, InputFrame p1, InputFrame p2)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        PacketHeader.Write(writer, new PacketHeader(NetMessageType.Frame, seq, clientId));
        writer.Write(tick);
        InputFrameSerializer.Write(writer, p1);
        InputFrameSerializer.Write(writer, p2);
        return ms.ToArray();
    }

    public static bool TryDecodeHeader(byte[] data, out PacketHeader header)
    {
        header = default;
        if (data == null || data.Length < PacketHeader.HeaderSize)
            return false;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        if (!PacketHeader.TryRead(reader, out header))
            return false;
        return true;
    }

    public static bool TryDecodeHello(byte[] data, PacketHeader header, out uint clientRandom)
    {
        clientRandom = 0;
        int expectedSize = PacketHeader.HeaderSize + sizeof(uint);
        if (!HasExpectedSize(data, expectedSize) || header.Type != NetMessageType.Hello || !header.IsValid)
            return false;
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        reader.BaseStream.Seek(PacketHeader.HeaderSize, SeekOrigin.Begin);
        clientRandom = reader.ReadUInt32();
        return true;
    }

    public static bool TryDecodeWelcome(byte[] data, PacketHeader header, out byte playerId, out byte playerCount, out int startTick, out uint seed)
    {
        playerId = 0;
        playerCount = 0;
        startTick = 0;
        seed = 0;
        int expectedSize = PacketHeader.HeaderSize + sizeof(byte) + sizeof(byte) + sizeof(int) + sizeof(uint);
        if (!HasExpectedSize(data, expectedSize) || header.Type != NetMessageType.Welcome || !header.IsValid)
            return false;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        reader.BaseStream.Seek(PacketHeader.HeaderSize, SeekOrigin.Begin);
        playerId = reader.ReadByte();
        playerCount = reader.ReadByte();
        startTick = reader.ReadInt32();
        seed = reader.ReadUInt32();
        return true;
    }

    public static bool TryDecodeInput(byte[] data, PacketHeader header, out byte playerId, out InputFrame input)
    {
        playerId = 0;
        input = default;
        int expectedSize = PacketHeader.HeaderSize + sizeof(byte) + InputFrameSize;
        if (!HasExpectedSize(data, expectedSize) || header.Type != NetMessageType.Input || !header.IsValid)
            return false;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        reader.BaseStream.Seek(PacketHeader.HeaderSize, SeekOrigin.Begin);
        playerId = reader.ReadByte();
        input = InputFrameSerializer.Read(reader);
        return true;
    }

    public static bool TryDecodeFrame(byte[] data, PacketHeader header, out int tick, out InputFrame p1, out InputFrame p2)
    {
        tick = 0;
        p1 = default;
        p2 = default;
        int expectedSize = PacketHeader.HeaderSize + sizeof(int) + InputFrameSize + InputFrameSize;
        if (!HasExpectedSize(data, expectedSize) || header.Type != NetMessageType.Frame || !header.IsValid)
            return false;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        reader.BaseStream.Seek(PacketHeader.HeaderSize, SeekOrigin.Begin);
        tick = reader.ReadInt32();
        p1 = InputFrameSerializer.Read(reader);
        p2 = InputFrameSerializer.Read(reader);
        return true;
    }

    private static bool HasExpectedSize(byte[] data, int expectedSize)
    {
        return data != null && data.Length == expectedSize;
    }
}
