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

docker build    --build-arg GIT_SHA=$GIT_SHA `
                --build-arg BUILD_VERSION=$version `
                -t mdimai666/mars .
