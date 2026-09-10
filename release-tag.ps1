# Выпуск релиза: создаёт и пушит тег v<MarsAppVersion>.
# Перед созданием проверяет:
#   - рабочее дерево и что всё запушено в origin/master;
#   - что тег ещё не существует (локально/на origin);
#   - что версия ещё не опубликована на nuget.org (по mdimai666.Mars.Core).
# Пуш тега запускает .github/workflows/nuget-publish.yml (триггер tags: v*).
# Флаг -y — не спрашивать подтверждений (для CI/агента).

param([switch]$y)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot

# Версия из Directory.Build.props (единый источник, см. конвенцию MarsAppVersion)
$propsPath = Join-Path $root "Directory.Build.props"
$version = (Select-String -Path $propsPath -Pattern "<MarsAppVersion>(.+?)</MarsAppVersion>").Matches.Groups[1].Value
if (-not $version) {
    Write-Host "❌ Не найден MarsAppVersion в $propsPath" -ForegroundColor Red
    exit 1
}

$tag = "v$version"

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🚀 Выпуск релиза Mars $version" -ForegroundColor Green
Write-Host "📦 Тег: $tag" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host

# --- 1. Ветка и рабочее дерево -------------------------------------------------
$branch = git branch --show-current
if ($branch -ne "master") {
    Write-Host "❌ Релиз только из ветки master, сейчас: $branch" -ForegroundColor Red
    exit 1
}

$dirty = git status --porcelain
if ($dirty) {
    Write-Host "⚠️  В рабочем дереве незакоммиченные изменения:" -ForegroundColor Yellow
    $dirty | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    if (-not $y) {
        $ans = Read-Host "Продолжить? [y]"
        if ($ans -ne "y") {
            Write-Host "⛔ Отменено" -ForegroundColor Yellow
            exit
        }
    }
}

git fetch origin --quiet
$unpushed = [int](git rev-list --count "origin/master..HEAD")
if ($unpushed -gt 0) {
    Write-Host "❌ $unpushed коммит(ов) не запушено в origin/master. Сначала push." -ForegroundColor Red
    exit 1
}

# --- 2. Тег ещё не существует --------------------------------------------------
$localTag = git tag -l $tag
$remoteTag = git ls-remote --tags origin $tag
if ($localTag -or $remoteTag) {
    Write-Host "❌ Тег $tag уже существует (локально: $([bool]$localTag), на origin: $([bool]$remoteTag))." -ForegroundColor Red
    exit 1
}

# --- 3. Версия ещё не опубликована на nuget.org --------------------------------
$package = "mdimai666.mars.core"
$indexUrl = "https://api.nuget.org/v3-flatcontainer/$package/index.json"
Write-Host "🔍 Проверяю nuget.org: $package $version ..." -ForegroundColor Cyan
try {
    $index = Invoke-RestMethod -Uri $indexUrl -TimeoutSec 30
    $published = $index.versions | Where-Object { $_ -ieq $version }
    if ($published) {
        Write-Host "❌ Версия $version уже опубликована на nuget.org ($package)." -ForegroundColor Red
        Write-Host "   Подними MarsAppVersion в $propsPath и коммить." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "✅ Версия $version на nuget.org свободна." -ForegroundColor Green
}
catch {
    if ([int]$_.Exception.Response.StatusCode -eq 404) {
        Write-Host "ℹ️  Пакет $package ещё не существует — первый релиз." -ForegroundColor DarkGray
    }
    else {
        Write-Host "⚠️  Не удалось проверить nuget.org: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "   Продолжаю без проверки nuget.org." -ForegroundColor DarkGray
    }
}

# --- 4. Создание и пуш тега ----------------------------------------------------
Write-Host
if (-not $y) {
    $ans = Read-Host "Создать и запушить тег $tag? [y]"
    if ($ans -ne "y") {
        Write-Host "⛔ Отменено" -ForegroundColor Yellow
        exit
    }
}

git tag -a $tag -m "Release $version"
git push origin $tag

Write-Host
Write-Host "✅ Тег $tag запушен. CI запустится автоматически:" -ForegroundColor Green
Write-Host "   https://github.com/mdimai666/Mars/actions" -ForegroundColor Cyan
