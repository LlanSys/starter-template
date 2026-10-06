using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using ST.SharedKernel.Features.Shared.Lookups.Dtos;

namespace ST.SharedKernel.Extensions;

public static class EnumExtensions
{
    public static string ToDisplayString(this Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.GetType()
                .GetField(value.ToString())
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description ?? value.ToString();
    }

    public static T ToEnum<T>(this string description) where T : struct, Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();

            // Check if the Description matches the input string
            if (attribute != null && attribute.Description == description)
            {
                return (T)field.GetValue(null)!;
            }

            // Fallback: Check if the name of the enum member itself matches
            if (field.Name == description)
            {
                return (T)field.GetValue(null)!;
            }
        }

        throw new ArgumentException($"No enum member with description '{description}' found in {typeof(T).Name}");
    }

    public static Collection<LookupResponse> ToLookupResponses<T>() where T : struct, Enum
    {
        return [.. Enum.GetValues<T>().Select(static e => new LookupResponse(
            Convert.ToInt32(e, CultureInfo.InvariantCulture),
            e.ToString(),
            e.ToDisplayString()

        ))];
    }

    public static IReadOnlyList<(T Value, string Display)> GetSelectList<T>()
        where T : struct, Enum
        => [.. Enum.GetValues<T>().Select(e => (e, e.ToDisplayString()))];
}
