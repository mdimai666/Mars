$propsPath = "Directory.Packages.props"

# Парсим XML, чтобы получить значение MarsAppVersion
[xml]$xml = Get-Content $propsPath

# Извлекаем MarsAppVersion
$version = $xml.Project.PropertyGroup.MarsAppVersion
if (-not $version) {
    Write-Error "Не найден MarsAppVersion в $propsPath"
    exit 1
}

Write-Host "Версия из Directory.Packages.props: $version"

# Имя Docker-образа
$imageName = "mdimai666/mars"

# Собираем образ с тегом версии
Write-Host "Собираем Docker образ с тегом $version..."
docker build -t "${imageName}:${version}" .

if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при сборке Docker образа"
    exit 1
}

# Тег latest
Write-Host "Тегируем образ как latest..."
docker tag "${imageName}:${version}" "$imageName:latest"

if (0) {
    # Публикуем образ с тегом версии
    Write-Host "Публикуем образ с тегом $version..."
    docker push "${imageName}:${version}"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ошибка при публикации Docker образа с тегом $version"
        exit 1
    }
}

# Публикуем образ с тегом latest
Write-Host "Публикуем образ с тегом latest..."
docker push "${imageName}:latest"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при публикации Docker образа с тегом latest"
    exit 1
}

Write-Host "Публикация завершена успешно!"
