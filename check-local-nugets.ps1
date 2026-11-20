# Валидация локального NuGet-фида: можно ли установить kit-наборы (Plugin.Kit.Host / Kit.Front / Sdk).
# Read-only: ничего не меняет, только читает nupkg и csproj. Exit 1 при ошибках (warning'ы не валят).
# Проверки:
#   1. csproj: пакуемые проекты (с PackageId) не ссылаются на проекты без PackageId
#      (иначе dotnet pack пишет в nuspec голое имя проекта — пакет не установить).
#   2. nuspec: нет голых зависимостей Mars.* / имён непакуемых проектов.
#   3. Каждая mdimai666.* зависимость присутствует в фиде в подходящей версии.
#   4. Замыкания Kit.Host / Kit.Front / Plugin.Sdk резолвятся целиком;
#      с -Online сторонние зависимости дополнительно проверяются на nuget.org.
#   5. Warning: nupkg старше csproj/props — dotnet pack молча пропускает существующий пакет,
#      такая версия могла не обновиться (удалить nupkg и перепаковать).
param(
    [string]$OutDir = (Join-Path $HOME "Documents\VisualStudio\_LocalNugets"),
    [switch]$Online
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = $PSScriptRoot
$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

# --- NuGet-версии: парсер и сравнение (prerelease по правилам SemVer) ---------
function Parse-NuGetVersion([string]$v) {
    $v = $v.Split('+')[0]
    $pre = $null
    $i = $v.IndexOf('-')
    if ($i -ge 0) { $pre = $v.Substring($i + 1); $v = $v.Substring(0, $i) }
    $parts = $v.Split('.')
    $nums = @(0, 0, 0)
    for ($k = 0; $k -lt [Math]::Min(3, $parts.Count); $k++) {
        if ($parts[$k]) { $nums[$k] = [int]$parts[$k] }
    }
    return @{ Num = $nums; Pre = $pre }
}

function Compare-NuGetVersion($a, $b) {
    for ($k = 0; $k -lt 3; $k++) {
        if ($a.Num[$k] -ne $b.Num[$k]) { return [Math]::Sign($a.Num[$k] - $b.Num[$k]) }
    }
    if ($null -eq $a.Pre -and $null -eq $b.Pre) { return 0 }
    if ($null -eq $a.Pre) { return 1 }
    if ($null -eq $b.Pre) { return -1 }
    $pa = $a.Pre.Split('.'); $pb = $b.Pre.Split('.')
    for ($k = 0; $k -lt [Math]::Min($pa.Count, $pb.Count); $k++) {
        $na = 0; $nb = 0
        $aNum = [int]::TryParse($pa[$k], [ref]$na)
        $bNum = [int]::TryParse($pb[$k], [ref]$nb)
        if ($aNum -and $bNum) { if ($na -ne $nb) { return [Math]::Sign($na - $nb) } }
        elseif ($aNum) { return -1 }
        elseif ($bNum) { return 1 }
        else {
            $c = [string]::Compare($pa[$k], $pb[$k], [StringComparison]::OrdinalIgnoreCase)
            if ($c -ne 0) { return [Math]::Sign($c) }
        }
    }
    return [Math]::Sign($pa.Count - $pb.Count)
}

function Test-VersionSatisfies([string]$version, [string]$range) {
    $range = $range.Trim()
    $v = Parse-NuGetVersion $version
    if ($range -match '^([\[\(])(.*?)([\]\)])$') {
        $loInc = $Matches[1] -eq '['; $hiInc = $Matches[3] -eq ']'
        $parts = $Matches[2].Split(',')
        $lo = $parts[0].Trim()
        $hi = if ($parts.Count -gt 1) { $parts[1].Trim() } else { '' }
        if ($lo) {
            $c = Compare-NuGetVersion $v (Parse-NuGetVersion $lo)
            if ($c -lt 0 -or ($c -eq 0 -and -not $loInc)) { return $false }
        }
        if ($hi) {
            $c = Compare-NuGetVersion $v (Parse-NuGetVersion $hi)
            if ($c -gt 0 -or ($c -eq 0 -and -not $hiInc)) { return $false }
        }
        return $true
    }
    # голое значение = минимальная версия включительно
    return (Compare-NuGetVersion $v (Parse-NuGetVersion $range)) -ge 0
}

# --- Данные фида --------------------------------------------------------------
if (-not (Test-Path $OutDir)) {
    Write-Host "❌ Фид не найден: $OutDir" -ForegroundColor Red
    exit 1
}

$packages = @{}   # id -> @{ Version; Deps = @( @{Id;Range} ) }
foreach ($f in Get-ChildItem "$OutDir\*.nupkg") {
    $z = [IO.Compression.ZipFile]::OpenRead($f.FullName)
    try {
        $e = $z.Entries | Where-Object { $_.Name -like '*.nuspec' } | Select-Object -First 1
        $r = New-Object IO.StreamReader($e.Open())
        [xml]$x = $r.ReadToEnd()
        $r.Close()
    }
    finally { $z.Dispose() }
    $id = $x.package.metadata.id
    $deps = New-Object System.Collections.Generic.List[object]
    foreach ($g in $x.package.metadata.dependencies.group) {
        foreach ($d in $g.dependency) { $deps.Add(@{ Id = $d.id; Range = $d.version }) }
    }
    $packages[$id] = @{ Version = $x.package.metadata.version; Deps = $deps; File = $f }
}

if ($packages.Count -eq 0) {
    Write-Host "❌ В фиде нет nupkg: $OutDir" -ForegroundColor Red
    exit 1
}

# --- Данные репо: проекты и их PackageId --------------------------------------
$projects = @{}   # BaseName -> @{ HasPackageId; Path; LastWrite }
$byPackageId = @{}
Get-ChildItem (Join-Path $root "src") -Recurse -Filter *.csproj | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    $pkgId = if ($text -match '<PackageId>([^<]+)</PackageId>') { $Matches[1].Trim() } else { $null }
    $projects[$_.BaseName] = @{ HasPackageId = [bool]$pkgId; Path = $_.FullName; LastWrite = $_.LastWriteTime; PackageId = $pkgId }
    if ($pkgId) { $byPackageId[$pkgId] = $_ }
}
$propsTime = (Get-Item (Join-Path $root "Directory.Build.props")).LastWriteTime
foreach ($p in @("Directory.Build.targets", "Directory.Packages.props")) {
    $f = Join-Path $root $p
    if (Test-Path $f) { $propsTime = [Math]::Max($propsTime.Ticks, (Get-Item $f).LastWriteTime.Ticks) }
}
$propsTime = [DateTime]::new($propsTime)

