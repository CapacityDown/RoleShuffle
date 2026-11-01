param(
    [string]$GameDirectory = 'E:/SteamLibrary/steamapps/common/REPO/REPO_Data/Managed',
    [string]$GlyphOutput
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
foreach ($name in @('UnityEngine.CoreModule.dll','UnityEngine.TextCoreFontEngineModule.dll')) {
    [Reflection.Assembly]::LoadFrom((Join-Path $GameDirectory $name)) | Out-Null
}
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $GameDirectory 'Unity.TextMeshPro.dll'))
$textType = $assembly.GetType('TMPro.TMP_Text', $true)
foreach ($name in @('useTextShaper','isRightToLeftText')) {
    $property = $textType.GetProperty($name)
    if (-not $property -or -not $property.SetMethod.IsPublic -or $property.PropertyType -ne [bool]) {
        throw "Native TMP contract missing: $name"
    }
}
$shaper = $assembly.GetType('UnityTextShaper.TextShaper', $true)
$direction = $shaper.GetNestedType('TextDirection', [Reflection.BindingFlags]'Public,NonPublic')
$method = $shaper.GetMethod('ShapeText', [Reflection.BindingFlags]'Public,NonPublic,Static', $null, [type[]]@([string], $direction), $null)
$glyphs = [ordered]@{}
$count = 0
foreach ($language in @('Arabic','Thai')) {
    $codepoints = [Collections.Generic.HashSet[int]]::new()
    $mode = if ($language -eq 'Arabic') { 'RightToLeft' } else { 'LeftToRight' }
    $enumValue = [Enum]::Parse($direction, $mode)
    $catalog = Get-Content -LiteralPath (Join-Path $root "Assets/Localization/$language.json") -Raw | ConvertFrom-Json -AsHashtable
    foreach ($value in $catalog.Values) {
        $text = [regex]::Replace($value, '\{[0-9]+\}', '17')
        # The game returns a reused, null-terminated backing buffer. Its tail may
        # still contain characters from the previous call; stop at the terminator.
        $buffer = $method.Invoke($null, @($text, $enumValue))
        $shaped = [Collections.Generic.List[int]]::new()
        foreach ($codepoint in $buffer) { if ($codepoint -eq 0) { break }; $shaped.Add([int]$codepoint) }
        if ($shaped.Count -eq 0) { throw "Native shaping returned empty text: $language / $text" }
        foreach ($codepoint in $shaped) { $null = $codepoints.Add([int]$codepoint) }
        $count++
    }
    if ($language -eq 'Arabic' -and -not @($codepoints | Where-Object { $_ -ge 0xfb50 -and $_ -le 0xfeff }).Count) {
        throw 'Arabic shaping did not produce joined presentation forms.'
    }
    $glyphs[$language] = @($codepoints | Sort-Object)
}
if ($GlyphOutput) {
    [IO.File]::WriteAllText($GlyphOutput, ($glyphs | ConvertTo-Json -Depth 4), [Text.UTF8Encoding]::new($false))
}
Write-Output "PASS: $count Arabic/Thai catalog values shaped by installed game TMP; public text-shaping/direction contracts verified. Rendering still requires the game."
