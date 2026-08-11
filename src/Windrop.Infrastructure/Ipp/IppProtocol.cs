using System.Buffers.Binary;
using System.Text;

namespace Windrop.Infrastructure.Ipp;

public static class IppOperations
{
    public const ushort PrintJob = 0x0002;
    public const ushort ValidateJob = 0x0004;
    public const ushort GetJobAttributes = 0x0009;
    public const ushort GetJobs = 0x000a;
    public const ushort GetPrinterAttributes = 0x000b;
}

public sealed record IppRequest(byte Major, byte Minor, ushort OperationId, int RequestId,
    IReadOnlyDictionary<string, IReadOnlyList<object>> Attributes, int DocumentOffset)
{
    public string? StringAttribute(string name) =>
        Attributes.TryGetValue(name, out var values) ? values.OfType<string>().FirstOrDefault() : null;
}

public static class IppParser
{
    public static IppRequest Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 9) throw new InvalidDataException("IPP message is too short.");
        var major = bytes[0];
        var minor = bytes[1];
        var operation = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..4]);
        var requestId = BinaryPrimitives.ReadInt32BigEndian(bytes[4..8]);
        var offset = 8;
        var currentName = string.Empty;
        var values = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);

        while (offset < bytes.Length)
        {
            var tag = bytes[offset++];
            if (tag == 0x03) break;
            if (tag is >= 0x01 and <= 0x0f) continue;
            Ensure(bytes, offset, 2);
            var nameLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..(offset + 2)]);
            offset += 2;
            Ensure(bytes, offset, nameLength + 2);
            if (nameLength > 0)
            {
                currentName = Encoding.UTF8.GetString(bytes.Slice(offset, nameLength));
                offset += nameLength;
            }
            if (string.IsNullOrEmpty(currentName)) throw new InvalidDataException("IPP value has no attribute name.");
            var valueLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..(offset + 2)]);
            offset += 2;
            Ensure(bytes, offset, valueLength);
            var valueBytes = bytes.Slice(offset, valueLength);
            offset += valueLength;
            object value = tag switch
            {
                0x21 or 0x23 when valueLength == 4 => BinaryPrimitives.ReadInt32BigEndian(valueBytes),
                0x22 when valueLength == 1 => valueBytes[0] != 0,
                _ => Encoding.UTF8.GetString(valueBytes)
            };
            if (!values.TryGetValue(currentName, out var list)) values[currentName] = list = [];
            list.Add(value);
        }

        return new IppRequest(major, minor, operation, requestId,
            values.ToDictionary(x => x.Key, x => (IReadOnlyList<object>)x.Value, StringComparer.OrdinalIgnoreCase), offset);
    }

    private static void Ensure(ReadOnlySpan<byte> bytes, int offset, int count)
    {
        if (count < 0 || offset < 0 || offset + count > bytes.Length)
            throw new InvalidDataException("Truncated IPP attribute.");
    }
}

public sealed class IppResponseWriter(byte major, byte minor, ushort status, int requestId)
{
    private readonly MemoryStream _stream = CreateHeader(major, minor, status, requestId);

    public IppResponseWriter Group(byte tag) { _stream.WriteByte(tag); return this; }
    public IppResponseWriter Charset(string name, string value) => Value(0x47, name, value);
    public IppResponseWriter Language(string name, string value) => Value(0x48, name, value);
    public IppResponseWriter Name(string name, string value) => Value(0x42, name, value);
    public IppResponseWriter Text(string name, string value) => Value(0x41, name, value);
    public IppResponseWriter Keyword(string name, params string[] values) => Values(0x44, name, values);
    public IppResponseWriter Uri(string name, string value) => Value(0x45, name, value);
    public IppResponseWriter Mime(string name, params string[] values) => Values(0x49, name, values);
    public IppResponseWriter Integer(string name, int value) => BinaryValue(0x21, name, value);
    public IppResponseWriter Enum(string name, int value) => BinaryValue(0x23, name, value);
    public IppResponseWriter Enums(string name, params int[] values)
    {
        var first = true;
        Span<byte> data = stackalloc byte[4];
        foreach (var value in values)
        {
            BinaryPrimitives.WriteInt32BigEndian(data, value);
            WriteAttribute(0x23, first ? name : string.Empty, data);
            first = false;
        }
        return this;
    }
    public IppResponseWriter Boolean(string name, bool value)
    {
        WriteAttribute(0x22, name, [value ? (byte)1 : (byte)0]);
        return this;
    }

    public byte[] Build() { _stream.WriteByte(0x03); return _stream.ToArray(); }

    private IppResponseWriter Value(byte tag, string name, string value)
    {
        WriteAttribute(tag, name, Encoding.UTF8.GetBytes(value));
        return this;
    }

    private IppResponseWriter Values(byte tag, string name, IEnumerable<string> values)
    {
        var first = true;
        foreach (var value in values)
        {
            WriteAttribute(tag, first ? name : string.Empty, Encoding.UTF8.GetBytes(value));
            first = false;
        }
        return this;
    }

    private IppResponseWriter BinaryValue(byte tag, string name, int value)
    {
        Span<byte> data = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(data, value);
        WriteAttribute(tag, name, data);
        return this;
    }

    private void WriteAttribute(byte tag, string name, ReadOnlySpan<byte> value)
    {
        _stream.WriteByte(tag);
        WriteU16((ushort)Encoding.UTF8.GetByteCount(name));
        if (name.Length > 0) _stream.Write(Encoding.UTF8.GetBytes(name));
        WriteU16((ushort)value.Length);
        _stream.Write(value);
    }

    private void WriteU16(ushort value)
    {
        Span<byte> data = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(data, value);
        _stream.Write(data);
    }

    private static MemoryStream CreateHeader(byte major, byte minor, ushort status, int requestId)
    {
        var stream = new MemoryStream();
        stream.WriteByte(major is 1 or 2 ? major : (byte)2);
        stream.WriteByte(minor);
        Span<byte> header = stackalloc byte[6];
        BinaryPrimitives.WriteUInt16BigEndian(header[..2], status);
        BinaryPrimitives.WriteInt32BigEndian(header[2..], requestId);
        stream.Write(header);
        return stream;
    }
}
