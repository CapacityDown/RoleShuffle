param(
    [string]$PluginAssembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/RoleShuffle.dll'),
    [string]$GameDirectory = 'E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed'
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-Repo050Compatibility.ps1') -PluginAssembly $PluginAssembly -GameDirectory $GameDirectory
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDirectory 'Assembly-CSharp.dll'))
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
$script:checks = 0
function Check($condition, $label) {
    $script:checks++
    if (-not $condition) { throw $label }
}
function Calls($method, $typeName, $methodName) {
    @($method.Body.Instructions | Where-Object {
        $_.Operand -is [Mono.Cecil.MethodReference] -and
        $_.Operand.DeclaringType.Name -eq $typeName -and $_.Operand.Name -eq $methodName
    }).Count -gt 0
}
try {
    $localization = $game.MainModule.Types | Where-Object Name -eq LocalizationManager
    foreach ($name in @('AddLocalizationChangedCallback', 'RemoveLocalizationChangedCallback')) {
        $method = $localization.Methods | Where-Object Name -eq $name
        Check ($method.IsPublic -and $method.ReturnType.FullName -eq 'System.Void' -and $method.Parameters.Count -eq 1 -and $method.Parameters[0].ParameterType.FullName -eq 'System.Action') ('Native callback signature changed: ' + $name)
    }
    foreach ($name in @('OnLocaleChanged', 'UpdateLocalizationOverrides')) {
        $method = $localization.Methods | Where-Object Name -eq $name
        Check (Calls $method 'LocalizationManager' 'LoadLocalizationOverrides') ('Overrides no longer reload: ' + $name)
        Check (Calls $method 'LocalizationManager' 'NotifyLocalizationChanged') ('Overrides no longer notify listeners: ' + $name)
        $callOrder = @($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } | ForEach-Object { $_.Operand.Name })
        Check ([Array]::IndexOf($callOrder, 'LoadLocalizationOverrides') -lt [Array]::IndexOf($callOrder, 'NotifyLocalizationChanged')) ('Listeners run before native tables update: ' + $name)
    }
    $notify = $localization.Methods | Where-Object Name -eq NotifyLocalizationChanged
    Check (Calls $notify 'Action' 'Invoke') 'Native localization callback is not invoked'
    $adapter = $plugin.MainModule.Types | Where-Object Name -eq GameLocalization
    $synchronize = $adapter.Methods | Where-Object Name -eq SynchronizeSources
    Check (Calls $synchronize 'LocalizationManager' 'AddLocalizationChangedCallback') 'Built MOD does not subscribe to translation refresh'
    Check (Calls $synchronize 'LocalizationManager' 'RemoveLocalizationChangedCallback') 'Built MOD does not detach the previous manager'
    $reset = $adapter.Methods | Where-Object Name -eq Reset
    Check (Calls $reset 'LocalizationManager' 'RemoveLocalizationChangedCallback') 'Built MOD does not clean up native callbacks'
    $lifecycle = $plugin.MainModule.Types | Where-Object Name -eq StageRolesPlugin
    Check (Calls ($lifecycle.Methods | Where-Object Name -eq OnDestroy) 'GameLocalization' 'Reset') 'Plugin shutdown leaves localization callbacks active'
    $roulette = $plugin.MainModule.Types | Where-Object Name -eq BaseUpgradeDrawRuntime
    $update = $roulette.Methods | Where-Object Name -eq Update
    Check (Calls $update 'GameLocalization' 'get_Revision') 'Visible roulette ignores same-locale translation changes'
    Check (Calls $update 'BaseUpgradeDrawRuntime' 'SetUpgradeSlot') 'Visible roulette does not refresh the shown upgrade'
    "PASS: $script:checks R.E.P.O. v0.5.1 localization and built-plugin refresh contracts. In-game rendering remains untested."
}
finally { $plugin.Dispose(); $game.Dispose() }
