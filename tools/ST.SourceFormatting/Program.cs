using System.Text;
using ST.SourceFormatting;

if (args.Contains("--self-test", StringComparer.Ordinal))
{
    FormatterFixtures.Run();
    return 0;
}

var check = args.Contains("--check", StringComparer.Ordinal);
var includeIndex = Array.IndexOf(args, "--include");
var roots = includeIndex >= 0 ? args[(includeIndex + 1)..] : ["src", "tests", "tools"];
foreach (var root in roots)
{
    if (!File.Exists(root) && !Directory.Exists(root))
    {
        throw new DirectoryNotFoundException("A selected source path does not exist: " + root);
    }
}

var files = roots.SelectMany(path => File.Exists(path) ? [path] : Directory.Exists(path)
    ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories) : [])
    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
    .Where(path => !path.Split(
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
        StringSplitOptions.RemoveEmptyEntries)
        .Any(part => part is "bin" or "obj" or "Migrations")
        && !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
        && !path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
        && !path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
        && !path.EndsWith(
            ".Designer.cs",
            StringComparison.OrdinalIgnoreCase))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var changed = 0;
var failed = false;
var edits = new List<(string Path, string Text)>();
foreach (var file in files)
{
    var original = await File.ReadAllTextAsync(file).ConfigureAwait(false);
    string updated;

    try
    {
        updated = file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
            ? RazorLayout.Format(original, file) : CSharpLayout.Format(original);
    }
    catch (InvalidOperationException exception)
    {
        Console.Error.WriteLine($"{file}: {exception.Message}");
        failed = true;
        continue;
    }

    if (original == updated)
    {
        continue;
    }

    changed++;

    if (check)
    {
        Console.WriteLine($"{file}: source layout differs; run ./scripts/Format-Code.ps1.");
    }
    else
    {
        edits.Add((file, updated));
    }
}

if (failed)
{
    return 1;
}

foreach (var edit in edits)
{
    await File.WriteAllTextAsync(edit.Path, edit.Text, new UTF8Encoding(false)).ConfigureAwait(false);
}

Console.WriteLine(
    $"Source layout: {files.Length} files checked; {changed} {(check ? "need formatting" : "formatted")}.");
return check && changed > 0 ? 1 : 0;
