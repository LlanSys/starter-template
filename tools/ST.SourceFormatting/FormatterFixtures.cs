namespace ST.SourceFormatting;

internal static class FormatterFixtures
{
    internal static void Run()
    {
        string[] samples =
        [
            "<MudText Title=\"@(flag?\"two words\":\"other value\")\" Class=\"x\">@(flag?\"Yes\":\"No\")</MudText>\r\n",
            "<AdminEditorModal Title=\"@($\"Edit {Get(\"two words\")}\")\" OnClose=\"@(() => Close(\"keep this space\"))\" />\r\n",
            "@inject ILogger<Component> Logger\r\n<div>@if (value is { } item){<MudText>@item.Name</MudText>}</div>\r\n",
            "<pre>  keep\r\n    these spaces</pre>\r\n<textarea name=\"message\">\r\n leave this alone </textarea>\r\n",
            "<script type=\"application/ld+json\">@((MarkupString)Json)</script>\r\n",
            "@foreach(var item in new[]{\"one\",\"two\"}){<MudText>@item</MudText>}\r\n",
            "<MudTd>@row.When.ToString(\"dd MMM 'UTC'\")</MudTd><MudTd>@row.Name</MudTd>\r\n"
        ];

        samples = [.. samples,
            "<MudGrid><MudTextField Label=\"One\" Value=\"@first\" /><MudTextField Label=\"Two\" Value=\"@second\" /></MudGrid>\r\n",
            "<MudSelect T=\"Guid?\" Value=\"@id\"><MudSelectItem T=\"Guid?\" Value=\"@(null)\">None</MudSelectItem></MudSelect>\r\n"];

        samples = [.. samples,
            "<a class=\"first button\" href=\"/one\">One <span class=\"icon\" aria-hidden=\"true\">→</span></a><a class=\"second button\" href=\"/two\">Two</a>\r\n",
            "<meta name=\"robots\" content=\"noindex\" /><meta name=\"referrer\" content=\"no-referrer\" />\r\n"];

        samples = [.. samples,
            "@{int? previousSection=null;}\r\n@foreach(var node in Nodes){<MudText>@node.Name</MudText>}\r\n",
            "@foreach(var item in Items){\r\nvar inputId = $\"setting-{item.Key.Replace('.', '-')}\";\r\n<label for=\"@inputId\">Value</label>\r\n}\r\n",
            "<p>Please use {name} here.</p>\r\n"];

        samples = [.. samples, "@if (active // preserve this comment\r\n    && ready){<MudText>Ready</MudText>}\r\n"];

        foreach (var sample in samples)
        {
            var formatted = RazorLayout.Format(sample, "Fixture.razor");

            if (RazorLayout.Format(formatted, "Fixture.razor") != formatted)
            {
                throw new InvalidOperationException("Razor formatting was not idempotent.");
            }

            if ((sample.StartsWith("<pre", StringComparison.Ordinal)
                || sample.StartsWith("<script", StringComparison.Ordinal)) && formatted != sample)
            {
                throw new InvalidOperationException("Raw element text/whitespace changed.");
            }

            if (sample.StartsWith("@foreach(", StringComparison.Ordinal)
                && !formatted.Contains("@foreach (", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Inline collection header spacing failed.");
            }

            foreach (var literal in new[] { "two words", "keep this space", "leave this alone", "dd MMM 'UTC'" })
            {
                if (sample.Contains(literal, StringComparison.Ordinal)
                    && !formatted.Contains(literal, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A literal changed.");
                }
            }
        }

        string[] protectedSpans =
        [
            "@code { private void Save() { var text = \"keep  this\"; } }",
            "@code { private RenderFragment Show() => @<div>Keep  spacing @if (ready) { <span>OK</span> }</div>; }",
            "<MudButton OnClick=\"@(() => { Save(); Close(); })\" />",
            "<script>const text = \"@(ready? 1 : 0)\";</script>"
        ];

        foreach (var span in protectedSpans)
        {
            var formatted = RazorLayout.Format("<div>Example</div>\r\n" + span, "Fixture.razor");

            if (!formatted.Contains(span, StringComparison.Ordinal)
                || RazorLayout.Format(formatted, "Fixture.razor") != formatted)
            {
                throw new InvalidOperationException("A protected Razor span changed.");
            }
        }

        var code = "class C { void M() {\r\n        var values = new[]\r\n        {\r\n            1, 2\r\n        };\r\n        foreach (var value in values)\r\n        {\r\n        }\r\n    }\r\n}\r\n";
        var updated = CSharpLayout.Format(code);

        if (updated == code || CSharpLayout.Format(updated) != updated
            || !updated.Contains("};\r\n\r\n        foreach", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Logical step spacing failed.");
        }

        var rawStringSample = "var text = \"\"\"\n    keep  \n    \"\"\";  \n";
        var expectedLiteralLayout = "var text = \"\"\"\n    keep  \n    \"\"\";\n";

        if (CSharpLayout.Format(rawStringSample) != expectedLiteralLayout)
        {
            throw new InvalidOperationException("C# trailing whitespace cleanup changed literal content.");
        }

        Console.WriteLine(
            $"Formatter fixtures passed: {samples.Length + protectedSpans.Length} Razor preservation/idempotence cases and C# logical spacing.");
    }
}
