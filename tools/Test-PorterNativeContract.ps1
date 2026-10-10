param(
    [string]$PluginAssembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/RoleShuffle.dll'),
    [string]$GameAssembly = 'E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed/Assembly-CSharp.dll'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/mono.cecil/0.11.4/lib/netstandard2.0/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($GameAssembly)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
$script:checks = 0
function Check($condition, $label) {
    $script:checks++
    if (-not $condition) { throw $label }
}
function AllTypes($types) { foreach ($type in $types) { $type; AllTypes $type.NestedTypes } }
try {
    $methods = @{}
    foreach ($type in (AllTypes $game.MainModule.Types)) {
        foreach ($method in $type.Methods) { $methods[$method.FullName] = $method }
    }
    $calls = 0
    foreach ($type in (AllTypes $plugin.MainModule.Types)) {
        if ($type.FullName -notlike 'REPOJP.StageRoles.PorterRuntime*') { continue }
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            foreach ($instruction in $method.Body.Instructions) {
                $ref = $instruction.Operand
                if ($ref -isnot [Mono.Cecil.MethodReference] -or $ref.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
                $calls++
                $native = $methods[$ref.FullName]
                Check ($native -and $native.IsPublic) ('Porter requires a public native method: ' + $ref.FullName)
            }
        }
    }
    Check ($calls -ge 10) 'Porter method scan did not exercise the built adapter'
    $physics = $game.MainModule.Types | Where-Object Name -eq PhysGrabObject
    $valuable = $game.MainModule.Types | Where-Object Name -eq ValuableObject
    Check (($physics.Fields | Where-Object Name -eq massOriginal).FieldType.FullName -eq 'System.Single') 'Original mass field changed'
    Check (($valuable.Fields | Where-Object Name -eq dollarValueCurrent).FieldType.FullName -eq 'System.Single') 'Current value field changed'
    $deactivate = ($physics.Methods | Where-Object Name -eq OverrideDeactivate).Body.Instructions
    Check ([bool]($deactivate | Where-Object { $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'physDisabledPosition' })) 'Native parking position contract changed'
    Check ([bool]($deactivate | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'Teleport' })) 'Native parking no longer teleports the object'
    $reset = ($physics.Methods | Where-Object Name -eq OverrideDeactivateReset).Body.Instructions
    Check ([bool]($reset | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'set_detectCollisions' })) 'Native inventory reset no longer restores collision state'
    $grabber = $game.MainModule.Types | Where-Object Name -eq PhysGrabber
    $release = ($grabber.Methods | Where-Object Name -eq OverrideGrabRelease).Body.Instructions
    Check ([bool]($release | Where-Object { $_.Operand -is [string] -and $_.Operand -eq 'ReleaseObjectRPC' })) 'Native owner grab-release RPC changed'
    $nativeRelease = $grabber.Methods | Where-Object Name -eq ReleaseObjectRPC
    Check ([bool]($nativeRelease.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'PunRPC' })) 'Native owner release receiver missing'
    $pun = $game.MainModule.Types | Where-Object Name -eq PunManager
    $speed = $pun.Methods | Where-Object Name -eq UpgradePlayerSprintSpeed
    Check ($speed.IsPublic -and $speed.Parameters.Count -eq 2 -and $speed.Parameters[1].ParameterType.FullName -eq 'System.Int32') 'Native Speed delta API changed'
    Check ([bool]($speed.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'UpdateSprintSpeedRightAway' })) 'Speed changes no longer immediately update movement'
    $command = $pun.Methods | Where-Object Name -eq TesterUpgradeCommandRPC
    Check ([bool]($command.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'PunRPC' }) -and [bool]($command.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'UpgradePlayerSprintSpeed' })) 'Vanilla peers cannot receive Speed deltas'
    $image = $plugin.MainModule.Resources | Where-Object { $_.Name.EndsWith('44-Porter.png') }
    Check ($null -ne $image) 'Porter emblem missing in the built DLL'
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $embedded = [Convert]::ToHexString($sha.ComputeHash($image.GetResourceData()))
        $local = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '../Assets/role-emblems-semibot-v1/transparent/runtime/44-Porter.png') -Algorithm SHA256).Hash
        Check ($embedded -eq $local) 'Embedded Porter artwork differs from the approved image'
    } finally { $sha.Dispose() }
    "PASS: $script:checks Porter native API, owner-release, storage and embedded-artwork checks ($calls game method calls). This is not live multiplayer validation."
} finally { $plugin.Dispose(); $game.Dispose() }
