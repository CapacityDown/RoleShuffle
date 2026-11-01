$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$controller = Get-Content -LiteralPath (Join-Path $root 'StageRoleController.cs') -Raw
$models = Get-Content -LiteralPath (Join-Path $root 'RoleModels.cs') -Raw
$catalog = [regex]::Match($models, '(?s)    internal static bool IsSecretRole.*?(?=    internal static IReadOnlyList<UpgradeGrant> BaseUpgrades)').Value
$solo = [regex]::Match($controller, '(?s)    private static readonly StageRole\[\] SoloExcludedRoles.*?    };').Value
if (!$catalog -or !$solo) { throw 'Production catalog extraction failed' }
$helpers = ''
foreach ($method in @('RemoveSoloIncompatibleRoles','RemoveOrphanedSuperbot','EnabledRoles','BuildRerollHistory','CopyRecentRoleHistory','RecordRecentRole')) {
    $match = [regex]::Match($controller, '(?ms)^    private [^
{]+\b' + $method + '\(.*?(?=^    (?:private|internal|public) |\z)')
    if (!$match.Success) { throw "Production helper extraction failed: $method" }
    $helpers += $match.Value
}
$source = "#nullable enable annotations`n#nullable disable warnings`nusing System; using System.Collections.Generic; namespace REPOJP.StageRoles {`ninternal static partial class RoleCatalog {`n$catalog`n}`ninternal sealed partial class StageRoleController {`n$solo`n$helpers`n}}"
$folder = Join-Path $PSScriptRoot 'DrawEligibilityChecks/obj'
$null = New-Item -ItemType Directory -Force -Path $folder
[IO.File]::WriteAllText((Join-Path $folder 'ProductionDependencies.cs'), $source, [Text.UTF8Encoding]::new($false))
dotnet run --project (Join-Path $PSScriptRoot 'DrawEligibilityChecks/DrawEligibilityChecks.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Draw eligibility checks failed' }
