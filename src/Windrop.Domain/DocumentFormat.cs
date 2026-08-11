namespace Windrop.Domain;

public static class DocumentFormat
{
    public static ReceivedItemKind Detect(ReadOnlySpan<byte> header, string? declaredFormat = null)
    {
        if (header.StartsWith("%PDF"u8)) return ReceivedItemKind.Pdf;
        if (header.Length >= 2 && header[0] == 0xff && header[1] == 0xd8) return ReceivedItemKind.Jpeg;
        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return ReceivedItemKind.Png;
        if (string.Equals(declaredFormat, "image/urf", StringComparison.OrdinalIgnoreCase)) return ReceivedItemKind.Urf;
        return ReceivedItemKind.Unknown;
    }

    public static string Extension(this ReceivedItemKind kind) => kind switch
    {
        ReceivedItemKind.Pdf => ".pdf",
        ReceivedItemKind.Jpeg => ".jpg",
        ReceivedItemKind.Png => ".png",
        ReceivedItemKind.Text => ".txt",
        ReceivedItemKind.Urf => ".urf",
        _ => ".bin"
    };
}
