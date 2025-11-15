using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serde;
using Tomlyn.Model;

namespace Serde.Toml;

/// <summary>
/// Implements IDeserializer for TOML format using Tomlyn library.
/// </summary>
public sealed class TomlDeserializer : IDeserializer
{
    private readonly object _currentValue;

    internal TomlDeserializer(object value)
    {
        _currentValue = value;
    }

    /// <summary>
    /// Deserialize from a TOML string.
    /// </summary>
    public static T Deserialize<T>(string tomlString, IDeserialize<T> deserialize)
    {
        TomlTable model = Tomlyn.Toml.ToModel(tomlString);
        var deserializer = new TomlDeserializer(model);
        var result = deserialize.Deserialize(deserializer);
        return result;
    }

    public static T Deserialize<T, TProvider>(string tomlString) where TProvider : IDeserializeProvider<T>
    {
        return Deserialize(tomlString, TProvider.Instance);
    }

    public static T Deserialize<T>(string tomlString) where T : IDeserializeProvider<T>
    {
        return Deserialize(tomlString, T.Instance);
    }

    public T? ReadNullableRef<T>(IDeserialize<T> deserialize) where T : class
    {
        return deserialize.Deserialize(this);
    }

    public bool ReadBool()
    {
        return _currentValue is bool b ? b : throw DeserializerHelpers.TypeMismatchException("bool", _currentValue);
    }

    public char ReadChar()
    {
        return _currentValue is string s && s.Length == 1 ? s[0] : throw DeserializerHelpers.TypeMismatchException("single character string", _currentValue);
    }

    public byte ReadU8()
    {
        return Convert.ToByte(ReadI64());
    }

    public ushort ReadU16()
    {
        return Convert.ToUInt16(ReadI64());
    }

    public uint ReadU32()
    {
        return Convert.ToUInt32(ReadI64());
    }

    public ulong ReadU64()
    {
        return Convert.ToUInt64(ReadI64());
    }

    public sbyte ReadI8()
    {
        return Convert.ToSByte(ReadI64());
    }

    public short ReadI16()
    {
        return Convert.ToInt16(ReadI64());
    }

    public int ReadI32()
    {
        return Convert.ToInt32(ReadI64());
    }

    public long ReadI64()
    {
        return _currentValue switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            _ => throw DeserializerHelpers.TypeMismatchException("integer", _currentValue)
        };
    }

    public float ReadF32()
    {
        return Convert.ToSingle(ReadF64());
    }

    public double ReadF64()
    {
        return _currentValue switch
        {
            double d => d,
            float f => f,
            long l => (double)l,
            int i => (double)i,
            _ => throw DeserializerHelpers.TypeMismatchException("float", _currentValue)
        };
    }

    public decimal ReadDecimal()
    {
        return Convert.ToDecimal(ReadF64());
    }

    public string ReadString()
    {
        return _currentValue is string s ? s : throw DeserializerHelpers.TypeMismatchException("string", _currentValue);
    }

    public DateTime ReadDateTime()
    {
        return _currentValue switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeOffset dto => dto.UtcDateTime,
            string s => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
            _ => throw DeserializerHelpers.TypeMismatchException("DateTime", _currentValue)
        };
    }

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        var s = ReadString();
        var bytes = Convert.FromBase64String(s);
        writer.Write(bytes);
    }

    public ITypeDeserializer ReadType(ISerdeInfo typeInfo)
    {
        return typeInfo.Kind switch
        {
            InfoKind.List => new ListDeserializer((TomlArray)_currentValue),
            InfoKind.Dictionary => new DictionaryDeserializer((TomlTable)_currentValue),
            InfoKind.CustomType => new TableDeserializer((TomlTable)_currentValue),
            InfoKind.Nullable => new TableDeserializer((TomlTable)_currentValue),
            InfoKind.Enum => new TableDeserializer((TomlTable)_currentValue),
            _ => throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}")
        };
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
