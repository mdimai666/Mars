param(
    # Локальный NuGet-фид; по умолчанию ~/Documents/VisualStudio/_LocalNugets
    [string]$OutDir = (Join-Path $HOME "Documents\VisualStudio\_LocalNugets")
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

# Скрипт лежит в корне репо
$root = $PSScriptRoot

# Версия из Directory.Build.props (единый источник, см. конвенцию MarsAppVersion)
$propsPath = Join-Path $root "Directory.Build.props"
$version = (Select-String -Path $propsPath -Pattern "<MarsAppVersion>(.+?)</MarsAppVersion>").Matches.Groups[1].Value
if (-not $version) {
    Write-Host "❌ Не найден MarsAppVersion в $propsPath" -ForegroundColor Red
    exit 1
}

# Пакуемые проекты = все csproj в src/, у которых объявлен <PackageId>
$packables = Get-ChildItem -Path (Join-Path $root "src") -Recurse -Filter *.csproj |
    Where-Object { Select-String -Path $_.FullName -Pattern "<PackageId>" -Quiet }

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📦 Локальная упаковка NuGet" -ForegroundColor Green
Write-Host "🔖 Версия: $version   |   проектов: $($packables.Count)" -ForegroundColor Yellow
Write-Host "📂 Фид: $outDir" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host

$ans = Read-Host "📋 Упаковать все пакеты? [y]"
if ($ans -ne "y") {
    Write-Host "⛔ Отменено" -ForegroundColor Yellow
    exit
}

# Mars.Plugin.Sdk пакует манифесты из вывода сборки Mars.WebApp — решение должно быть собрано (Release).
Write-Host "🔨 Сборка решения (Release)..." -ForegroundColor Cyan
dotnet build (Join-Path $root "src/Mars.WebApp/Mars.WebApp.csproj") --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Сборка не удалась" -ForegroundColor Red
    exit 1
}

# Функция для отрисовки прогресс-бара
function Draw-ProgressBar {
    param(
        [int]$Current,
        [int]$Total,
        [string]$CurrentItem,
        [string]$Status
    )

    $percent = [math]::Round(($Current / $Total) * 100)
    $barWidth = 63
    $filled = [math]::Round(($percent / 100) * $barWidth)
    $empty = $barWidth - $filled

    $bar = "█" * $filled + "░" * $empty

    $itemTruncated = if ($CurrentItem.Length -gt 58) { $CurrentItem.Substring(0, 55) + "..." } else { $CurrentItem }
    $statusTruncated = if ($Status.Length -gt 58) { $Status.Substring(0, 55) + "..." } else { $Status }
    $visibleLength = [System.Globalization.StringInfo]::ParseCombiningCharacters($statusTruncated).Length

    # Формируем строки прогресс-бара (эмодзи = 2 символа ширины)
    $line1 = "┌─────────────────────────────────────────────────────────────────┐"

    $progressText = "Прогресс: $Current/$Total ($percent%)"
    $line2 = "│ 🔄 $progressText" + (" " * (63 - $progressText.Length - 2)) + "│"

    $line3 = "│ $bar" + (" " * (63 - $bar.Length + 1)) + "│"

    $line4 = "│ 📦 $itemTruncated" + (" " * (63 - $itemTruncated.Length - 2)) + "│"

    $line5 = "│ $statusTruncated" + (" " * (63 - $visibleLength)) + "│"

    $line6 = "└─────────────────────────────────────────────────────────────────┘"

    # Возвращаемся на 6 строк назад (если не первый вызов)
    if ($script:progressDrawn) {
        Write-Host "`r`e[6A" -NoNewline
    }

    Write-Host $line1 -ForegroundColor DarkGray
    Write-Host $line2 -ForegroundColor Cyan
    Write-Host $line3 -ForegroundColor Green
    Write-Host $line4 -ForegroundColor White
    Write-Host $line5 -ForegroundColor Cyan
    Write-Host $line6 -ForegroundColor DarkGray

    $script:progressDrawn = $true
}

$script:progressDrawn = $false
$failures = @()
$totalDirs = $packables.Count
$currentIndex = 0

foreach ($p in $packables) {
    $currentIndex++
    $pname = $p.BaseName

    Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem $pname -Status "⏳ Подготовка..."

    Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "📦 Упаковка..."

    $ErrorActionPreference = "Continue"
    $processResult = dotnet pack $p.FullName --configuration Release -o $outDir --include-source -p:PackWithSymbols=true 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = "Stop"

    if ($exitCode -ne 0) {
        $failures += $pname
        $processResult | ForEach-Object { Write-Host $_ }
        Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "❌ Ошибка (код $exitCode)"
    }
    else {
        Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "✅ Завершено!"
    }
    Start-Sleep -Milliseconds 200
}

if ($failures.Count -eq 0) {
    Draw-ProgressBar -Current $totalDirs -Total $totalDirs -CurrentItem "Все пакеты" -Status "🎉 Все операции завершены успешно!"
}
else {
    Draw-ProgressBar -Current $totalDirs -Total $totalDirs -CurrentItem "Все пакеты" -Status "⚠️ Завершено с ошибками ($($failures.Count))"
}

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
if ($failures.Count -eq 0) {
    Write-Host "✨ Готово: $($packables.Count) пакетов в $outDir" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Завершено с ошибками ($($failures.Count)): $($failures -join ', ')" -ForegroundColor Red
}
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
