# Source readability and formatting

StarterTemplate uses the .NET formatter, private Roslynator formatting analyzers, and a build-only source layout tool. The script applies an explicit list of layout diagnostics so a formatting pass stays separate from unrelated analyzer fixes.

## Commands

From the repository root, run the following in PowerShell 7.

```powershell
dotnet restore StarterTemplate.slnx
./scripts/Format-Code.ps1 -Check
./scripts/Format-Code.ps1 -Include src/Backend/Application/ST.Application/Features/ControlPlane/Tenants/Commands
./scripts/Format-Code.ps1 -Check -Include src/Backend/Application/ST.Application/Features/ControlPlane/Tenants/Commands
```

Omit `-Include` to cover handwritten C# and Razor under `src`, `tests`, and `tools`. Use `-Check` to report differences without editing source. It builds the formatter and runs its preservation fixtures. Use `-WrapLongLines` with a narrow scope to opt in to Roslynator width fixes. The full-source check is the formatting baseline. Use bounded scopes for routine edits in an active worktree.

## C# layout

- Use four spaces, braces on separate lines, and braces for short branches and loops. Give procedural methods block bodies. Simple properties may use expression bodies.
- Put one statement on each line. Put a blank line between methods and between distinct steps, especially after a multiline statement or before a control-flow block. Keep related simple statements together.
- Start a multiline argument list on a new line and put each argument or initializer entry on its own line. Prefer one call per line in a fluent chain of three or more calls. The three-call threshold is a review convention. The formatter checks consistency of already wrapped chains.
- Aim for 120 columns where wrapping helps. Preserve literal text, raw string contents, URLs, SQL, and serialized values.

## Razor layout

- Use four-space nesting, one substantial tag or table cell per line, and aligned multiline attributes. Keep short inline text compact.
- For complex components, put state, lifecycle methods, and event handlers in the adjacent `.razor.cs` partial class. Keep markup, directives, and injected dependencies in `.razor`.
- The formatter preserves existing inline member blocks, block-bodied callbacks, and trailing inline Razor templates. If it cannot establish a safe block boundary, it reports the component for editor formatting. The layout pass checks all selected files before it writes edits. It protects raw elements, comments, C# literals, and attribute order. Review output and build affected Razor projects after applying formatting.

The script excludes migrations, generated designers, and build output. Runtime projects carry only the private analyzer dependency. The formatter project pins its parser and RazorStyle dependencies separately and is used only for development.

## StarterTemplate adoption scope

The BT-derived tool, preservation fixtures, private analyzer dependency and editor settings are installed. The authentication/dashboard pass on 2026-11-13 applied scoped formatting to 36 affected source, test and tool files. A full historical repository formatting baseline has not been claimed; use `-Include` for routine edits in the current active worktree.
