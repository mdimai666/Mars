<#
.SYNOPSIS
    Fast full test run: builds Mars.slnx, then runs each xUnit v3 / MTP test exe in parallel.

.DESCRIPTION
    xUnit v3 test projects build as MTP executables (OutputType=Exe). This script runs them
    directly (the "dotnet test" MTP driver path is blocked on SDK 10.0.400, exit 5).
    Default: unit + Docker-integration projects. Mars.E2E.Tests and Mars.DockerImage.Tests
    are excluded unless -IncludeE2E.

.PARAMETER Configuration
    Build/runtime configuration (Debug/Release). Default: Debug.

.PARAMETER Framework
    Target framework of the built exes. Default: net10.0.

.PARAMETER MaxParallel
    How many test exes run concurrently. Default: 4.

.PARAMETER TimeoutSec
    Per-project timeout. Default: 1800.

.PARAMETER SkipBuild
    Do not run "dotnet build Mars.slnx" first.

.PARAMETER IncludeE2E
    Also run Mars.E2E.Tests and Mars.DockerImage.Tests.

.PARAMETER List
    Print discovered projects and exit without building/running.

.EXAMPLE
    pwsh -File test-all.ps1
    pwsh -File test-all.ps1 -IncludeE2E -MaxParallel 2
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$Framework = 'net10.0',
    [int]$MaxParallel = 4,
    [int]$TimeoutSec = 1800,
    [switch]$SkipBuild,
    [switch]$IncludeE2E,
    [switch]$List
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Проекты без собственных тестов (общие фикстуры/хелперы).
$alwaysExclude = @('Mars.Test.Common', 'ExternalServices.TestContainers')
# E2E/браузер/докер-имидж — вне дефолтного прогона.
$e2eExclude = @('Mars.E2E.Tests', 'Mars.DockerImage.Tests')

# Грубые длительности измерительного прогона (2026-09) — только для порядка запуска
# (самые долгие стартуют первыми, чтобы сократить общее время при лимите воркеров).
$orderHint = @{
    'Mars.Integration.Tests'              = 155
    'Mars.WebApiClient.Integration.Tests' = 86
    'ExternalServices.Integration.Tests'  = 30
    'Mars.SiteEngine.Integration.Tests'   = 25
    'Test.Mars.MetaModelGenerator'        = 20
    'Mars.Datasource.Integration.Tests'   = 19
    'Mars.Plugin.Integration.Tests'       = 18
    'Mars.HttpSmartAuthFlow.Integration.Tests' = 17
    'Mars.Cli.EndToEnd.Tests'             = 9
    'Mars.Nodes.Tests'                    = 8
    'Mars.Plugin.Tests'                   = 4
    'Mars.Server.Tests'                   = 3
    'Mars.SiteEngine.Tests'               = 2
    'Mars.Admin.Framework.Tests'          = 1
    'Test.EditorJsBlazored'               = 1
    'Mars.AiServices.Integration.Tests'   = 1
    'Mars.Core.Tests'                     = 1
}

function Get-TestProjects {
    $testsDir = Join-Path $root 'tests'
    $csprojs = Get-ChildItem -Path $testsDir -Recurse -Filter *.csproj |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

    $projects = foreach ($csproj in $csprojs) {
        if ($csproj.BaseName -in $alwaysExclude) { continue }
        if (-not $IncludeE2E -and $csproj.BaseName -in $e2eExclude) { continue }
        $content = Get-Content -Raw $csproj.FullName
        if ($content -notmatch 'Microsoft.Testing.Platform') { continue }
        $binDir = Join-Path $csproj.DirectoryName "bin\$Configuration\$Framework"
        [pscustomobject]@{
            Name = $csproj.BaseName
            Dir  = $csproj.DirectoryName
            Bin  = $binDir
            Exe  = Join-Path $binDir ($csproj.BaseName + '.exe')
        }
    }

    $projects | Sort-Object -Property {
        if ($orderHint.ContainsKey($_.Name)) { $orderHint[$_.Name] } else { [int]::MaxValue }
    } -Descending
}

$projects = @(Get-TestProjects)
if ($projects.Count -eq 0) {
    Write-Error 'No test projects discovered.'
}

if ($List) {
    $projects | ForEach-Object { Write-Output $_.Exe }
    exit 0
}

if (-not $SkipBuild) {
    Write-Host "==> dotnet build Mars.slnx -c $Configuration"
    & dotnet build (Join-Path $root 'Mars.slnx') -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed (exit $LASTEXITCODE)."
    }
}

