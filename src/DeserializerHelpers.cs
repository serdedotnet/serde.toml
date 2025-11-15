using System;

namespace Serde.Toml;

internal static class DeserializerHelpers
{
    public static InvalidOperationException TypeMismatchException(string expectedType, object? actualValue)
    {
        return new InvalidOperationException($"Expected {expectedType}, got {actualValue?.GetType()}");
    }
}
