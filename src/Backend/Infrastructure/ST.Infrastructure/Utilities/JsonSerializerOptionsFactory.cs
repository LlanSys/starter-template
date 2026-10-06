using ST.Domain.Features.HR.Employees.Enums;
using ST.SharedKernel.Dtos.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ST.Infrastructure.Utilities;

public static class JsonSerializerOptionsFactory
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            AllowTrailingCommas = false,
            WriteIndented = true
        };

        options.Converters.Add(new EnumConverter<Gender>());
        options.Converters.Add(new NullableEnumConverter<Gender>());

        return options;
    }
}