namespace IsDccSharp.Core;

using System;
using System.IO;
using System.Text;

internal sealed class BinaryReaderHelper : IDisposable
{
    private readonly Stream stream;
    private readonly BinaryReader reader;

    public BinaryReaderHelper(Stream stream)
    {
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
    }

    public long Tell => stream.Position;

    public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin) => stream.Seek(offset, origin);

    public void Skip(long count) => Seek(Tell + count);

    public byte Get1Byte() => reader.ReadByte();

    public ushort Get2Byte()
    {
        var b0 = reader.ReadByte();
        var b1 = reader.ReadByte();
        return (ushort)(b0 | (b1 << 8));
    }

    public uint Get4Byte()
    {
        var b0 = reader.ReadByte();
        var b1 = reader.ReadByte();
        var b2 = reader.ReadByte();
        var b3 = reader.ReadByte();
        return (uint)b0 | ((uint)b1 << 8) | ((uint)b2 << 16) | ((uint)b3 << 24);
    }

    public string GetString(int length)
    {
        if (length <= 0)
            return string.Empty;

        return Encoding.ASCII.GetString(ReadExact(length));
    }

    public void Dispose() => reader.Dispose();

    private byte[] ReadExact(int count)
    {
        var bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
            throw new EndOfStreamException($"Expected {count} byte(s), but only {bytes.Length} could be read.");

        return bytes;
    }
}
