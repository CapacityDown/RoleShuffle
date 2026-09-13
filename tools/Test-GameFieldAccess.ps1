param(
    [string]$PluginAssembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/RoleShuffle.dll'),
    [string]$GameAssembly = 'E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll'
)
$ErrorActionPreference = 'Stop'
$cecilPath = Join-Path $PSScriptRoot 'ScrollChecks/bin/Release/net9.0/Mono.Cecil.dll'
if (-not (Test-Path -LiteralPath $cecilPath)) {
    $cecilPath = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/mono.cecil/0.11.4/lib/netstandard2.0/Mono.Cecil.dll'
}
Add-Type -Path $cecilPath
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($GameAssembly)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
function Get-AllTypes($types) {
    foreach ($type in $types) { $type; Get-AllTypes $type.NestedTypes }
}
try {
    $fields = @{}
    foreach ($type in (Get-AllTypes $game.MainModule.Types)) {
        foreach ($field in $type.Fields) { $fields[$type.FullName + '::' + $field.Name] = $field }
    }
    $checked = 0
    $invalid = @(
        foreach ($type in (Get-AllTypes $plugin.MainModule.Types)) {
            foreach ($method in $type.Methods) {
                if (-not $method.HasBody) { continue }
                foreach ($instruction in $method.Body.Instructions) {
                    $reference = $instruction.Operand
                    if ($reference -isnot [Mono.Cecil.FieldReference] -or $reference.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
                    $checked++
                    $field = $fields[$reference.DeclaringType.GetElementType().FullName + '::' + $reference.Name]
                    if (-not $field) { $method.FullName + ' -> missing game field ' + $reference.FullName }
                    elseif ($field.IsPrivate -or $field.IsAssembly -or $field.IsFamilyAndAssembly) {
                        $method.FullName + ' -> inaccessible game field ' + $field.FullName
                    }
                }
            }
        }
    )
    if ($checked -eq 0) { throw 'No game field references inspected; verify assembly paths.' }
    if ($invalid.Count -gt 0) {
        $invalid | Sort-Object -Unique | Write-Output
        throw "$($invalid.Count) game field accesses would be rejected by the unmodified game."
    }
    "PASS: $checked compiled game field references checked against the installed, non-publicized assembly."
} finally { $plugin.Dispose(); $game.Dispose() }
