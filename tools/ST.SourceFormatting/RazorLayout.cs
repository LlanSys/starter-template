using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PinguApps.RazorStyle.Core.Documents;
using PinguApps.RazorStyle.Core.Running;

namespace ST.SourceFormatting;

internal static class RazorLayout
{
    private const string MarkerPrefix = "__ST_FORMAT_PROTECTED_";

    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Latest);

    private static readonly RazorStyleOptions AttributeOptions = new(["PARS0002", "PARS0003"]);

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };

    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "div",
        "section",
        "table",
        "thead",
        "tbody",
        "tr",
        "td",
        "dl",
        "dt",
        "dd",
        "ul",
        "li",
        "ol",
        "form",
        "fieldset",
        "article",
        "nav",
        "header",
        "footer",
        "main" };

    internal static string Format(string original, string path)
    {
        if (original.Contains(MarkerPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Source contains a reserved formatting marker.");
        }

        var payloads = new List<string>();
        var controlMarkers = new HashSet<string>(StringComparer.Ordinal);
        string Save(string value, bool control = false)
        {
            var marker = MarkerPrefix + payloads.Count.ToString(CultureInfo.InvariantCulture)
                + "__";

            payloads.Add(value);

            if (control)
            {
                controlMarkers.Add(marker);
            }

            return marker;
        }

        var text = Replace(original, @"@\*[\s\S]*?\*@|<!--[\s\S]*?-->", match => Save(match.Value));
        text = Replace(text, @"<(pre|textarea|script|style)\b[^>]*>[\s\S]*?</\1\s*>", match => Save(match.Value));
        text = ProtectCodeBlocks(text, value => Save(value));
        text = Replace(
            text,
            @"(?m)^[ \t]*@(?:page|attribute|layout|rendermode|inject|using|implements|typeparam|namespace)\b[^\r\n]*",
            match => Save(FormatDirective(match.Value.TrimStart())));
        // Protect complete C# spans before the markup scanner sees quotes, interpolation braces or > operators.
        for (var index = text.Length - 1; index >= 0; index--)
        {
            if (text[index] != '@' || index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '@'))
            {
                continue;
            }

            var start = index + 1;

            if (start >= text.Length)
            {
                continue;
            }

            if (text[start] == '{')
            {
                var blockEnd = BalancedEnd(text, start, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken);

                if (blockEnd > start)
                {
                    var block = SyntaxFactory.ParseStatement(text[start..blockEnd], options: ParseOptions);

                    if (block is BlockSyntax && !block.ContainsDiagnostics)
                    {
                        text = text[..index] + Save("@" + block.NormalizeWhitespace().ToFullString())
                            + text[blockEnd..];

                        continue;
                    }
                }
            }

            var header = Match(text[index..], @"^@(?:if|foreach|for|while|switch|lock)\s*\(");

            if (header.Success)
            {
                var open = text.IndexOf('(', index);
                var end = BalancedEnd(text, open, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken);

                if (end <= open)
                {
                    continue;
                }

                var statement = SyntaxFactory.ParseStatement(text[start..end] + " { }", options: ParseOptions);
                var normalized = statement.NormalizeWhitespace(indentation: " ", eol: " ").ToFullString();
                var brace = normalized.LastIndexOf(" {", StringComparison.Ordinal);
                var value = !statement.ContainsDiagnostics && brace >= 0 && !normalized[..brace].Contains('\n')
                    ? "@" + normalized[..brace].TrimEnd() : text[index..end];

                if (!SyntaxFactory.AreEquivalent(
                    statement,
                    SyntaxFactory.ParseStatement(value[1..] + " { }", options: ParseOptions)))
                {
                    value = text[index..end];
                }

                text = text[..index] + Save(value, true) + text[end..];
                continue;
            }

            var expressionEnd = text[start] == '('
                ? BalancedEnd(text, start, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken)
                : ImplicitEnd(text, start);

            if (expressionEnd <= start)
            {
                continue;
            }

            var expressionText = text[start..expressionEnd];
            var expression = SyntaxFactory.ParseExpression(expressionText, options: ParseOptions);

            if (expression.ContainsDiagnostics)
            {
                continue;
            }

            if (expression.DescendantNodesAndSelf().OfType<LambdaExpressionSyntax>()
                .Any(lambda => lambda.Body is BlockSyntax))
            {
                text = text[..index] + Save(text[index..expressionEnd]) + text[expressionEnd..];
                continue;
            }

            var formatted = expression.NormalizeWhitespace(indentation: " ", eol: " ").ToFullString();

            if (formatted.Contains('\n') || !SyntaxFactory.AreEquivalent(
                expression,
                SyntaxFactory.ParseExpression(formatted, options: ParseOptions)))
            {
                formatted = expressionText;
            }

            text = text[..index] + Save("@" + formatted, expressionText == "code") + text[expressionEnd..];
        }

        text = Replace(
            text,
            @"\belse\s+if\s*\([^\r\n]*?\)(?=\s*\{)",
            match =>
            {
                var condition = SyntaxFactory.ParseExpression(
                    match.Value[match.Value.IndexOf('(')..],
                    options: ParseOptions);

                return Save(
                    condition.ContainsDiagnostics ? match.Value : "else if " + condition.NormalizeWhitespace(),
                    true);
            });

        text = ProtectLiterals(text, value => Save(value));
        text = IndentAndSeparate(text, controlMarkers);
        // Keep mixed inline HTML on its existing line: adding nodes of whitespace can change text flow.
        foreach (var tag in new RazorStyleDocument(text, path).Tags.OrderByDescending(tag => tag.StartIndex))
        {
            var lineStart = text.LastIndexOf('\n', Math.Max(0, tag.StartIndex - 1)) + 1;

            if (char.IsUpper(tag.Name[0]) || VoidTags.Contains(tag.Name) || BlockTags.Contains(tag.Name)
                || tag.Attributes.Count < 2 || text[lineStart..tag.StartIndex].Trim().Length == 0)
            {
                continue;
            }

            var inlineTag = "<" + tag.Name + " "
                + string.Join(" ", tag.Attributes.Select(attribute => attribute.RawText.Trim()))
                + (tag.IsSelfClosing ? " />" : ">");

            text = text[..tag.StartIndex] + Save(inlineTag) + text[(tag.EndIndex + 1)..];
        }

        text = new RazorStyleRunner().FixText(text, path, AttributeOptions).RewrittenText ?? text;

        text = Replace(text, @"(?m)[ \t]+(?=\r?$)", _ => "");

        for (var index = payloads.Count - 1; index >= 0; index--)
        {
            text = text.Replace(
                MarkerPrefix + index.ToString(CultureInfo.InvariantCulture) + "__",
                payloads[index],
                StringComparison.Ordinal);
        }

        if (Replace(original, @"\s", _ => "") != Replace(text, @"\s", _ => ""))
        {
            throw new InvalidOperationException("A Razor layout edit changed non-whitespace source.");
        }

        return text;
    }

    private static string ProtectCodeBlocks(string text, Func<string, string> save)
    {
        var blocks = Regex.Matches(
            text,
            @"(?m)^[ \t]*@(?:code|functions)\s*\{",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(2));

        foreach (var block in blocks.Reverse())
        {
            var start = block.Index + block.Length - 1;
            var end = BalancedEnd(text, start, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken);

            if (end <= start)
            {
                throw new InvalidOperationException("Could not locate the end of an inline component block.");
            }

            var members = text[start..end];
            var isTrailingTemplate = string.IsNullOrWhiteSpace(text[end..])
                && members.Contains("@<", StringComparison.Ordinal);

            if (!isTrailingTemplate && SyntaxFactory.ParseCompilationUnit("class __Formatting " + members,
                options: ParseOptions).ContainsDiagnostics)
            {
                throw new InvalidOperationException(
                    "Inline component members contain mixed Razor syntax. Format this file in the Razor editor.");
            }

            text = text[..block.Index] + save(text[block.Index..end]) + text[end..];
        }

        return text;
    }

    private static string FormatDirective(string text)
    {
        if (!text.StartsWith("@attribute ", StringComparison.Ordinal))
        {
            return text;
        }

        var declaration = SyntaxFactory.ParseCompilationUnit(text[11..] + " class __FormatAttribute { }")
            .Members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        return declaration is null || declaration.ContainsDiagnostics ? text
            : "@attribute " + string.Join(
                " ",
                declaration.AttributeLists.Select(list => list.NormalizeWhitespace().ToFullString()));
    }

    private static string ProtectLiterals(string text, Func<string, string> save)
    {
        var tags = new RazorStyleDocument(text, "component.razor").Tags.OrderBy(tag => tag.StartIndex).ToArray();
        var tagIndex = 0;
        var edits = new List<(int Start, int End, string Replacement)>();

        for (var index = 0; index < text.Length; index++)
        {
            while (tagIndex < tags.Length && tags[tagIndex].EndIndex < index)
            {
                tagIndex++;
            }

            if (tagIndex < tags.Length && index >= tags[tagIndex].StartIndex && index <= tags[tagIndex].EndIndex)
            {
                index = tags[tagIndex].EndIndex;
                continue;
            }

            if (text[index] is not ('"' or '\'' or '$' or '@'))
            {
                continue;
            }

            var root = SyntaxFactory.ParseExpression(text[index..], options: ParseOptions, consumeFullText: false);
            var literal = root.DescendantNodesAndSelf().OfType<ExpressionSyntax>().FirstOrDefault(node =>
                node.SpanStart == 0 && !node.ContainsDiagnostics && (node is InterpolatedStringExpressionSyntax
                    || node is LiteralExpressionSyntax value && (value.IsKind(SyntaxKind.StringLiteralExpression)
                        || value.IsKind(SyntaxKind.CharacterLiteralExpression))));

            if (literal is null)
            {
                continue;
            }

            var end = index + literal.Span.End;
            edits.Add((index, end, save(text[index..end])));
            index = end - 1;
        }

        foreach (var edit in edits.OrderByDescending(edit => edit.Start))
        {
            text = text[..edit.Start] + edit.Replacement + text[edit.End..];
        }

        return text;
    }

    private static int ImplicitEnd(string text, int start)
    {
        if (!char.IsLetter(text[start]) && text[start] != '_')
        {
            return start;
        }

        var index = start;

        while (index < text.Length)
        {
            while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
            {
                index++;
            }

            if (index < text.Length && text[index] is '(' or '[')
            {
                var end = BalancedEnd(
                    text,
                    index,
                    text[index] == '(' ? SyntaxKind.OpenParenToken : SyntaxKind.OpenBracketToken,
                    text[index] == '(' ? SyntaxKind.CloseParenToken : SyntaxKind.CloseBracketToken);

                if (end <= index)
                {
                    return index;
                }

                index = end;
            }

            if (index + 1 < text.Length && text[index] == '?' && text[index + 1] == '.')
            {
                index++;
            }

            if (index < text.Length && text[index] == '.' && index + 1 < text.Length
                && (char.IsLetter(text[index + 1]) || text[index + 1] == '_'))
            {
                index++;
                continue;
            }

            break;
        }

        return index;
    }

    private static int BalancedEnd(string text, int start, SyntaxKind open, SyntaxKind close)
    {
        var depth = 0;

        foreach (var token in SyntaxFactory.ParseTokens(text[start..], options: ParseOptions))
        {
            if (token.IsKind(open))
            {
                depth++;
            }

            if (token.IsKind(close) && --depth == 0)
            {
                return start + token.Span.End;
            }
        }

        return start;
    }

    private static string IndentAndSeparate(string text, IReadOnlySet<string> controlMarkers)
    {
        var events = Events(text, controlMarkers);
        var inserts = new HashSet<int>();

        foreach (var item in events.Where(item => item.Kind is "braceOpen" or "braceClose"))
        {
            var start = text.LastIndexOf('\n', Math.Max(0, item.Start - 1)) + 1;

            if (text[start..item.Start].Trim().Length > 0)
            {
                inserts.Add(item.Start);
            }

            var end = text.IndexOf('\n', item.End);

            if (end < 0)
            {
                end = text.Length;
            }

            if (text[item.End..end].Trim().Length > 0)
            {
                inserts.Add(item.End);
            }
        }

        for (var index = 1; index < events.Count; index++)
        {
            var before = events[index - 1];
            var after = events[index];

            if (before.End > after.Start || before.Kind.StartsWith("brace", StringComparison.Ordinal)
                || after.Kind.StartsWith("brace", StringComparison.Ordinal))
            {
                continue;
            }

            var gap = text[before.End..after.Start];

            if (!gap.Contains('\n') && string.IsNullOrWhiteSpace(gap) && (char.IsUpper(before.Name[0])
                || char.IsUpper(after.Name[0])
                || BlockTags.Contains(before.Name)
                || BlockTags.Contains(after.Name)
                || VoidTags.Contains(before.Name)
                || VoidTags.Contains(after.Name)))
            {
                inserts.Add(after.Start);
            }
        }

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        foreach (var offset in inserts.OrderDescending())
        {
            text = text.Insert(offset, newline);
        }

        events = Events(text, controlMarkers);
        var lines = SourceText.From(text).Lines;
        var depth = 0;
        var changes = new Dictionary<int, int>();
        var protectedLines = new HashSet<int>();
        var cursor = 0;
        void Mark(int offset)
        {
            var line = lines.GetLineFromPosition(offset);

            if (!protectedLines.Contains(line.LineNumber) && !changes.ContainsKey(line.Start))
            {
                changes[line.Start] = Math.Max(0, depth) * 4;
            }
        }

        foreach (var item in events)
        {
            while (cursor < item.Start)
            {
                if (!char.IsWhiteSpace(text[cursor]))
                {
                    Mark(cursor);
                }

                cursor++;
            }

            if (item.Kind is "close" or "braceClose")
            {
                depth = Math.Max(0, depth - 1);
            }

            Mark(item.Start);
            var startLine = lines.GetLineFromPosition(item.Start).LineNumber;
            var endLine = lines.GetLineFromPosition(item.End - 1).LineNumber;

            for (var index = startLine + 1; index <= endLine; index++)
            {
                protectedLines.Add(index);
            }

            if (item.Kind is "open" or "braceOpen")
            {
                depth++;
            }

            cursor = item.End;
        }

        while (cursor < text.Length)
        {
            if (!char.IsWhiteSpace(text[cursor]))
            {
                Mark(cursor);
            }

            cursor++;
        }

        foreach (var change in changes.OrderByDescending(change => change.Key))
        {
            var count = 0;

            while (change.Key + count < text.Length && text[change.Key + count] is ' ' or '\t')
            {
                count++;
            }

            text = text[..change.Key] + new string(' ', change.Value) + text[(change.Key + count)..];
        }

        return text;
    }

    private static List<LayoutEvent> Events(string text, IReadOnlySet<string> controlMarkers)
    {
        var tags = new RazorStyleDocument(text, "component.razor").Tags.ToDictionary(tag => tag.StartIndex);
        var result = new List<LayoutEvent>();
        var htmlDepth = 0;
        var controlDepths = new Stack<int>();

        for (var index = 0; index < text.Length;)
        {
            if (tags.TryGetValue(index, out var tag))
            {
                result.Add(new(
                    index,
                    tag.EndIndex + 1,
                    tag.IsSelfClosing || VoidTags.Contains(tag.Name) ? "void" : "open",
                    tag.Name));
                // The scanner exposes an inclusive end index.
                if (!tag.IsSelfClosing && !VoidTags.Contains(tag.Name))
                {
                    htmlDepth++;
                }

                index = tag.EndIndex + 1;
                continue;
            }

            if (text.AsSpan(index).StartsWith("</", StringComparison.Ordinal))
            {
                var match = Match(text[index..], @"^</(?<name>[\w.:-]+)\s*>");

                if (match.Success)
                {
                    result.Add(new(index, index + match.Length, "close", match.Groups["name"].Value));
                    htmlDepth = Math.Max(0, htmlDepth - 1);
                    index += match.Length;
                    continue;
                }
            }

            if (text[index] == '{')
            {
                var prefix = text[..index].TrimEnd();
                var marker = prefix.LastIndexOf(MarkerPrefix, StringComparison.Ordinal);
                var header = marker >= 0 && controlMarkers.Contains(prefix[marker..]);
                var afterClose = result.Count > 0 && result[^1].Kind == "braceClose"
                    && text[result[^1].End..index].Trim() == "else";

                if (header || afterClose)
                {
                    result.Add(new(index, index + 1, "braceOpen", "{}"));
                    controlDepths.Push(htmlDepth);
                }
            }
            else if (text[index] == '}' && controlDepths.TryPeek(out var bodyDepth) && htmlDepth == bodyDepth)
            {
                result.Add(new(index, index + 1, "braceClose", "{}"));
                controlDepths.Pop();
            }

            index++;
        }

        return result;
    }

    private static Match Match(string text, string pattern)
    {
        return Regex.Match(text, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
    }

    private static string Replace(string text, string pattern, MatchEvaluator evaluator)
    {
        return Regex.Replace(text, pattern, evaluator, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
    }

    private sealed record LayoutEvent(int Start, int End, string Kind, string Name);
}
