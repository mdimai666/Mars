[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$dateStamp = Get-Date -UFormat "%Y-%m-%d_%H-%M-%S"
$log_filename = "logs/nuget-deploy-$dateStamp.local.log"

Start-Transcript -path $log_filename -append
$startTime = Get-Date

$dirs = @(
    # shared
    "Mars.Core",
    "Mars.Shared",
    "Mars.Options/Mars.Options",
    "AppFront.Shared",
    "AppFront.Main",
    # host
    "Mars.Host.Shared",
    "Mars.Host.Data",
    "Mars.Host.Data.PostgreSQL",
    # nodes
    "Mars.Nodes/Mars.Nodes.Core",
    "Mars.Nodes/Mars.Nodes.Core.Implements",
    "Mars.Nodes/Mars.Nodes.Host.Shared",
    "Mars.Nodes/Mars.Nodes.Front.Shared",
    "Mars.Nodes/Mars.Nodes.FormEditor",
    # modules
    "Modules/MarsEditors",
    "Modules/MarsCodeEditor2",
    "Mars.WebApiClient",
    "Modules/BlazoredHtmlRender",
    "Modules/Mars.HttpSmartAuthFlow",
    # plugin
    "Plugin/Mars.Plugin.Abstractions",
    "Plugin/Mars.Plugin.Front",
    "Plugin/Mars.Plugin.Kit.Host",
    "Plugin/Mars.Plugin.Kit.Front",
    "Plugin/Mars.Plugin.PluginHost",
    "Plugin/Mars.Plugin.Front.Abstractions",
    "Plugin/Mars.Plugin.PluginPublishScript"
)

$MarsAppVersion = (Select-String -Path ..\..\Directory.Packages.props -Pattern "<MarsAppVersion>(.+?)</MarsAppVersion>").Matches.Groups[1].Value

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📦 Публикация NuGet пакетов" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🔖 Версия: $MarsAppVersion" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host

foreach ($d in $dirs) {
    $pname = ($d.split('/')[-1])
    $csproj = "$pname.csproj"
    Write-Host "  📄 $csproj " -NoNewline -ForegroundColor White
    
    $csproj_file = "../$d/$csproj"
    $regPackageVersion = "<PackageVersion>(.+?)</PackageVersion>"
    
    $version = (Select-String -Path $csproj_file -Pattern $regPackageVersion).Matches.Groups[1].Value.Replace('$(MarsAppVersion)', $MarsAppVersion)
    Write-Host "→ v$version" -ForegroundColor Cyan
}

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

$apikey = [System.Environment]::GetEnvironmentVariable('NUGET_MARS')
function CheckNugetKey() {
    $_apikey = [System.Environment]::GetEnvironmentVariable('NUGET_MARS')
    if ([string]::IsNullOrEmpty($_apikey)) {
        Write-Host "❌ Переменная окружения NUGET_MARS не настроена" -ForegroundColor Red
        exit
    }
}

$isDeployRelease = If ($args) { $args[0].Contains("deploy-release") } else { $false }
Write-Host "🚀 Режим публикации: " -NoNewline
if ($isDeployRelease) {
    Write-Host "Release (NuGet.org)" -ForegroundColor Green
} else {
    Write-Host "Local" -ForegroundColor Yellow
}

CheckNugetKey

$ans = Read-Host "📋 Опубликовать все пакеты? [y]"

if ($ans -ne "y") {
    Write-Host "⛔ Отменено" -ForegroundColor Yellow
    Stop-Transcript
    exit
}

Write-Host
Write-Host "⚙️  Начинаем публикацию..." -ForegroundColor Green
Write-Host

$__DIR__ = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\')
$totalDirs = $dirs.Count
$currentIndex = 0

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
    
    # Подсчёт эмодзи в статусе (примерный список используемых эмодзи)
    $emojiCount = 0
    $emojiCount += ([regex]::Matches($statusTruncated, '⏳')).Count
    $emojiCount += ([regex]::Matches($statusTruncated, '📦')).Count
    $emojiCount += ([regex]::Matches($statusTruncated, '🚀')).Count
    $emojiCount += ([regex]::Matches($statusTruncated, '✅')).Count
    $emojiCount += ([regex]::Matches($statusTruncated, '🎉')).Count
    $visibleLength = [System.Globalization.StringInfo]::ParseCombiningCharacters($statusTruncated).Length

    # Формируем строки прогресс-бара (эмодзи = 2 символа ширины)
    $line1 = "┌─────────────────────────────────────────────────────────────────┐"
    
    $progressText = "Прогресс: $Current/$Total ($percent%)"
    $line2 = "│ 🔄 $progressText" + (" " * (63 - $progressText.Length - 2)) + "│"
    
    $line3 = "│ $bar" + (" " * (63 - $bar.Length + 1)) + "│"
    
    $line4 = "│ 📦 $itemTruncated" + (" " * (63 - $itemTruncated.Length - 2)) + "│"
    
    $line5 = "│ $statusTruncated" + (" " * (63 - $visibleLength + 0)) + "│"
    
    $line6 = "└─────────────────────────────────────────────────────────────────┘"
    
    # Возвращаемся на 6 строк назад (если не первый вызов)
    if ($script:progressDrawn) {
        Write-Host "`r`e[6A" -NoNewline
    }
    
    # Выводим прогресс-бар
    Write-Host $line1 -ForegroundColor DarkGray
    Write-Host $line2 -ForegroundColor Cyan
    Write-Host $line3 -ForegroundColor Green
    Write-Host $line4 -ForegroundColor White
    Write-Host $line5 -ForegroundColor Cyan
    Write-Host $line6 -ForegroundColor DarkGray
    
    $script:progressDrawn = $true
}

