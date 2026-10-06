using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ST.SharedKernel.Dtos.Utilities;

public class NullableEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            // Try exact match first
            if (Enum.TryParse<T>(stringValue, true, out var exactResult))
            {
                return exactResult;
            }

            // Try with underscores replaced with spaces
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

            // Log and return null instead of throwing
            Console.WriteLine($"Warning: Could not parse nullable enum value '{stringValue}' for type {typeof(T).Name}. Using null.");
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var numValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(T), numValue))
            {
                return (T)Enum.ToObject(typeof(T), numValue);
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteNullValue();
        }
    }
}