Write-Host
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🔍 Валидация NuGet-фида" -ForegroundColor Green
Write-Host "📂 $OutDir   |   пакетов: $($packages.Count)" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

function Write-CheckResult([string]$title, [int]$bad) {
    if ($bad -eq 0) { Write-Host "✅ $title" -ForegroundColor Green }
    else { Write-Host "❌ $title — проблем: $bad" -ForegroundColor Red }
}

# --- 1. csproj: пакуемые → непакуемые ссылки ----------------------------------
$bad1 = 0
foreach ($name in ($projects.Keys | Sort-Object)) {
    $p = $projects[$name]
    if (-not $p.HasPackageId) { continue }
    $text = Get-Content $p.Path -Raw
    foreach ($m in [regex]::Matches($text, 'ProjectReference Include="([^"]+)"')) {
        $ref = [IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)
        if ($projects.ContainsKey($ref) -and -not $projects[$ref].HasPackageId) {
            $errors.Add("csproj: $name (пакуемый) ссылается на $ref без PackageId → pack напишет голую зависимость '$ref'")
            $bad1++
        }
    }
}
Write-CheckResult "1. csproj: пакуемые проекты не ссылаются на непакуемые" $bad1

# --- 2. Голые зависимости в nuspec ---------------------------------------------
$bad2 = 0
foreach ($id in ($packages.Keys | Sort-Object)) {
    foreach ($d in $packages[$id].Deps) {
        $isBare = $d.Id -match '^Mars\.' -or ($projects.ContainsKey($d.Id) -and -not $projects[$d.Id].HasPackageId)
        if ($isBare) {
            $src = if ($projects.ContainsKey($d.Id)) { " (проект без PackageId: $($projects[$d.Id].Path))" } else { "" }
            $errors.Add("nuspec: $id -> $($d.Id) — голое имя, такого пакета нет в фидах$src")
            $bad2++
        }
    }
}
Write-CheckResult "2. nuspec: нет голых зависимостей Mars.*" $bad2

# --- 3. Целостность mdimai666.* зависимостей -----------------------------------
$bad3 = 0
foreach ($id in ($packages.Keys | Sort-Object)) {
    foreach ($d in $packages[$id].Deps) {
        if ($d.Id -notlike 'mdimai666.*') { continue }
        if (-not $packages.ContainsKey($d.Id)) {
            $errors.Add("feed: $id требует $($d.Id) ($($d.Range)) — пакета нет в фиде")
            $bad3++
        }
        elseif (-not (Test-VersionSatisfies $packages[$d.Id].Version $d.Range)) {
            $errors.Add("feed: $id требует $($d.Id) ($($d.Range)) — в фиде только $($packages[$d.Id].Version)")
            $bad3++
        }
    }
}
Write-CheckResult "3. Все mdimai666.* зависимости есть в фиде в нужных версиях" $bad3

