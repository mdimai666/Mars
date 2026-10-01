param(
    [switch]$Check,
    [ValidateSet('all', 'admin', 'formeditor')]
    [string]$Entry = 'all'
)
# Compile style.less -> style.css with less@4.1.3 + BOM (repo format). Run with pwsh.
# -Check: do not write; compare repo style.css with fresh lessc output (whitespace/BOM
# insensitive) and exit 1 if stale. Recipe source: ai/CssRefactoringGuide.md.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$entries = [ordered]@{
    admin      = 'src\Mars.Admin\wwwroot\css'
    formeditor = 'src\Mars.Nodes\Mars.Nodes.FormEditor\wwwroot\css'
}
$targets = if ($Entry -eq 'all') { @($entries.Keys) } else { @($Entry) }

function Normalize([string]$s) {
    ($s -replace "^\uFEFF", '') -replace '\s+', ' '
}

# Inline sourcemap embeds the OUTPUT file name ("file":"<tmp>") — rewrite it to
# style.css so the result is deterministic regardless of the temp name.
function Fix-SourceMapFile([string]$css) {
    $m = [regex]::Match($css, 'sourceMappingURL=data:application/json;base64,([A-Za-z0-9+/=]+)')
    if (-not $m.Success) { return $css }
    $json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($m.Groups[1].Value))
    $json = $json -replace '"file":"[^"]*"', '"file":"style.css"'
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
    return $css.Substring(0, $m.Groups[1].Index) + $b64 + $css.Substring($m.Groups[1].Index + $m.Groups[1].Length)
}

$stale = @()
foreach ($name in $targets) {
    $dir = Join-Path $repo $entries[$name]
    $less = Join-Path $dir 'style.less'
    $css = Join-Path $dir 'style.css'
    if (-not (Test-Path $less)) { Write-Warning "no style.less in $dir"; continue }
    # temp MUST live in the same dir as style.css: inline sourcemap stores paths
    # relative to the output file location
    $tmp = Join-Path $dir ("style.css.tmp-" + [guid]::NewGuid().ToString('N'))
    try {
        Push-Location $dir
        try {
            npx --package less@4.1.3 lessc --source-map-map-inline style.less $tmp
            if ($LASTEXITCODE -ne 0) { throw "lessc failed for $name" }
        } finally { Pop-Location }
        $compiled = Fix-SourceMapFile ([IO.File]::ReadAllText($tmp))
        if ($Check) {
            $current = if (Test-Path $css) { [IO.File]::ReadAllText($css) } else { '' }
            if ((Normalize $current) -ne (Normalize $compiled)) {
                $stale += $name
                Write-Host "STALE  ${name}: $css does not match style.less"
            } else {
                Write-Host "OK     ${name}: style.css up to date"
            }
        } else {
            [IO.File]::WriteAllText($css, $compiled, [Text.UTF8Encoding]::new($true))
            Write-Host "BUILT  ${name}: $css"
        }
    } finally { Remove-Item $tmp -ErrorAction SilentlyContinue }
}
if ($Check -and $stale.Count -gt 0) { exit 1 }