$script:progressDrawn = $false

foreach ($d in $dirs) {
    $currentIndex++
    $pname = ($d.split('/')[-1])
    
    Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem $pname -Status "⏳ Подготовка..."
    
    cd $__DIR__
    cd "../$d/"
    
    $csproj = "$pname.csproj"
    $csproj_file = "$csproj"
    $regPackageVersion = "<PackageVersion>(.+?)</PackageVersion>"
    $version = (Select-String -Path $csproj_file -Pattern $regPackageVersion).Matches.Groups[1].Value.Replace('$(MarsAppVersion)', $MarsAppVersion)
    
    Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "📦 Упаковка..."
    
    try {
        if ($isDeployRelease) {
            $processResult = dotnet pack --configuration Release -p:DebugType=portable -p:DebugSymbols=true
            if ($LASTEXITCODE -ne 0) {
                throw "Ошибка при упаковке пакета"
            }
            
            Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "🚀 Публикация в NuGet.org..."
            
            $processResult = dotnet nuget push ".\bin\Release\*.$version.nupkg" --api-key $apikey --source https://api.nuget.org/v3/index.json
            if ($LASTEXITCODE -ne 0) {
                throw "Ошибка при публикации пакета в NuGet.org"
            }
        }
        else {
            $processResult = dotnet pack --configuration Release -o "C:\Users\D\Documents\VisualStudio\_LocalNugets" --include-source  -p:DebugType=portable -p:DebugSymbols=true
            if ($LASTEXITCODE -ne 0) {
                throw "Ошибка при локальной упаковке пакета"
            }
        }
        
        Draw-ProgressBar -Current $currentIndex -Total $totalDirs -CurrentItem "$pname v$version" -Status "✅ Завершено!"
        Start-Sleep -Milliseconds 200
    }
    catch {
        Write-Host
        Write-Host
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
        Write-Host "❌ ОШИБКА: $pname v$version" -ForegroundColor Red
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
        Write-Host "   $_" -ForegroundColor Yellow
        Write-Host "   Код выхода: $LASTEXITCODE" -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
        cd $__DIR__
        $processResult | ForEach-Object { Write-Host $_ }
        Stop-Transcript
        Read-Host "Нажмите Enter для выхода"
        return
    }
    
    cd $__DIR__
}

# Финальный прогресс-бар
Draw-ProgressBar -Current $totalDirs -Total $totalDirs -CurrentItem "Все пакеты" -Status "🎉 Все операции завершены успешно!"

$endTime = Get-Date
cd $__DIR__

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "✨ Готово!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
$elapsed = ($endTime - $startTime).TotalSeconds
Write-Host ("TotalSeconds {0:F1}s" -f $elapsed)

Stop-Transcript
