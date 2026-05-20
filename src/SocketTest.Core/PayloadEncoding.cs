using System.Text;

namespace SocketTest.Core;

public enum PayloadEncoding
{
    Utf8,
    Windows1252,
    Ascii,
    Hex
}

public enum LineEnding
{
    None,
    Lf,
    CrLf
}

public static class PayloadEncodingExtensions
{
    static PayloadEncodingExtensions()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string Decode(this PayloadEncoding encoding, ReadOnlySpan<byte> bytes)
    {
        return encoding switch
        {
            PayloadEncoding.Utf8 => Encoding.UTF8.GetString(bytes),
            PayloadEncoding.Windows1252 => Encoding.GetEncoding(1252).GetString(bytes),
            PayloadEncoding.Ascii => Encoding.ASCII.GetString(bytes),
            PayloadEncoding.Hex => ToHexDump(bytes),
            _ => Encoding.UTF8.GetString(bytes)
        };
    }

    public static byte[] Encode(this PayloadEncoding encoding, string text)
    {
        return encoding switch
        {
            PayloadEncoding.Utf8 => Encoding.UTF8.GetBytes(text),
            PayloadEncoding.Windows1252 => Encoding.GetEncoding(1252).GetBytes(text),
            PayloadEncoding.Ascii => Encoding.ASCII.GetBytes(text),
            PayloadEncoding.Hex => FromHex(text),
            _ => Encoding.UTF8.GetBytes(text)
        };
    }

    public static string AppendLineEnding(this LineEnding ending, string text) => ending switch
    {
        LineEnding.None => text,
        LineEnding.Lf => text + "\n",
        LineEnding.CrLf => text + "\r\n",
        _ => text
    };

    private static string ToHexDump(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty) return string.Empty;
        var sb = new StringBuilder(bytes.Length * 3);
        for (var i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString("X2"));
            sb.Append(' ');
            if ((i + 1) % 16 == 0) sb.Append('\n');
        }
        return sb.ToString();
    }

    private static byte[] FromHex(string text)
    {
        var cleaned = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            cleaned.Append(c);
        }
        if (cleaned.Length % 2 != 0)
            throw new FormatException("Hex payload must have an even number of characters.");

        var bytes = new byte[cleaned.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(cleaned.ToString(i * 2, 2), 16);
        return bytes;
    }
}
