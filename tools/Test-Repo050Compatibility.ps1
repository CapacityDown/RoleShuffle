param(
    [string]$PluginAssembly = (Join-Path $PSScriptRoot '../bin/Release/netstandard2.1/RoleShuffle.dll'),
    [string]$GameDirectory = 'E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed'
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages/mono.cecil/0.11.4/lib/netstandard2.0/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDirectory 'Assembly-CSharp.dll'))
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginAssembly)
function AllTypes($types) { foreach ($type in $types) { $type; AllTypes $type.NestedTypes } }
$script:checked = 0
$script:failures = [Collections.Generic.List[string]]::new()
function Check($condition, $label) { $script:checked++; if (-not $condition) { $script:failures.Add($label) } }
function PlainType($type) { $type.FullName.TrimEnd('&') }
function PatchTarget($attrs) {
    foreach ($attr in $attrs | Where-Object { $_.AttributeType.Name -eq 'HarmonyPatch' }) {
        $type = $null; $name = $null
        foreach ($arg in $attr.ConstructorArguments) {
            if ($arg.Type.FullName -eq 'System.Type') { $type = $arg.Value }
            if ($arg.Type.FullName -eq 'System.String') { $name = $arg.Value }
        }
        if ($type -and $name) { return @($type.FullName, $name) }
    }
}
try {
    $types = @{}; $members = @{}
    foreach ($type in (AllTypes $game.MainModule.Types)) {
        $types[$type.FullName] = $type
        foreach ($method in $type.Methods) { $members[$method.FullName] = $method }
    }
    foreach ($type in (AllTypes $plugin.MainModule.Types)) {
        $classTarget = PatchTarget $type.CustomAttributes
        foreach ($method in $type.Methods) {
            if ($method.HasBody) {
                foreach ($instruction in $method.Body.Instructions) {
                    $ref = $instruction.Operand
                    if ($ref -is [Mono.Cecil.MethodReference] -and $ref.DeclaringType.Scope.Name -eq 'Assembly-CSharp' -and $ref.DeclaringType -isnot [Mono.Cecil.GenericInstanceType]) {
                        Check ($members.ContainsKey($ref.FullName)) ('Missing game call: ' + $ref.FullName)
                    }
                }
            }
            $isHook = [bool]($method.CustomAttributes | Where-Object { $_.AttributeType.Name -in @('HarmonyPrefix','HarmonyPostfix','HarmonyFinalizer','HarmonyTranspiler') })
            if (-not $isHook) { continue }
            $target = PatchTarget $method.CustomAttributes
            if (-not $target) { $target = $classTarget }
            if (-not $target -or -not $types.ContainsKey($target[0])) { continue }
            $nativeType = $types[$target[0]]
            $candidates = @($nativeType.Methods | Where-Object Name -eq $target[1])
            Check ($candidates.Count -gt 0) ('Missing patch target: ' + ($target -join '::'))
            if ($candidates.Count -ne 1) { continue }
            $native = $candidates[0]
            if ($method.CustomAttributes | Where-Object { $_.AttributeType.Name -eq "HarmonyTranspiler" }) { continue }
            foreach ($param in $method.Parameters) {
                if ($param.Name.StartsWith('___')) {
                    $fieldName = $param.Name.Substring(3)
                    $field = $nativeType.Fields | Where-Object Name -eq $fieldName
                    Check ($field -and (PlainType $field.FieldType) -eq (PlainType $param.ParameterType)) ('Missing Harmony field injection: ' + $method.FullName + '/' + $fieldName)
                }
                elseif ($param.Name -match '^__([0-9]+)$') {
                    $index = [int]$Matches[1]
                    Check ($index -lt $native.Parameters.Count -and (PlainType $native.Parameters[$index].ParameterType) -eq (PlainType $param.ParameterType)) ('Changed indexed patch argument: ' + $method.FullName + '/' + $param.Name)
                }
                elseif (-not $param.Name.StartsWith('__')) {
                    $argument = $native.Parameters | Where-Object Name -eq $param.Name
                    Check ($argument -and (PlainType $argument.ParameterType) -eq (PlainType $param.ParameterType)) ('Changed named patch argument: ' + $method.FullName + '/' + $param.Name)
                }
            }
        }
    }
    # Statically named reflection bindings, including nonpublic members that
    # compile-time reference checks cannot verify. Optional external mods are excluded.
    $pattern = 'AccessTools\.(?<kind>Field|Method)\(\s*typeof\((?<type>[\w.]+)\),\s*(?:"(?<name>[^"]+)"|nameof\((?:[\w.]+\.)?(?<named>\w+)\))'
    foreach ($file in Get-ChildItem (Join-Path $PSScriptRoot '..') -Filter '*.cs' -File) {
        foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), $pattern)) {
            $type = $types[$match.Groups['type'].Value]
            if (-not $type) { continue }
            $name = $match.Groups['name'].Value
            if (-not $name) { $name = $match.Groups['named'].Value }
            $found = if ($match.Groups['kind'].Value -eq 'Field') { @($type.Fields | Where-Object Name -eq $name).Count } else { @($type.Methods | Where-Object Name -eq $name).Count }
            Check ($found -gt 0) ('Missing reflection binding: ' + $file.Name + ': ' + $type.Name + '::' + $name)
        }
    }
    $chat = $types['PlayerAvatar'].Methods | Where-Object Name -eq ChatMessageSendRPC
    Check ($chat.Parameters.Count -eq 4 -and $chat.Parameters[2].ParameterType.FullName -eq 'System.String') 'Private TTS requires the new locale payload'
    $drain = $types['ItemBattery'].Methods | Where-Object Name -eq DrainContinuous
    Check ($drain.Parameters.Count -eq 2) 'Continuous battery drain signature changed'
    $leaf = $types['ItemLeafBlower'].Methods | Where-Object Name -eq BatteryDrain
    Check ([bool]($leaf.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'DrainContinuous' })) 'Leaf blower must use central drain without a duplicate discount'
    if ($failures.Count -gt 0) { $failures | Sort-Object -Unique | Write-Output; throw "$($failures.Count) failed compatibility checks of $checked." }
    Write-Output "PASS: $checked installed v0.5.0 calls, Harmony bindings and reflection contracts."
}
finally { $plugin.Dispose(); $game.Dispose() }
