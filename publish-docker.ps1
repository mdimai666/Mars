param(
    [switch]$Latest = $true, # по умолчанию сборка помечается тегом latest; чтобы отключить: -Latest:$false
    [switch]$Yes             # пропустить подтверждение публикации (для CI)
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

$userName = docker info --format '{{.UserName}}'
if ($LASTEXITCODE -ne 0 -or -not $userName) {
    Write-Error "Нет входа в Docker Hub - выполните docker login"
    exit 1
}

Write-Host "Версия из Directory.Packages.props: $version"
Write-Host "GIT_SHA: $GIT_SHA"

# Имя Docker-образа
$imageName = "mdimai666/mars"
$tags = @("${imageName}:${version}", "${imageName}:${GIT_SHA}")
if ($Latest) { $tags += "${imageName}:latest" }

Write-Host "Теги: $($tags -join ', ')"
if (-not $Yes) {
    $confirm = Read-Host "Публикуем в публичный реестр. Продолжить? [y/N]"
    if ($confirm -ne 'y' -and $confirm -ne 'Y') {
        Write-Host "Отменено"
        exit 0
    }
}

# 1. Собираем образ со всеми тегами
Write-Host "Собираем Docker образ..."
$tagArgs = foreach ($t in $tags) { @('-t', $t) }
docker build --build-arg GIT_SHA=$GIT_SHA `
             --build-arg BUILD_VERSION=$version `
             @tagArgs `
             $root
if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при сборке Docker образа"
    exit 1
}

# 2. Публикуем теги по очереди
foreach ($t in $tags) {
    Write-Host "Публикуем $t..."
    docker push $t
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ошибка при публикации тега $t"
        exit 1
    }
}

Write-Host "Публикация всех образов завершена успешно!"
