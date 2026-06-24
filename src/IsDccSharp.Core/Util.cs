namespace IsDccSharp.Core;

using System;
using System.Text;

internal static class Util
{
    private const int XorValue = 0xF1;

    public static string FilterLF(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c != '\r' && c != '\n')
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static void Error(string message) => throw new InvalidOperationException(message);

    // InstallShield INX obfuscation uses a position-dependent byte transform.
    public static byte[] Scramble(byte[] buffer)
    {
        var xbuffer = new byte[buffer.Length];

        for (var j = 0; j < buffer.Length; j++)
        {
            int c = buffer[j];
            c += (j % 71);
            c = ((c << 2) & 0xFF) | ((c >> 6) & 0x03);
            c ^= XorValue;
            xbuffer[j] = (byte)c;
        }
        return xbuffer;
    }

    public static byte[] UnScramble(byte[] buffer)
    {
        var xbuffer = new byte[buffer.Length];

        for (var j = 0; j < buffer.Length; j++)
        {
            var c = buffer[j];
            c ^= XorValue;

            var b = ((c >> 2) | (c << 6)) & 0xFF;
            b = (b - (j % 71)) & 0xFF;
            xbuffer[j] = (byte)b;
        }

        return xbuffer;
    }
}
