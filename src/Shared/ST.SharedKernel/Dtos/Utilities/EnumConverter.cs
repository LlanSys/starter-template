using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ST.SharedKernel.Dtos.Utilities;

public class EnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return default;
            }

            // Try exact match first
            if (Enum.TryParse<T>(stringValue, true, out var exactResult))
            {
                return exactResult;
            }

            // Try with underscores replaced with spaces (common BC pattern)
            var normalizedValue = stringValue.Replace("_", " ");
            if (Enum.TryParse<T>(normalizedValue, true, out var normalizedResult))
            {
                return normalizedResult;
            }

            // Try with spaces replaced with underscores
            var underscoreValue = stringValue.Replace(" ", "_");
            if (Enum.TryParse<T>(underscoreValue, true, out var underscoreResult))
            {
                return underscoreResult;
            }

            // Log the issue but don't throw - return default value
            Console.WriteLine($"Warning: Could not parse enum value '{stringValue}' for type {typeof(T).Name}. Using default value.");
            return default;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var numValue = reader.GetInt32();
            return Enum.IsDefined(typeof(T), numValue) ? (T)Enum.ToObject(typeof(T), numValue) : default;
        }

        // For null or other token types, return default
        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}
