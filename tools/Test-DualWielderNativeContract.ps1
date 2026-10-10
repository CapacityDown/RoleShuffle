param(
    [string]$PluginAssembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/RoleShuffle.dll'),
    [string]$GameAssembly = 'E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed/Assembly-CSharp.dll'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/mono.cecil/0.11.4/lib/netstandard2.0/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($GameAssembly)
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
$script:checks = 0
function Check($condition, $label) { $script:checks++; if (-not $condition) { throw $label } }
function AllTypes($types) { foreach ($type in $types) { $type; AllTypes $type.NestedTypes } }
function Calls($method, $name) { return [bool]($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $name }) }
try {
    $methods = @{}
    foreach ($type in (AllTypes $game.MainModule.Types)) { foreach ($method in $type.Methods) { $methods[$method.FullName] = $method } }
    $calls = 0
    foreach ($type in (AllTypes $plugin.MainModule.Types)) {
        if ($type.FullName -notlike 'REPOJP.StageRoles.DualWielder*') { continue }
        foreach ($method in $type.Methods) {
            if (-not $method.HasBody) { continue }
            foreach ($instruction in $method.Body.Instructions) {
                $ref = $instruction.Operand
                if ($ref -isnot [Mono.Cecil.MethodReference] -or $ref.DeclaringType.Scope.Name -ne 'Assembly-CSharp') { continue }
                $calls++; $native = $methods[$ref.FullName]
                if (-not $native -and $ref.DeclaringType -is [Mono.Cecil.GenericInstanceType]) {
                    $definition = $game.MainModule.Types | Where-Object FullName -eq $ref.DeclaringType.ElementType.FullName
                    $candidates = @($definition.Methods | Where-Object { $_.Name -eq $ref.Name -and $_.Parameters.Count -eq $ref.Parameters.Count })
                    if ($candidates.Count -eq 1) { $native = $candidates[0] }
                }
                Check ($native -and $native.IsPublic) ('Public native method required: ' + $ref.FullName)
            }
        }
    }
    Check ($calls -ge 7) 'No usable DualWielder adapter scan'
    foreach ($expected in @(
        @('ItemMelee','hurtCollider','HurtCollider'), @('ItemMelee','itemBattery','ItemBattery'),
        @('ItemMelee','hitTimer','System.Single'), @('HurtCollider','playerCausingHurtOverride','PlayerAvatar'),
        @('ItemAttributes','instanceName','System.String')
    )) {
        $type = $game.MainModule.Types | Where-Object Name -eq $expected[0]
        Check (($type.Fields | Where-Object Name -eq $expected[1]).FieldType.FullName -eq $expected[2]) ('Reflection contract: ' + $expected[0] + '.' + $expected[1])
    }
    $melee = $game.MainModule.Types | Where-Object Name -eq ItemMelee
    $timers = $melee.Methods | Where-Object Name -eq TimersTick
    Check ($timers -and $timers.Parameters.Count -eq 0) 'Native melee cooldown timer signature'
    foreach ($field in @('hitTimer','enemyOrPVPDurabilityLossCooldown','durabilityLossCooldown')) {
        Check ([bool]($timers.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq $field })) ('Cooldown maintained: ' + $field)
    }
    foreach ($methodName in @('EnemyOrPVPSwingHitRPC','SwingHitRPC')) {
        $method = $melee.Methods | Where-Object Name -eq $methodName
        Check ([bool]($method.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'PunRPC' })) ('Native hit RPC: ' + $methodName)
        Check ([bool]($method.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'itemBattery' })) ('Hit uses redirected battery: ' + $methodName)
        Check ([bool]($method.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'batteryLife' })) ('Native hit consumes battery: ' + $methodName)
    }
    $hurtType = $game.MainModule.Types | Where-Object Name -eq HurtCollider
    $detector = (AllTypes $hurtType.NestedTypes).Methods | Where-Object { $_.HasBody -and (Calls $_ 'EnemyHurt') }
    Check ([bool]($detector.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.FieldReference] -and $_.Operand.Name -eq 'playerCausingHurtOverride' })) 'Native collision detector uses the explicit attack owner'
    Check ([bool]($detector.Body.Instructions | Where-Object { $_.OpCode.Name -eq 'stfld' -and $_.Operand.Name -eq 'playerCausingHurt' })) 'Native detector publishes attacker for role hit accounting'
    $patches = $plugin.MainModule.Types | Where-Object Name -eq DualWielderPatches
    $targets = [Collections.Generic.HashSet[string]]::new()
    foreach ($patch in $patches.Methods) {
        $attribute = $patch.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'HarmonyPatch' }
        if (-not $attribute) { continue }
        Check ($attribute.ConstructorArguments.Count -eq 2) ('Explicit patch target: ' + $patch.Name)
        $nativeType = $game.MainModule.Types | Where-Object FullName -eq $attribute.ConstructorArguments[0].Value.FullName
        $target = @($nativeType.Methods | Where-Object Name -eq $attribute.ConstructorArguments[1].Value)
        Check ($target.Count -eq 1) ('Unambiguous patch: ' + $patch.Name)
        $null = $targets.Add($nativeType.Name + '.' + $target[0].Name)
        foreach ($parameter in $patch.Parameters) {
            if ($parameter.Name -match '^__(\d+)$') {
                $position = [int]$Matches[1]
                Check ($target[0].Parameters[$position].ParameterType.FullName -eq $parameter.ParameterType.FullName) ('Patch argument type: ' + $patch.Name + '.' + $parameter.Name)
            }
        }
    }
    foreach ($target in @('ItemMelee.Update','ItemMelee.FixedUpdate','ItemManager.AddSpawnedItem','StatsManager.ItemFetchName','ItemEquippable.RPC_RequestEquip','PhysGrabObject.GrabLinkRPC','PhysGrabObject.GrabStartedRPC','PhysGrabObject.GrabPlayerAddRPC')) {
        Check ($targets.Contains($target)) ('Required hook missing: ' + $target)
    }
    $pluginType = $plugin.MainModule.Types | Where-Object Name -eq StageRolesPlugin
    Check ([bool]($pluginType.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.OpCode.Name -eq 'ldtoken' -and $_.Operand.Name -eq 'DualWielderPatches' })) 'Harmony hooks are registered'
    $echo = $plugin.MainModule.Types | Where-Object Name -eq DualWielderEcho
    Check (-not [bool]($echo.Methods | Where-Object HasBody | ForEach-Object { $_.Body.Instructions } | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -in @('ItemAdd','ItemRemove') })) 'Copies must not mutate purchased inventory identities'
    $pun = $game.MainModule.Types | Where-Object Name -eq PunManager
    Check (Calls ($pun.Methods | Where-Object Name -eq SetItemName) 'RPC') 'Shared native item name is sent to vanilla peers'
    $grabber = $game.MainModule.Types | Where-Object Name -eq PhysGrabber
    Check ([bool](($grabber.Methods | Where-Object Name -eq OverrideGrabRelease).Body.Instructions | Where-Object { $_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'ReleaseObjectRPC' })) 'Owner grab rejection reaches vanilla peers'
    $image = $plugin.MainModule.Resources | Where-Object { $_.Name.EndsWith('45-DualWielder.png') }
    Check ($null -ne $image) 'DualWielder emblem embedded'
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $embedded = [Convert]::ToHexString($sha.ComputeHash($image.GetResourceData()))
        Check ($embedded -eq (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot '../Assets/role-emblems-semibot-v1/transparent/runtime/45-DualWielder.png') -Algorithm SHA256).Hash) 'Embedded emblem matches approved PNG'
    } finally { $sha.Dispose() }
    "PASS: $script:checks DualWielder native API, hit consumption, Harmony hook, inventory and emblem checks ($calls game method calls). Live physics/multiplayer remain untested."
} finally { $plugin.Dispose(); $game.Dispose() }
