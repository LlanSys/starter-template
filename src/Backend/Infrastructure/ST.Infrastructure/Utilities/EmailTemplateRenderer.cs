using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ST.Infrastructure.Utilities;

public static class EmailTemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(template, nameof(template));
        ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));
        foreach (var (key, value) in parameters)
        {
            template = template.Replace(
                $"{{{{{key}}}}}",
                WebUtility.HtmlEncode(value));
        }

        return template;
    }
}
