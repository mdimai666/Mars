param(
    [switch]$Latest = $true, # по умолчанию сборка помечается тегом latest; чтобы отключить: -Latest:$false
    [switch]$Yes             # пропустить подтверждение публикации (для CI)
)
$ErrorActionPreference = "Stop"

# Пути от корня репозитория (рядом со скриптом), а не от текущей директории
$root = $PSScriptRoot
$propsPath = Join-Path $root "Directory.Build.props"

# Парсим XML, чтобы получить значение MarsAppVersion. PropertyGroup в файле
# несколько — берём первую группу, где свойство реально задано (member
# enumeration по массиву групп вернула бы массив со значениями-пустышками).
[xml]$xml = Get-Content $propsPath

# Извлекаем MarsAppVersion
$version = [string]($xml.Project.PropertyGroup | Where-Object { $_.MarsAppVersion } | Select-Object -First 1).MarsAppVersion
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

# Проверка входа в Docker Hub. При credsStore/credHelpers docker login не пишет
# запись в auths, и docker info отдаёт пустой UserName — поэтому дополнительно
# проверяем хранилище учёток через docker-credential-<helper>.
function Test-DockerHubLogin {
    $userName = docker info --format '{{.UserName}}' 2>$null
    if ($LASTEXITCODE -eq 0 -and $userName) {
        return $true
    }

    $configPath = Join-Path $env:USERPROFILE '.docker\config.json'
    if (-not (Test-Path $configPath)) {
        return $false
    }

    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    $helper = $config.credsStore
    if (-not $helper -and $config.credHelpers) {
        $prop = $config.credHelpers.PSObject.Properties['https://index.docker.io/v1/']
        if ($prop) {
            $helper = $prop.Value
        }
    }
    if (-not $helper) {
        return $false
    }

    $list = & "docker-credential-$helper" list 2>$null
    return $LASTEXITCODE -eq 0 -and $list -match 'index\.docker\.io/v1/'
}

if (-not (Test-DockerHubLogin)) {
    Write-Error "Нет входа в Docker Hub - выполните docker login"
    exit 1
}

Write-Host "Версия из Directory.Build.props: $version"
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
