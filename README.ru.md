<p align="center">
  <a href="https://Mars-dotnet.org/#gh-light-mode-only">
    <img src="assets/mars-logo.svg" width="318px" alt="Mars logo" />
  </a>
</p>

<h3 align="center">Открытая платформа визуального программирования. Создавайте сайты, автоматизируйте задачи, подключайте что угодно — без кода.</h3>

<p align="center">Self-hosted или облако. Всё под вашим контролем.</p>

<p align="center"><a href="https://cloud.Mars-dotnet.org/signups?source=github1">Облако</a> · <a href="https://Mars-dotnet.org/demo">Демо</a></p>

<p align="center">
  <span>Русский</span> · <a href="README.md">English</a>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/mdimai666.Mars.Core">
    <img src="https://img.shields.io/nuget/v/mdimai666.Mars.Core" alt="NuGet Version" />
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License" />
  </a>
  <a href="https://dotnet.microsoft.com/download">
    <img src="https://img.shields.io/badge/.NET-10.0-blue.svg" alt=".NET Version" />
  </a>
</p>

<br>

<p align="center">
  <a href="https://Mars-dotnet.org">
    <img src="assets/Mars_gif.gif" alt="Mars Platform" />
  </a>
</p>

<br>

## Что такое Mars?

Mars — это платформа, объединяющая **визуальное программирование** и **гибкий движок контента**. Соединяйте ноды на холсте, чтобы создавать сайты, API, автоматизации, IoT-сценарии и данные-пайплайны — без написания кода. Когда нужна полная мощность — переключайтесь на C# прямо внутри визуального редактора.

Вдохновлён Node-RED и WordPress, Mars даёт простоту flow-based программирования с глубиной полноценной платформы.

## Ключевые возможности

### Визуальное программирование
- **55+ типов нод** — HTTP, MQTT, SQL, файлы, C# код, шаблоны, циклы, события, email и другое
- **Flow-редактор** — перетаскивайте, соединяйте, отлаживайте. Видьте свою логику как диаграмму
- **No-code и low-code** — визуальные блоки для типовых задач, C# когда нужен полный контроль

### Управление контентом
- **Кастомные типы записей** — создавайте любые сущности (статьи, товары, заказы, произвольные данные)
- **15 типов полей** — текст, числа, даты, селекты, связи, файлы, изображения, вложенные группы, списки
- **Мультиязычный контент** с категориями и таксономиями

### Источники данных
- Подключение к **PostgreSQL, MsSQL, MySQL** базам данных
- Визуальные запросы к удалённым базам через SQL-ноды
- Интроспекция схем, резервное копирование, исследование данных

### Плагины
- Расширение Mars через **.NET сборки**, загружаемые в runtime
- Бэкенд и фронтенд (Blazor WebAssembly) плагины
- Загрузка через админ-панель или установка из маркетплейса

### Docker и автоматизация
- Управление **Docker контейнерами**, образами и volumes из админ-панели
- Встроенный **планировщик задач** (Quartz.NET) — cron, интервалы, ежедневные задачи
- Запуск внешних инструментов и сервисов как части ваших сценариев

### AI интеграция
- **Semantic Kernel** для LLM-функций
- AI-инструменты доступны внутри визуальных сценариев
- Интроспекция схем баз данных с помощью AI

### Мультифронт
- Публикация контента как **SPA, статический HTML, Blazor или шаблоны**
- Используйте любой фронтенд-фреймворк (React, Vue, Angular) через API
- Мобильные приложения и IoT-устройства подключаются напрямую

### Админ-панель
- **Blazor Wasm** одностраничное приложение
- Управление всем: контент, пользователи, медиа, плагины, навигация, настройки
- Визуальный редактор нод интегрирован в админ-интерфейс

### Наблюдаемость
- **OpenTelemetry** с Prometheus endpoint
- Структурированное логирование и трейсинг

## Сценарии использования

| Сценарий | Описание |
|----------|----------|
| **Сайты** | CMS с визуальной логикой — создавайте страницы, управляйте контентом, настраивайте поведение |
| **API** | Визуально создавайте REST-эндпоинты, подключайтесь к базам, трансформируйте данные |
| **Автоматизация** | Задачи по расписанию, обработка данных, работа с файлами, email-уведомления |
| **IoT** | MQTT-ноды для общения с устройствами, сценарии умного дома |
| **Данные-пайплайны** | Подключение баз данных, ETL-процессы, экспорт в любой формат |
| **Внутренние инструменты** | Админ-панели, дашборды, сценарии согласования |

## Быстрый старт

### Docker

```bash
docker run -d --name mars-app \
  -w /app -p 5005:80 \
  -e "ConnectionStrings__DefaultConnection=Host=host.docker.internal:5432;Database=mars_app;Username=postgres;Password=your_password" \
  mdimai666/mars:latest
```

Или используйте **docker-compose** — см. [docker-compose.yml](https://mdimai666.github.io/Mars/files/docker/docker-compose.yml) и [appsettings.Production.json](https://mdimai666.github.io/Mars/files/docker/appsettings.Production.json).

### Разработка

Требования: [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download) или [Visual Studio 2022](https://visualstudio.microsoft.com/ru/vs/community/)

```bash
git clone https://github.com/mdimai666/Mars.git
cd Mars
cp appsettings.json appsettings.Local.json
# Отредактируйте appsettings.Local.json — укажите подключение к базе
dotnet watch run --project src/Mars.WebApp
```

### Скачать

Скачайте последний релиз с [GitHub Releases](https://github.com/mdimai666/Mars/releases) и запустите `Mars.exe`.

## Документация

- [Документация разработчика](https://mdimai666.github.io/Mars/)
- [Руководство по быстрому старту](https://mdimai666.github.io/Mars/md/QuickStart.md)
- [Разработка плагинов](https://github.com/mdimai666/MyMarsPlugin)

## Развёртывание

- **ОС**: Windows, Linux, macOS
- **База данных**: PostgreSQL (рекомендуется), MsSQL, MySQL, SQLite (для разработки)
- **Облако**: AWS, Azure, Google Cloud, DigitalOcean
- **Docker Hub**: [mdimai666/mars](https://hub.docker.com/r/mdimai666/mars/)

## Сообщество и поддержка

- [GitHub](https://github.com/mdimai666/Mars) — Баг-репорты, вклад в проект
- [Сайт](https://Mars-dotnet.org) — Обзор и новости
- [Документация](https://mdimai666.github.io/Mars/) — Руководства и справочник API

## Стек технологий

- **.NET 10** / ASP.NET Core
- **Blazor** (Server + WebAssembly)
- **Entity Framework Core** — PostgreSQL, MsSQL, MySQL
- **Quartz.NET** — Планировщик задач
- **Semantic Kernel** — AI интеграция
- **Docker.DotNet** — Управление контейнерами
- **OpenTelemetry** — Наблюдаемость

## Лицензия

MIT License — см. [LICENSE](./LICENSE)
