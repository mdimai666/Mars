$propsPath = "Directory.Packages.props"

# Парсим XML, чтобы получить значение MarsAppVersion
[xml]$xml = Get-Content $propsPath

# Извлекаем MarsAppVersion
$version = $xml.Project.PropertyGroup.MarsAppVersion
if (-not $version) {
    Write-Error "Не найден MarsAppVersion в $propsPath"
    exit 1
}
$GIT_SHA = $(git rev-parse HEAD)

Write-Host "Версия из Directory.Packages.props: $version"
Write-Host "GIT_SHA: $GIT_SHA"

# Имя Docker-образа
$imageName = "mdimai666/mars"

# Собираем образ с тегом версии
Write-Host "Собираем Docker образ с тегом $version..."
docker build --build-arg GIT_SHA=$GIT_SHA `
             --build-arg BUILD_VERSION=$version `
             -t "${imageName}:${version}" `
             -t "${imageName}:${GIT_SHA}" `
             -t "${imageName}:latest" .
