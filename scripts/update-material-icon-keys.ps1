param([Parameter(Mandatory)][string]$MudBlazorAssemblyPath)
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $MudBlazorAssemblyPath).Path)
$iconRoot = $assembly.GetType('MudBlazor.Icons+Material', $true)
$keys = foreach ($style in $iconRoot.GetNestedTypes()) {
    foreach ($field in $style.GetFields([Reflection.BindingFlags]'Public,Static')) {
        if ($field.FieldType -eq [string]) { $style.Name + '.' + $field.Name }
    }
}
if ($keys.Count -eq 0) { throw 'No Material icons found.' }
$target = Join-Path $PSScriptRoot '../src/Shared/ST.SharedKernel/Features/IAM/Menus/MaterialIconKeys.txt'
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($target), [string[]]($keys | Sort-Object))
Write-Output "Updated $($keys.Count) Material icon keys."
