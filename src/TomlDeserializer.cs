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
    private readonly TomlTable _currentValue;

    internal TomlDeserializer(TomlTable value)
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
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public char ReadChar()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public byte ReadU8()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public ushort ReadU16()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public uint ReadU32()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public ulong ReadU64()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public sbyte ReadI8()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public short ReadI16()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public int ReadI32()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public long ReadI64()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public float ReadF32()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public double ReadF64()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public decimal ReadDecimal()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public string ReadString()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public DateTime ReadDateTime()
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        throw new NotSupportedException("TomlDeserializer can only deserialize structured types (tables). Primitive values are handled by TableDeserializer, ListDeserializer, or DictionaryDeserializer.");
    }

    public ITypeDeserializer ReadType(ISerdeInfo typeInfo)
    {
        return typeInfo.Kind switch
        {
            InfoKind.List => throw new InvalidOperationException("Lists should not be read from root TomlDeserializer"),
            InfoKind.Dictionary => new DictionaryDeserializer(_currentValue),
            InfoKind.CustomType => new TableDeserializer(_currentValue),
            InfoKind.Nullable => new TableDeserializer(_currentValue),
            InfoKind.Enum => new TableDeserializer(_currentValue),
            _ => throw new ArgumentException($"Unsupported type kind: {typeInfo.Kind}")
        };
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
