$root = "C:\Users\D\Documents\VisualStudio\2025\Mars\tests"
$enc = [System.Text.Encoding]::GetEncoding(28591)

$exact = @{
    'ShouldSuccess' = 'Succeeds'
    'ShouldSuccesss' = 'Succeeds'
    'ShouldSuccessAsync' = 'SucceedsAsync'
    'ShouldOK' = 'Succeeds'
    'ShouldNotOK' = 'Fails'
    'ShouldFail' = 'Fails'
    'ShouldFail400' = 'Fails400'
    'Should400' = 'Fails400'
    'Should401' = 'Fails401'
    'ShouldException' = 'Throws'
    'ShouldThrow' = 'Throws'
    'ShouldThrowException' = 'ThrowsException'
    'ShouldNotThrowError' = 'DoesNotThrowError'
    'ShouldTrue' = 'ReturnsTrue'
    'ShouldEmpty' = 'IsEmpty'
    'ShouldOptionNotRegisteredException' = 'ThrowsOptionNotRegisteredException'
    'ShouldResponseOK' = 'RespondsOk'
    'ShouldResponseDataContext' = 'RespondsWithDataContext'
    'ShouldAuthUserName' = 'RendersAuthenticatedUserName'
    'ShouldGreaterThanZero' = 'IsGreaterThanZero'
    'ShouldUnauthorized' = 'ReturnsUnauthorized'
}

$manual = @(
    'ShouldWorkExpect', 'ShouldExpectResult', 'ShouldValidateError', 'ShouldValidationError',
    'ShouldUnsupportedStatusError', 'ShouldOnePack', 'ShouldToDateTime', 'ShouldValidTotalCount',
    'ShouldValidPath', 'ShouldChildElementRecalcPath', 'ShouldDidntChangePayload',
    'ShouldNotNextReturn', 'ShouldTimeoutAndReturnAggregated', 'ShouldSuccessReturnCorrentDictionaryType',
    'ShouldError466', 'Should466Denied', 'ShouldExpectExt'
)

$prefixKeys = @('ShouldStatusCode', 'ShouldNotBe', 'ShouldBe', 'ShouldStatus', 'ShouldSuccess')
$prefixMap = @{
    'ShouldStatusCode' = 'ReturnsStatusCode'
    'ShouldNotBe' = 'IsNot'
    'ShouldBe' = 'Is'
    'ShouldStatus' = 'ReturnsStatus'
    'ShouldSuccess' = 'Succeeds'
}

function Conjugate([string]$w) {
    if ($w -ceq 'Have') { return 'Has' }
    if ($w -cmatch '[^aeiou]y$') { return $w.Substring(0, $w.Length - 1) + 'ies' }
    if ($w -cmatch '(s|sh|ch|x|z)$') { return $w + 'es' }
    return $w + 's'
}

function Transform([string]$name) {
    $segs = $name -split '_'
    $last = $segs[-1]
    if ($last -cnotmatch 'Should') { return 'MANUAL:middlename' }
    if (-not $last.StartsWith('Should')) { return 'MANUAL:midsegment' }
    if ($manual -contains $last) { return 'MANUAL:' + $last }
    $newLast = $null
    if ($exact.ContainsKey($last)) { $newLast = $exact[$last] }
    else {
        foreach ($p in $prefixKeys) {
            if ($last.StartsWith($p)) { $newLast = $prefixMap[$p] + $last.Substring($p.Length); break }
        }
        if ($null -eq $newLast) {
            $rest = $last.Substring(6)
            if ($rest -cmatch '^([A-Z][a-z]+)(.*)$') {
                $newLast = (Conjugate $matches[1]) + $matches[2]
            } else {
                return 'MANUAL:generic-fail:' + $last
            }
        }
    }
    if ($segs.Count -eq 1) { return $newLast }
    return (($segs[0..($segs.Count - 2)] + $newLast) -join '_')
}

$attrRe = '\[(Fact|Theory|SkippableFact|SkippableTheory|IntegrationFact|ExternalIntegrationFact|DockerContainerFact|SqlFact)'
$methodRe = '^\s*(public\s+)?(async\s+)?(Task|ValueTask|void)\s+(\w+)\s*\('

$renames = New-Object System.Collections.ArrayList
$manuals = New-Object System.Collections.ArrayList

$files = Get-ChildItem $root -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch 'obj|bin' }
foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $text = $enc.GetString($bytes)
    $lines = $text -split '(?<=\n)'
    $changed = $false
    $seen = @{}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $attrRe) {
            for ($j = $i + 1; $j -lt [Math]::Min($i + 10, $lines.Count); $j++) {
                if ($lines[$j] -match $methodRe) {
                    $name = $matches[4]
                    if ($name -cmatch 'Should' -and -not $seen.ContainsKey($j)) {
                        $seen[$j] = $true
                        $new = Transform $name
                        if ($new -like 'MANUAL*') {
                            [void]$manuals.Add(($f.FullName.Substring($root.Length + 1) + ' | ' + $name + ' | ' + $new))
                        } elseif ($new -ne $name) {
                            $lines[$j] = $lines[$j].Replace($name, $new)
                            [void]$renames.Add(($name + ' -> ' + $new))
                            $changed = $true
                        }
                    }
                    break
                } elseif ($lines[$j] -match '^\s*\[') { continue } else { break }
            }
        }
    }
    if ($changed) {
        [System.IO.File]::WriteAllBytes($f.FullName, $enc.GetBytes(($lines -join '')))
    }
}

Write-Output ('RENAMED: ' + $renames.Count)
$renames | Sort-Object | Group-Object | ForEach-Object { Write-Output ('  ' + $_.Count + 'x ' + $_.Name) }
Write-Output ''
Write-Output ('MANUAL: ' + $manuals.Count)
$manuals | Sort-Object -Unique | ForEach-Object { Write-Output ('  ' + $_) }
