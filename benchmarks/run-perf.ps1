<#
.SYNOPSIS
    Нагрузочный прогон Mars: герметичный стенд (Kestrel + Testcontainers PostgreSQL) + k6.

.DESCRIPTION
    1. Собирает стенд benchmarks/Mars.Performance.Stand.
    2. Поднимает его: контейнер PostgreSQL 14, миграции, сидинг (админ, фронт с дефолтной
       темой, N постов), прогрев и smoke-проверки.
    3. Прогоняет k6-сценарии последовательно (или все параллельно через -Parallel).
    4. Останавливает стенд; JSON-сводки k6 кладёт в benchmarks/results/.

.EXAMPLE
    .\benchmarks\run-perf.ps1
    .\benchmarks\run-perf.ps1 -Scenarios render_static_anon, post_read
    .\benchmarks\run-perf.ps1 -Parallel -Posts 5000
#>
param(
    [int]$Posts = 1000,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string[]]$Scenarios = @(),
    # smoke — короткие 1000 итераций (~минута, ловит явные просадки);
    # full  — 30-секундные окна (полный замер, ~5 минут)
    [ValidateSet('smoke', 'full')]
    [string]$Mode = 'full',
    [switch]$Parallel,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$standCsproj = Join-Path $repoRoot 'benchmarks\Mars.Performance.Stand\Mars.Performance.Stand.csproj'
$standDll = Join-Path $repoRoot "benchmarks\Mars.Performance.Stand\bin\$Configuration\net10.0\Mars.Performance.Stand.dll"
$k6Script = Join-Path $repoRoot 'benchmarks\k6\run-all.js'
$resultsDir = Join-Path $repoRoot 'benchmarks\results'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'

# post_create последним: он раздувает БД тысячами постов, и последующие
# чтения по slug деградируют (индекса на Slug нет — seq scan).
$allScenarios = @(
    'render_static_anon', 'render_static_auth',
    'render_db_anon', 'render_db_auth',
    'post_read', 'post_list', 'post_update', 'post_create'
)

function ConvertTo-SafeFolderName([string]$name) {
    $invalid = [System.IO.Path]::GetInvalidFileNameChars()
    $safe = -join ($name.ToCharArray() | ForEach-Object { if ($invalid -contains $_) { '_' } else { $_ } })
    if (-not $safe) { $safe = 'unknown' }
    return $safe
}

# При запуске через `powershell -File` массив приходит одной строкой "a,b" — нормализуем
$Scenarios = @($Scenarios | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$historyFile = Join-Path $repoRoot 'benchmarks\history.csv'
$historyHeader = 'timestamp,commit,version,mode,scenario,rps,avg_ms,p95_ms,failed_pct'

function Format-Inv([double]$value, [string]$format) {
    return $value.ToString($format, [Globalization.CultureInfo]::InvariantCulture)
}

function Get-K6Summary([string]$path) {
    $json = Get-Content $path -Raw | ConvertFrom-Json
    $duration = $json.metrics.http_req_duration
    $failed = $json.metrics.http_req_failed
    $failedValue = $null
    if ($failed) {
        if ($null -ne $failed.value) { $failedValue = $failed.value }
        elseif ($null -ne $failed.rate) { $failedValue = $failed.rate }
    }
    return [pscustomobject]@{
        Rps       = [double]$json.metrics.iterations.rate
        AvgMs     = [double]$duration.avg
        P95Ms     = [double]$duration.'p(95)'
        FailedPct = [double]$failedValue * 100
    }
}

function Add-HistoryRow([string]$timestamp, [string]$commit, [string]$version, [string]$mode, [string]$scenario, $summary) {
    if (-not (Test-Path $historyFile)) {
        Set-Content $historyFile $historyHeader -Encoding utf8
    }
    $row = @(
        $timestamp,
        $commit,
        $version,
        $mode,
        $scenario,
        (Format-Inv $summary.Rps '0'),
        (Format-Inv $summary.AvgMs '0.##'),
        (Format-Inv $summary.P95Ms '0.##'),
        (Format-Inv $summary.FailedPct '0.##')
    ) -join ','
    Add-Content $historyFile $row -Encoding utf8
}

# ---------------------------------------------------------------- prerequisites
$k6 = Get-Command k6 -ErrorAction SilentlyContinue
if (-not $k6) {
    $candidate = Join-Path $env:ProgramFiles 'k6\k6.exe'
    if (Test-Path $candidate) {
        $k6 = $candidate
    }
    else {
        throw 'k6 не найден. Установка: winget install GrafanaLabs.k6'
    }
}
else {
    $k6 = $k6.Source
}

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw 'Docker не запущен — Testcontainers требует работающий Docker Desktop.'
}