# --- 4. Замыкания kit-наборов ---------------------------------------------------
$kitRoots = @('mdimai666.Mars.Plugin.Kit.Host', 'mdimai666.Mars.Plugin.Kit.Front', 'mdimai666.Mars.Plugin.Sdk')
$bad4 = 0
$thirdParty = @{}   # "id|range" -> @{Id;Range}
foreach ($kit in $kitRoots) {
    if (-not $packages.ContainsKey($kit)) {
        $errors.Add("kit: $kit отсутствует в фиде")
        $bad4++
        continue
    }
    $visited = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::OrdinalIgnoreCase)
    $queue = New-Object System.Collections.Generic.Queue[string]
    $queue.Enqueue($kit)
    $missing = New-Object System.Collections.Generic.List[string]
    while ($queue.Count -gt 0) {
        $cur = $queue.Dequeue()
        if (-not $visited.Add($cur)) { continue }
        if (-not $packages.ContainsKey($cur)) { $missing.Add($cur); continue }
        foreach ($d in $packages[$cur].Deps) {
            if ($d.Id -like 'mdimai666.*' -or $d.Id -match '^Mars\.') { $queue.Enqueue($d.Id) }
            else { $thirdParty["$($d.Id)|$($d.Range)"] = $d }
        }
    }
    if ($missing.Count -gt 0) {
        foreach ($m in ($missing | Sort-Object -Unique)) { $errors.Add("kit: $kit не устанавливается — нет пакета '$m'") }
        $bad4 += $missing.Count
        Write-Host "   ❌ $kit — не хватает: $(($missing | Sort-Object -Unique) -join ', ')" -ForegroundColor Red
    }
    else {
        Write-Host "   ✅ $kit — замыкание $($visited.Count) пакетов резолвится" -ForegroundColor Green
    }
}
Write-CheckResult "4. Kit-наборы устанавливаемы из фида" $bad4

if ($Online) {
    $badOnline = 0
    $nugetCache = @{}
    foreach ($key in ($thirdParty.Keys | Sort-Object)) {
        $d = $thirdParty[$key]
        $lid = $d.Id.ToLowerInvariant()
        try {
            if (-not $nugetCache.ContainsKey($lid)) {
                $index = Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$lid/index.json" -TimeoutSec 30
                $nugetCache[$lid] = @($index.versions)
            }
            $ok = $false
            foreach ($v in $nugetCache[$lid]) { if (Test-VersionSatisfies $v $d.Range) { $ok = $true; break } }
            if (-not $ok) {
                $errors.Add("nuget.org: $($d.Id) ($($d.Range)) — подходящей версии нет")
                $badOnline++
            }
        }
        catch {
            $warnings.Add("nuget.org: не удалось проверить $($d.Id): $($_.Exception.Message)")
        }
    }
    Write-CheckResult "4-online. Сторонние зависимости kit-замыканий есть на nuget.org ($($thirdParty.Count) шт.)" $badOnline
}

# --- 5. Свежесть nupkg (warning) ------------------------------------------------
$bad5 = 0
foreach ($id in ($packages.Keys | Sort-Object)) {
    if (-not $byPackageId.ContainsKey($id)) { continue }
    $csproj = $byPackageId[$id]
    $newest = [Math]::Max($csproj.LastWriteTime.Ticks, $propsTime.Ticks)
    if ($packages[$id].File.LastWriteTime.Ticks -lt $newest) {
        $warnings.Add("stale: $($packages[$id].File.Name) старше csproj/props — dotnet pack мог пропустить перепаковку (удалите nupkg и пакуйте заново)")
        $bad5++
    }
}
if ($bad5 -eq 0) { Write-Host "✅ 5. Пакеты не старше исходников" -ForegroundColor Green }
else { Write-Host "⚠️  5. Подозрительно старых пакетов: $bad5 (warning)" -ForegroundColor Yellow }

# --- Итог ----------------------------------------------------------------------
Write-Host
if ($warnings.Count -gt 0) {
    Write-Host "⚠️  Предупреждения:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    Write-Host
}
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
if ($errors.Count -eq 0) {
    Write-Host "✅ ФИД ВАЛИДЕН — kit-наборы устанавливаемы" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    exit 0
}
else {
    Write-Host "❌ ПРОБЛЕМЫ ($($errors.Count)):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    exit 1
}