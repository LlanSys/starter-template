using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ST.SourceFormatting;

internal static class CSharpLayout
{
    internal static string Format(string text)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var source = SourceText.From(text);
        var root = CSharpSyntaxTree.ParseText(source, options).GetRoot();
        var positions = new HashSet<int>();
        int Line(int offset) => source.Lines.GetLineFromPosition(offset).LineNumber;

        foreach (var block in root.DescendantNodes().OfType<BlockSyntax>())
        {
            for (var index = 1; index < block.Statements.Count; index++)
            {
                var before = block.Statements[index - 1];
                var after = block.Statements[index];
                var complexStep = before is LocalDeclarationStatementSyntax or ExpressionStatementSyntax
                    && Line(before.SpanStart) < Line(before.Span.End);

                var controlStep = after is IfStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax
                    or ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax or SwitchStatementSyntax
                    or TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax;

                if ((complexStep || controlStep) && Line(after.SpanStart) - Line(before.Span.End) == 1)
                {
                    positions.Add(source.Lines[Line(after.SpanStart)].Start);
                }
            }
        }

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var changes = positions.Select(position => new TextChange(new TextSpan(position, 0), newline))
            .Concat(root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                    && (trivia.Span.End == text.Length || text[trivia.Span.End] is '\r' or '\n'))
                .Select(trivia => new TextChange(trivia.Span, string.Empty)))
            .OrderBy(change => change.Span.Start)
            .ToArray();

        if (changes.Length == 0)
        {
            return text;
        }

        var updated = source.WithChanges(changes);

        if (!SyntaxFactory.AreEquivalent(root, CSharpSyntaxTree.ParseText(updated, options).GetRoot()))
        {
            throw new InvalidOperationException("A C# layout edit changed syntax.");
        }

        return updated.ToString();
    }
}
