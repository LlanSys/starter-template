using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ST.Infrastructure.Logging;

/// <summary>
/// A Serilog destructuring policy that intercepts objects being logged
/// and masks sensitive Personally Identifiable Information (PII) properties.
/// </summary>
public class PiiDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Security & Auth
        "Password",
        "Token",
        "Secret",
        "ClientSecret",
        "MfaCode",
        "SecurityStamp",
        "RecoveryCode",
        "RefreshToken",
        "AccessToken",

        // Identifiers
        "TINNumber",
        "NationalId",
        "Ssn",
        "SocialSecurityNumber",

        // Contact Information
        "Email",
        "EmailAddress",
        "Mobile",
        "Phone",
        "PhoneNumber",
        "PhoneHome",
        "PhoneWork",
        "FaxNo",
        "MobileNumber"
    };

    private const string MaskedValue = "***REDACTED***";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (propertyValueFactory == null) throw new ArgumentNullException(nameof(propertyValueFactory));
        var type = value.GetType();

        // Let Serilog handle primitive types and strings natively
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.IsEnum)
        {
            result = null;
            return false;
        }

        // Only destructure complex objects that have properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        var logProperties = new List<LogEventProperty>();

        bool hasSensitiveProperties = false;

        foreach (var propertyInfo in properties)
        {
            if (SensitivePropertyNames.Contains(propertyInfo.Name))
            {
                hasSensitiveProperties = true;
                logProperties.Add(new LogEventProperty(propertyInfo.Name, new ScalarValue(MaskedValue)));
            }
            else
            {
                try
                {
                    var propValue = propertyInfo.GetValue(value);
                    logProperties.Add(new LogEventProperty(propertyInfo.Name, propertyValueFactory.CreatePropertyValue(propValue, true)));
                }
                catch
                {
                    // If we can't read the property, just ignore it for logging
                }
            }
        }

        // If the object doesn't have any sensitive properties, let Serilog use its default destructuring.
        // This is important for performance and proper recursive serialization.
        if (!hasSensitiveProperties)
        {
            result = null;
            return false;
        }

        result = new StructureValue(logProperties, type.Name);
        return true;
    }
}
