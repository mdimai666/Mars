<!-- Title: Быстрый старт -->
<!-- Order: 1 -->


# Быстрый старт

## Docker compose (рекомендуемый способ)

Готовый [docker-compose.yml](https://mdimai666.github.io/Mars/files/docker/docker-compose.yml): PostgreSQL + Mars.

```
docker compose up -d
```

- **Первый запуск без конфигурации** — поднимется мастер настройки: откройте
  `http://localhost:5004/setup` и пройдите шаги (БД → сайт → администратор).
  Результат сохранится в `./config/appsettings.Production.json`, дальше приложение стартует сразу.
- **Установка без мастера** — в compose есть закомментированный блок `environment`:
  задайте connection string, администратора и стартовый шаблон фронта и раскомментируйте.
  Если задан connection string, мастер не запускается — установка полностью автоматическая.

Пример готового конфига —
[appsettings.Production.json](https://mdimai666.github.io/Mars/files/docker/appsettings.Production.json)
(его можно примонтировать вместо `./config`, тогда мастер тоже не запускается).

> Пока установка не завершена, веб-интерфейс доступен без авторизации — не выставляйте порт наружу до первой настройки.

## Docker run: пробный запуск

Быстро посмотреть Mars без сохранения данных (конфиг и файлы живут в контейнере):
```
docker run -d --name mars-try -p 5005:80 mdimai666/mars:latest
```
Конфигурации нет, поэтому при первом старте поднимется **мастер настройки** — откройте `http://localhost:5005/setup`.

## Docker run: запуск с мастером настройки

Мастер (как в WordPress): БД → настройки сайта → администратор → готово.
Результат сохраняется в `./config/appsettings.Production.json`, при следующих запусках приложение стартует сразу:
```
docker run -d --name mars -p 5005:80 -v "$(pwd)/config:/app/config" -v "$(pwd)/data:/app/data" -v "$(pwd)/upload:/app/wwwroot/upload" -v "$(pwd)/data-protection-keys:/root/.aspnet/DataProtection-Keys" mdimai666/mars:latest
```

## Автоматическая установка через переменные окружения

Если задан connection string, мастер не запускается — установка происходит автоматически (удобно для CI и автоматизации):
```
docker run -d --name mars -p 5005:80 -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal;Database=mars;Username=mars;Password=mars" -e "Setup__AdminEmail=admin@example.com" -e "Setup__AdminPassword=ChangeMe123!" -e "Setup__AdminFirstName=Admin" -v "$(pwd)/data:/app/data" -v "$(pwd)/upload:/app/wwwroot/upload" -v "$(pwd)/data-protection-keys:/root/.aspnet/DataProtection-Keys" mdimai666/mars:latest
```

| Переменная | Назначение |
|---|---|
| `ConnectionStrings__DefaultConnection` | подключение к PostgreSQL; наличие отключает мастер |
| `Setup__AdminEmail`, `Setup__AdminPassword`, `Setup__AdminFirstName` | первый администратор |
| `Setup__SiteUrl`, `Setup__SiteName`, `Setup__SiteDescription` | настройки сайта |
| `Setup__FrontChoice` | стартовый шаблон фронта: `default`, `landing` |
| `Logging__LogLevel__Default` | уровень логирования |
| `MARS_SETUP_WIZARD=0` | принудительно отключить мастер настройки |

## База данных на хосте (host.docker.internal)

Если Mars работает в контейнере, а PostgreSQL — прямо на хост-машине, в строке подключения
(в переменной окружения или в мастере настройки) вместо `localhost` указывайте
`host.docker.internal` — из контейнера `localhost` это сам контейнер, а не хост:
```
Host=host.docker.internal;Port=5432;Database=mars;Username=mars;Password=<пароль>
```

Нюансы:
- **Docker Desktop (Windows/macOS)** — `host.docker.internal` работает из коробки.
- **Linux** — адрес нужно добавить явно:
  - `docker run`: `--add-host=host.docker.internal:host-gateway`
  - docker compose:
    ```
    services:
      mars-app:
        extra_hosts:
          - "host.docker.internal:host-gateway"
    ```
- PostgreSQL на хосте должен принимать подключения не только с localhost:
  `listen_addresses = '*'` в `postgresql.conf` и правило в `pg_hba.conf`,
  разрешающее подсеть Docker-моста (например `host all all 0.0.0.0/0 scram-sha-256`).

Описание создаваемых файлов можно посмотреть [здесь](md/Structure/MarsFilesStructure.md)