# ---------------------------------------------------------------- build
if (-not $SkipBuild) {
    Write-Host 'Сборка стенда...' -ForegroundColor Cyan
    dotnet build $standCsproj -c $Configuration --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Сборка стенда завершилась с ошибкой.' }
}
if (-not (Test-Path $standDll)) { throw "Стенд не найден: $standDll (соберите без -SkipBuild)." }

# ---------------------------------------------------------------- start stand
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$readyFile = Join-Path $env:TEMP "mars-perf-ready-$PID.txt"
$standLog = Join-Path $resultsDir "stand-$timestamp.log"
Remove-Item $readyFile -ErrorAction SilentlyContinue

Write-Host "Запуск стенда (постов: $Posts)..." -ForegroundColor Cyan
$stand = Start-Process -FilePath 'dotnet' `
    -ArgumentList "`"$standDll`" --posts $Posts --ready-file `"$readyFile`" --wait-minutes 60" `
    -PassThru -NoNewWindow `
    -RedirectStandardOutput $standLog `
    -RedirectStandardError "$standLog.err"

$url = $null
$appVersion = 'unknown'
$deadline = (Get-Date).AddMinutes(5)
while (-not $url -and -not $stand.HasExited -and (Get-Date) -lt $deadline) {
    if (Test-Path $readyFile) {
        # ready-файл: строка 1 — URL, строка 2 — версия приложения
        $lines = @(Get-Content $readyFile)
        $url = $lines[0].Trim()
        if ($lines.Count -gt 1 -and $lines[1].Trim()) { $appVersion = $lines[1].Trim() }
    }
    else {
        Start-Sleep -Milliseconds 500
    }
}
if (-not $url) {
    if (-not $stand.HasExited) { Stop-Process -Id $stand.Id -Force -ErrorAction SilentlyContinue }
    Write-Host '--- лог стенда ---' -ForegroundColor Yellow
    Get-Content $standLog -ErrorAction SilentlyContinue
    Get-Content "$standLog.err" -ErrorAction SilentlyContinue
    throw 'Стенд не поднялся за 5 минут.'
}
Write-Host "Стенд готов: $url (версия $appVersion)" -ForegroundColor Green

# Результаты раскладываются по папкам с версией приложения: results/<версия>/.
# Папка — базовая версия без git-SHA (иначе каждая сборка давала бы новую папку).
$versionFolder = $appVersion.Split('+')[0]
$versionDir = Join-Path $resultsDir (ConvertTo-SafeFolderName $versionFolder)
New-Item -ItemType Directory -Force -Path $versionDir | Out-Null

# ---------------------------------------------------------------- k6 runs
$env:MARS_URL = $url
$env:MARS_POSTS = "$Posts"
$env:MARS_MODE = $Mode
$exitCodes = [ordered]@{}

$gitCommit = & git -C $repoRoot rev-parse --short HEAD 2>$null
if (-not $gitCommit) { $gitCommit = 'unknown' }

# История: строки, записанные ДО этого прогона — база для сравнения
$history = @()
if (Test-Path $historyFile) { $history = @(Import-Csv $historyFile) }
$comparisons = @()

try {
    if ($Parallel) {
        Remove-Item Env:\MARS_SCENARIO -ErrorAction SilentlyContinue
        Write-Host ''
        Write-Host '=== k6: все сценарии параллельно ===' -ForegroundColor Cyan
        & $k6 run --summary-export (Join-Path $versionDir "k6-$timestamp-all.json") $k6Script
        $exitCodes['all'] = $LASTEXITCODE
    }
    else {
        if ($Scenarios.Count -gt 0) { $allScenarios = $Scenarios }
        foreach ($s in $allScenarios) {
            Write-Host ''
            Write-Host "=== k6: $s ===" -ForegroundColor Cyan
            $env:MARS_SCENARIO = $s
            $summaryPath = Join-Path $versionDir "k6-$timestamp-$s.json"
            & $k6 run --summary-export $summaryPath $k6Script
            $exitCodes[$s] = $LASTEXITCODE

            # история + сравнение с последним прогоном того же режима
            $summary = Get-K6Summary $summaryPath
            $prev = $history | Where-Object { $_.mode -eq $Mode -and $_.scenario -eq $s } | Select-Object -Last 1
            $prevRps = $null
            $prevP95 = $null
            if ($prev) {
                $prevRps = [double]$prev.rps
                $prevP95 = [double]$prev.p95_ms
            }
            $comparisons += [pscustomobject]@{
                Scenario  = $s
                Rps       = $summary.Rps
                PrevRps   = $prevRps
                P95Ms     = $summary.P95Ms
                PrevP95Ms = $prevP95
            }
            Add-HistoryRow (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $gitCommit $versionFolder $Mode $s $summary
        }
    }
}
finally {
    Remove-Item Env:\MARS_SCENARIO -ErrorAction SilentlyContinue
    Remove-Item Env:\MARS_URL -ErrorAction SilentlyContinue
    Remove-Item Env:\MARS_POSTS -ErrorAction SilentlyContinue
    Remove-Item Env:\MARS_MODE -ErrorAction SilentlyContinue
    Remove-Item $readyFile -ErrorAction SilentlyContinue

    if (-not $stand.HasExited) {
        Write-Host ''
        Write-Host 'Остановка стенда (PostgreSQL-контейнер подберёт Testcontainers Ryuk)...' -ForegroundColor Cyan
        Stop-Process -Id $stand.Id -Force -ErrorAction SilentlyContinue
        $stand.WaitForExit(15000) | Out-Null
    }

    # Лог стенда переезжает в папку версии после остановки процесса (пока стенд жив, файл занят)
    Move-Item $standLog (Join-Path $versionDir (Split-Path $standLog -Leaf)) -Force -ErrorAction SilentlyContinue
    Move-Item "$standLog.err" (Join-Path $versionDir (Split-Path "$standLog.err" -Leaf)) -Force -ErrorAction SilentlyContinue
    $standLog = Join-Path $versionDir (Split-Path $standLog -Leaf)
}

# ---------------------------------------------------------------- summary
Write-Host ''
Write-Host '=== Итог ===' -ForegroundColor Green
foreach ($name in $exitCodes.Keys) {
    $code = $exitCodes[$name]
    if ($code -eq 0) {
        Write-Host ("  {0,-20} OK" -f $name)
    }
    else {
        Write-Host ("  {0,-20} FAIL (exit {1})" -f $name, $code) -ForegroundColor Red
    }
}
Write-Host "Результаты ($appVersion): $versionDir"
Write-Host "Лог стенда:               $standLog"
Write-Host "История:                  $historyFile"

# ---------------------------------------------------------------- сравнение с историей
if ($comparisons.Count -gt 0) {
    Write-Host ''
    Write-Host "=== Сравнение с предыдущим прогоном (режим: $Mode) ===" -ForegroundColor Green
    Write-Host ('{0,-20} {1,10} {2,10} {3,8} {4,10} {5,10} {6,8}' -f 'сценарий', 'RPS', 'пред.', 'dRPS%', 'p95,мс', 'пред.', 'dp95%')
    foreach ($c in $comparisons) {
        $deltaRps = '—'
        $deltaP95 = '—'
        $warn = $false
        if ($null -ne $c.PrevRps -and $c.PrevRps -gt 0) {
            $v = ($c.Rps - $c.PrevRps) / $c.PrevRps * 100
            $deltaRps = Format-Inv $v '+0.0;-0.0;0.0'
            if ($v -le -20) { $warn = $true }
        }
        if ($null -ne $c.PrevP95Ms -and $c.PrevP95Ms -gt 0) {
            $v = ($c.P95Ms - $c.PrevP95Ms) / $c.PrevP95Ms * 100
            $deltaP95 = Format-Inv $v '+0.0;-0.0;0.0'
            if ($v -ge 25) { $warn = $true }
        }
        $prevRpsStr = '—'
        if ($null -ne $c.PrevRps) { $prevRpsStr = [int]$c.PrevRps }
        $prevP95Str = '—'
        if ($null -ne $c.PrevP95Ms) { $prevP95Str = Format-Inv $c.PrevP95Ms '0.##' }
        $line = '{0,-20} {1,10} {2,10} {3,8} {4,10} {5,10} {6,8}' -f $c.Scenario, [int]$c.Rps, $prevRpsStr, $deltaRps, (Format-Inv $c.P95Ms '0.##'), $prevP95Str, $deltaP95
        if ($warn) {
            Write-Host "$line   !!!" -ForegroundColor Red
        }
        else {
            Write-Host $line
        }
    }
    Write-Host "(!!! — подозрение на регрессию: RPS упал на 20%+ или p95 вырос на 25%+)"
}

$failedCount = @($exitCodes.Values | Where-Object { $_ -ne 0 }).Count
if ($failedCount -gt 0) { exit 1 }
exit 0