$runDir = Join-Path (Join-Path $env:TEMP 'mars-test-runs') (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Path $runDir | Out-Null
Write-Host "==> $($projects.Count) projects, max $MaxParallel parallel, logs: $runDir"
Write-Host ''

$running = [System.Collections.Generic.List[object]]::new()
$results = [System.Collections.Generic.List[object]]::new()

function Wait-ForFreeSlot {
    while ($running.Count -ge $MaxParallel) {
        Collect-Finished
        if ($running.Count -ge $MaxParallel) { Start-Sleep -Milliseconds 250 }
    }
}

function Collect-Finished {
    foreach ($r in @($running)) {
        $exited = $r.Proc.WaitForExit(0)
        if ($exited) {
            [void]$running.Remove($r)
            $r.Sw.Stop()
            $results.Add($r)
        }
    }
}

foreach ($p in $projects) {
    Wait-ForFreeSlot

    if (-not (Test-Path $p.Exe)) {
        $results.Add([pscustomobject]@{
            Name = $p.Name
            Proc = $null
            Sw   = [System.Diagnostics.Stopwatch]::new()
            Exit = 'MISSING'
            Out  = ''
            Err  = ''
        })
        Write-Host ("MISSING {0}" -f $p.Exe)
        continue
    }

    $outLog = Join-Path $runDir ($p.Name + '.out.log')
    $errLog = Join-Path $runDir ($p.Name + '.err.log')
    $proc = Start-Process -FilePath $p.Exe -WorkingDirectory $p.Bin -PassThru -NoNewWindow `
        -RedirectStandardOutput $outLog -RedirectStandardError $errLog

    $running.Add([pscustomobject]@{
        Name = $p.Name
        Proc = $proc
        Sw   = [System.Diagnostics.Stopwatch]::StartNew()
        Out  = $outLog
        Err  = $errLog
        Failed = 0
    })
    Write-Host ("START {0}" -f $p.Name)
}

while ($running.Count -gt 0) {
    foreach ($r in @($running)) {
        if ($r.Sw.Elapsed.TotalSeconds -gt $TimeoutSec) {
            Write-Host ("TIMEOUT {0} ({1}s)" -f $r.Name, $TimeoutSec)
            & taskkill /PID $r.Proc.Id /T /F 2>&1 | Out-Null
            $r.Proc.WaitForExit()
            [void]$running.Remove($r)
            $r.Sw.Stop()
            $r.Exit = 'TIMEOUT'
            $results.Add($r)
        }
    }
    Collect-Finished
    if ($running.Count -gt 0) { Start-Sleep -Milliseconds 250 }
}

function Get-Summary {
    param($logPath)
    if (-not (Test-Path $logPath)) { return @{ Total = 0; Failed = 0; Summary = '' } }
    $text = Get-Content -Raw $logPath
    if ($null -eq $text) { $text = '' }
    $m = [regex]::Match($text, 'Total:\s*(\d+),\s*Errors:\s*(\d+),\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)')
    if ($m.Success) {
        return @{ Total = [int]$m.Groups[1].Value; Failed = [int]$m.Groups[3].Value; Summary = $m.Value }
    }
    return @{ Total = 0; Failed = 0; Summary = '' }
}

Write-Host ''
Write-Host ('--- results: ' + $runDir + ' ---')

$totalTests = 0
$totalFailed = 0
$bad = 0
foreach ($r in ($results | Sort-Object Name)) {
    $exitCode = if ($null -ne $r.Exit) { $r.Exit } else { $r.Proc.ExitCode }
    $sum = Get-Summary $r.Out
    $totalTests += $sum.Total
    $totalFailed += $sum.Failed
    $secs = [math]::Round($r.Sw.Elapsed.TotalSeconds, 1)
    if ($exitCode -isnot [int] -or $exitCode -ne 0) { $bad++ }
    $line = "{0,-45} {1,8}s  exit={2,-4} tests={3,-5} failed={4}" -f $r.Name, $secs, $exitCode, $sum.Total, $sum.Failed
    Write-Host $line
    if ($exitCode -is [int] -and $exitCode -ne 0) {
        $tail = Get-Content $r.Out -ErrorAction SilentlyContinue | Select-Object -Last 20
        if ($tail) { $tail | ForEach-Object { Write-Host ('    | ' + $_) } }
        if ((Get-Item $r.Err -ErrorAction SilentlyContinue).Length -gt 0) {
            Write-Host ('    | stderr: ' + $r.Err)
        }
    }
}

Write-Host ''
Write-Host ("total tests: {0}, failed: {1}, bad projects: {2}" -f $totalTests, $totalFailed, $bad)
if ($bad -gt 0) { exit 1 }
