using System.IO;
using BulletHell.Simulation.Core;

public static class InputFrameSerializer
{
    public static void Write(BinaryWriter writer, InputFrame input)
    {
        writer.Write(input.Tick);
        writer.Write(input.MoveX);
        writer.Write(input.MoveY);
        writer.Write(input.AimX);
        writer.Write(input.AimY);
        writer.Write((byte)(input.FirePressed ? 1 : 0));
    }

    public static InputFrame Read(BinaryReader reader)
    {
        int tick = reader.ReadInt32();
        sbyte moveX = reader.ReadSByte();
        sbyte moveY = reader.ReadSByte();
        short aimX = reader.ReadInt16();
        short aimY = reader.ReadInt16();
        byte firePressed = reader.ReadByte();
        return new InputFrame(tick, moveX, moveY, aimX, aimY, firePressed == 1);
    }
}
