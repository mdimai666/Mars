<!-- Title: Быстрый старт -->
<!-- Order: 1 -->


# Быстрый старт

## Docker run

Запуск без сохранения данных
```
docker run -d --name mars-app-nocontent -w /app -p 5005:80  -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal:5432;Database=mars_docker_app2;Username=postgres;Password=ggxxrr" mdimai666/mars:latest
```

Полноценный запуск
```
docker run -d --name mars1 -w /app -p 5005:80 -e ASPNETCORE_CONTENTROOT=/app -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal:5432;Database=mars;Username=postgres;Password=postgres" -v "$(pwd)/data:/app/data" -v "$(pwd)/upload:/app/wwwroot/upload" -v "$(pwd)/data-protection-keys:/root/.aspnet/DataProtection-Keys" mdimai666/mars:latest
```

## Docker composer

Для более удобного запуска

файл [docker-composer.yml](https://mdimai666.github.io/Mars/files/docker/docker-compose.yml) и конфиг файл [appsettings.Production.json](https://mdimai666.github.io/Mars/files/docker/appsettings.Production.json)


Описание создаваемых файлов можно посмотреть [здесь](md/Structure/MarsFilesStructure.md)