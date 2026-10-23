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
function Calls($method, $name) {
    return [bool]($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $name })
}
try {
    $round = $game.MainModule.Types | Where-Object Name -eq RoundDirector
    $flag = $round.Fields | Where-Object Name -eq allExtractionPointsCompleted
    Check ($flag -and $flag.FieldType.FullName -eq 'System.Boolean' -and -not $flag.IsStatic) 'Native final completion flag changed'
    $rpc = $round.Methods | Where-Object Name -eq ExtractionCompletedAllRPC
    Check ($rpc -and [bool]($rpc.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'PunRPC' })) 'Native final extraction receiver missing'
    Check (Calls $rpc 'MasterOnlyRPC') 'Final extraction receiver must validate host authority'
    $store = $rpc.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq $flag.Name }
    Check ($store -and $store.Previous.OpCode.Name -eq 'ldc.i4.1') 'Accepted native completion must set the final flag'
    $all = $round.Methods | Where-Object Name -eq ExtractionCompletedAllCheck
    Check (Calls $all 'IsMasterClientOrSingleplayer') 'Only the host or solo game may announce final completion'
    Check (Calls $all 'ExtractionCompletedAllRPC') 'Solo final completion must invoke the receiver'
    Check ([bool]($all.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'ExtractionCompletedAllRPC' })) 'Multiplayer final completion must send the native receiver'
    foreach ($name in @('extractionPointsCompleted','extractionPoints')) {
        Check ([bool]($all.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq $name })) ('Final completion must compare ' + $name)
    }
    Check ([bool]($all.Body.Instructions | Where-Object { $_.OpCode.Name -like 'blt*' })) 'Intermediate extraction must bypass final completion'
    $point = $game.MainModule.Types | Where-Object Name -eq ExtractionPoint
    Check (Calls ($point.Methods | Where-Object Name -eq StateExtracting) 'ExtractionCompletedAllCheck') 'Final completion must occur during extraction, before payout ends'
    $patches = $plugin.MainModule.Types | Where-Object Name -eq LifecyclePatches
    $postfix = $patches.Methods | Where-Object Name -eq RoundDirectorFinalExtractionPostfix
    Check ([bool]($postfix.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'HarmonyPostfix' })) 'Final flag must be read after native sender validation'
    $patch = $postfix.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'HarmonyPatch' }
    Check ($patch.ConstructorArguments[0].Value.Name -eq 'RoundDirector' -and $patch.ConstructorArguments[1].Value -eq 'ExtractionCompletedAllRPC') 'Postfix targets the wrong completion event'
    Check ($postfix.Parameters[0].Name -eq '___allExtractionPointsCompleted' -and $postfix.Parameters[0].ParameterType.FullName -eq 'System.Boolean') 'Harmony must inject the native completion result'
    Check (Calls $postfix 'FinalExtractionCompleted') 'Native completion must reach the controller'
    $controller = $plugin.MainModule.Types | Where-Object Name -eq StageRoleController
    $callback = $controller.Methods | Where-Object Name -eq FinalExtractionCompleted
    Check (Calls $callback 'IsAuthority') 'Guests must not set the authoritative exemption'
    Check ([bool]($callback.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq '_stageReady' })) 'Inactive stages must ignore completion'
    Check (Calls $callback 'CompleteFinalExtraction') 'Final completion must disable Courier attrition'
    Check (-not (Calls ($controller.Methods | Where-Object Name -eq ExtractionCompleted) 'CompleteFinalExtraction')) 'Ordinary extraction must not disable Courier attrition'
    Check (Calls ($controller.Methods | Where-Object Name -eq BuildAbilityStatus) 'get_FinalExtractionCompleted') 'HUD must omit the finite grace countdown after final completion'
    Write-Output "Courier native contract checks passed: $script:checks"
}
finally { $plugin.Dispose(); $game.Dispose() }
