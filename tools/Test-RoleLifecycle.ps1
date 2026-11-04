param([string]$ReportDirectory = (Join-Path $PSScriptRoot '../tmp/role-lifecycle'))
$ErrorActionPreference = 'Stop'
$null = New-Item -ItemType Directory -Path $ReportDirectory -Force
$matrix = @(
    @{ Name='LifterChecks'; Scope='All destination roles: strength reset, death/revival, stage end, reconnect' },
    @{ Name='ReviewChecks'; Scope='HP preservation, damage and shared healing budgets' },
    @{ Name='OverhaulChecks'; Scope='Upgrade targets, support expiry and role rules' },
    @{ Name='InfluenzaChecks'; Scope='Onset, infection, role replacement and stage state' },
    @{ Name='TwinsChecks'; Scope='Shared HP, paired infection, separation and cleanup' },
    @{ Name='PorterChecks'; Scope='Death, role loss, cargo recovery, failed restoration and retries' },
    @{ Name='NegotiatorChecks'; Scope='Death/revocation, shop entry/exit, reconnect and run reset' },
    @{ Name='MusicianChecks'; Scope='Budget retention across swaps/revival and stage reset' },
    @{ Name='DualWielderChecks'; Scope='Weapon release, death, role loss and failed network cleanup' },
    @{ Name='MageBeamChecks'; Scope='Beam lifetime, object cleanup and view tracking' },
    @{ Name='UtilityRuntimeChecks'; Scope='Assignment notifications, countdowns and scene cleanup' },
    @{ Name='UpgradeItemChecks'; Scope='Role reapplication, save switch, reconnect and replay' }
)
$results = @()
foreach ($item in $matrix) {
    $project = Join-Path $PSScriptRoot ($item.Name + '/' + $item.Name + '.csproj')
    $log = Join-Path $ReportDirectory ($item.Name + '.log')
    & dotnet run --project $project -c Release --no-restore *> $log
    $code = $LASTEXITCODE
    $results += [pscustomobject]@{ suite=$item.Name; scope=$item.Scope; exitCode=$code; log=$log }
    if ($code -eq 0) { Write-Output ('PASS ' + $item.Name) }
    else { Write-Output ('FAIL ' + $item.Name); Get-Content -LiteralPath $log -Tail 20 }
}
$report = [ordered]@{ generatedAtUtc=[DateTime]::UtcNow.ToString('o'); nativeGameplaySimulated=$true; results=$results }
[IO.File]::WriteAllText((Join-Path $ReportDirectory 'results.json'), ($report | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
if (@($results | Where-Object exitCode -ne 0).Count) { throw 'Role lifecycle regression failed. See logs.' }
Write-Output ('PASS: ' + $results.Count + ' lifecycle suites. This does not replace live multiplayer testing.')
