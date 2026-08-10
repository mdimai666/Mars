param(
    [switch]$Latest = $true # по умолчанию сборка помечается тегом latest; чтобы отключить: -Latest:$false
)
$ErrorActionPreference = "Stop"

# Пути от корня репозитория (рядом со скриптом), а не от текущей директории
$root = $PSScriptRoot
$propsPath = Join-Path $root "Directory.Packages.props"

# Парсим XML, чтобы получить значение MarsAppVersion
[xml]$xml = Get-Content $propsPath

# Извлекаем MarsAppVersion
$version = $xml.Project.PropertyGroup.MarsAppVersion
if (-not $version) {
    Write-Error "Не найден MarsAppVersion в $propsPath"
    exit 1
}

$GIT_SHA = git rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or -not $GIT_SHA) {
    Write-Error "Не удалось получить git-коммит (запускайте из репозитория)"
    exit 1
}
if (git status --porcelain) {
    Write-Warning "Рабочее дерево не чистое - содержимое сборки может не совпадать с коммитом $GIT_SHA"
}

Write-Host "Версия из Directory.Packages.props: $version"
Write-Host "GIT_SHA: $GIT_SHA"

# Имя Docker-образа
$imageName = "mdimai666/mars"
$tags = @("${imageName}:${version}", "${imageName}:${GIT_SHA}")
if ($Latest) { $tags += "${imageName}:latest" }

# Собираем образ с тегами
Write-Host "Собираем Docker образ с тегами: $($tags -join ', ')"
$tagArgs = foreach ($t in $tags) { @('-t', $t) }
docker build --build-arg GIT_SHA=$GIT_SHA `
             --build-arg BUILD_VERSION=$version `
             @tagArgs `
             $root
if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при сборке Docker образа"
    exit 1
}

Write-Host "Сборка завершена успешно!"
