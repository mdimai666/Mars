# План: каталог плагинов Mars — отдельный сервер (пункт 1.1)

Исходные материалы: [PluginDistributionPlan.md](./PluginDistributionPlan.md) (исследование и общая
стратегия), [PlanPluginServerPrompt.md](./PlanPluginServerPrompt.md) (брейншторм).
Статус: **план согласовывается, не начат**.

Суть пункта 1.1: бинарники плагинов живут на nuget.org (`packageType=MarsPlugin`), а «список
доступных» — свой лёгкий сервер-каталог, который несёт метаданные, рейтинги/отзывы, статусы
(recommended/banned) и модерацию. Админка Mars ходит в каталог, ставит плагины с nuget.org.

## Принятые решения

1. **Сервер — НЕ Mars.** Mars для каталога слишком тяжёлый; пишем отдельный лёгкий сервис с нуля.
2. **Отдельный репозиторий** `mdimai666/Mars.PluginCatalog` (независимые релизы/деплой).
3. **Аккаунты — без Keycloak (пока).** Прямые OAuth-провайдеры Google/GitHub через middleware
   ASP.NET Core; идентичность — строка `provider:subject` (например `github:12345`) + снапшот
   имени, таблицы пользователей нет. Каталог выдаёт свой stateless JWT. Авторизация нужна для
   отзывов и для разработчиков; чтение витрины — анонимное. Когда вместе с Mars Cloud появится
   Keycloak — каталог перейдёт на его OIDC-токены сменой конфига (валидация проектируется
   конфигурируемой), модель данных не меняется.
4. **Сначала API-only.** Публичный веб-сайт каталога — поздняя фаза, потребитель v1 — админка Mars.
5. **Подача плагинов — заявка + модерация** (модель WordPress/Obsidian). *Вопрос остался без
   явного ответа — принято по умолчанию; меняется легко, влияет только на Фазу 3.*
6. **Стек каталога**: .NET 10, ASP.NET Core Minimal API, EF Core, PostgreSQL (SQLite для локальной
   разработки), Docker. Ничего от Mars не наследуем.
7. **Каталог не хранит бинарники** и не знает NuGet.Protocol: метаданные пакетов кешируются с
   nuget.org через обычный HTTP (search API). Скачивание и резолв зависимостей — на стороне Mars.

## Роли и права

Вход — OAuth через Google/GitHub (стандартные middleware ASP.NET Core, client id/secret в
конфиге; новый провайдер = новый handler). После входа каталог выдаёт свой JWT (stateless,
без сессий и куки). Пользователь в модели не хранится — только `UserKey` вида `provider:subject`
в отзывах/жалобах/заявках.

| Кто | Права |
|---|---|
| любой вошедший | отзывы (1 на плагин), жалобы |
| любой вошедший («разработчик» — не роль) | подача плагинов через модерацию, правка своих записей |
| модераторы | approve/reject, recommend, ban, модерация отзывов и жалоб |

Модераторы — allow-list `UserKey` в appsettings (команда маленькая, UI управления ролями не
нужен). При переходе на Keycloak вместе с Mars Cloud allow-list заменится клиентскими ролями.

## Модель данных

- **Plugin**: `PackageId` (уникальный, = NuGet id, lowercase), DisplayName, Summary, Description,
  AuthorName, RepositoryUrl, HomepageUrl, LicenseUrl, IconUrl, Tags[],
  `Status` (Pending | Approved | Recommended | Banned), MarsVersionMin/Max (совместимость),
  LatestVersion, TotalDownloads, AvgRating, ReviewsCount (кэш из nuget.org + своих отзывов),
  SubmittedById/Name (Keycloak sub), IsDiscovered (нашёл discovery-джоб, не подача),
  BannedReason?, CreatedAt/UpdatedAt.
- **Review**: PluginId, UserSub, UserName, Rating (1–5), Text, Hidden (модерация),
  CreatedAt/UpdatedAt; уникальность (PluginId, UserSub) — апсерт.
- **Report**: PluginId, ReporterSub/Name, Reason (malicious | spam | broken | other), Details,
  Status (Open | Resolved), Resolution?, ResolvedBy/At.

## API v1 (`/api`)

Анонимное чтение:
- `GET /plugins?q=&tag=&recommended=&minVersion=&sort=downloads|rating|newest&page=&take=` —
  витрина (только Approved/Recommended; бан не отдаётся никогда).
- `GET /plugins/{packageId}` — карточка + агрегированный рейтинг.
- `GET /plugins/{packageId}/reviews?page=&take=` — отзывы (без скрытых).

Пользователь (любой вошедший):
- `POST /plugins/{packageId}/reviews` `{ rating, text }` — апсерт своего отзыва;
- `DELETE /plugins/{packageId}/reviews/mine`;
- `POST /plugins/{packageId}/reports` `{ reason, details }`.

Разработчик (`catalog-developer`):
- `POST /plugins/submit` `{ nugetPackageId, comment? }` — проверяем, что пакет существует на
  nuget.org и имеет `packageType=MarsPlugin`; создаёт Plugin(Pending). Владение пакетом на
  nuget.org достоверно не проверяется — это работа модератора при approve.
- `PUT /plugins/{packageId}` — правка метаданных только своих плагинов
  (описание, теги, MarsVersionMin/Max, ссылки).

Модератор (`catalog-moderator`):
- `GET /moderation/plugins?status=pending|approved|banned|discovered`;
- `POST /moderation/plugins/{packageId}/approve|reject|recommend|unrecommend|ban|unban` (ban с
  причиной);
- `GET /moderation/reports?status=open`, `POST /moderation/reports/{id}/resolve` `{ action }`;
- `POST /moderation/reviews/{id}/hide|show`;
- `POST /plugins/{packageId}/sync` — принудительное обновление метаданных с nuget.org.

Служебное: `GET /healthz`.

Фоновые джобы (`IHostedService`, таймер):
- **NugetMetadataSyncJob** — раз в ~6 часов по Approved/Recommended: свежая версия,
  totalDownloads, иконка/описание (search API: `q=packageid:{id}` + `packageType`).
- **DiscoveryJob** (Фаза 3) — поиск nuget.org по `packageType=MarsPlugin`; неизвестные пакеты —
  в очередь модерации с флагом IsDiscovered.

## Структура репозитория

```
Mars.PluginCatalog/
├─ src/Mars.PluginCatalog/        — один проект-хост (папки: Endpoints/, Domain/, Data/, Services/, Nuget/)
├─ tests/Mars.PluginCatalog.Tests/ — xUnit + WebApplicationFactory (+SQLite)
├─ docker-compose.yml             — api + postgres
├─ Dockerfile, .github/workflows/ — CI: build+test, на релиз — образ mdimai666/mars-plugincatalog
```

Один проект намеренно (сервис маленький); на Core/Data разделим, если вырастет.

## Точка старта на стороне Mars (as-is)

- ZIP-загрузка работает (`PluginController.UploadPlugin` → `PluginZipInstaller`), но
  `PluginZipInstaller.InstallPlugin` — заглушка «not implemented».
- Метаданные плагинов уже читаются из атрибутов сборки (`PackageId`, `PackageTags`,
  `RepositoryUrl`, `PackageIcon` — `PluginManager.cs`).
- `src/Plugin/Mars.Plugin/NuspecHelper.cs` — неиспользуемый задел по nuspec.
- Плагины грузятся только при старте (`Assembly.LoadFrom`) → установка = «скачать и попросить
  рестарт» (горячая догрузка — отдельная большая тема, вне этого плана).
- Клиентские сервисы админки — паттерн `Mars.WebApiClient` (`PluginServiceClient` как образец).

## Фазы

Каждая фаза — самостоятельный коммит-набор; каталог после каждой фазы собирается и работает.
Фазы 0–3 — репозиторий Mars.PluginCatalog; фазы 4–5 — репозиторий Mars.

### Фаза 0. Bootstrap репозитория и конвенции пакетов

**Цель**: новый сервис поднимается локально одной командой; конвенция `packageType=MarsPlugin`
зафиксирована.

- Создать репо `Mars.PluginCatalog`: проект Minimal API (.NET 10), EF Core + PostgreSQL провайдер
  (+SQLite для dev-профиля), healthz, Dockerfile, docker-compose (api+postgres), CI workflow
  (build+test) по образцу `.github/workflows` Mars.
- Конвенция на стороне Mars (маленький коммит в Mars): `PluginPublishScript` добавляет
  `<packageType>MarsPlugin</packageType>` в генерируемый nuspec; обновить
  `ai/PluginCreationGuide.md` и документацию плагинов.

**Готово когда**: `docker compose up` поднимает api+postgres; `/healthz` отвечает; CI зелёный;
тестовый пакет, опубликованный скриптом, имеет `packageType=MarsPlugin`.

### Фаза 1. Доменная модель и read-API + синк с nuget.org

**Цель**: витрина работает анонимно, данные о пакетах свежие.

- Сущности Plugin/Review/Report, DbContext, миграции.
- Рид-эндпоинты: `GET /plugins` (поиск/фильтры/сортировка/пагинация), `GET /plugins/{packageId}`,
  `GET /plugins/{packageId}/reviews` (пока пустые).
- `NugetSyncService` (HTTP-клиент search API nuget.org) + NugetMetadataSyncJob;
  `POST /plugins/{packageId}/sync` (пока без авторизации).
- Тесты: WebApplicationFactory + SQLite (витрина, пагинация, sync на mock-HTTP).

**Готово когда**: после seed/добавления плагина вручную витрина отдаёт карточку со свежими
версией/загрузками с nuget.org.

### Фаза 2. Аутентификация: отзывы и жалобы

**Цель**: пользовательские действия под своим JWT после входа через Google/GitHub.

- OAuth-handler'ы Google/GitHub + выпуск своего JWT (подпись из конфига); валидация JWT
  проектируется конфигурируемой — позже сюда же подключится Keycloak-валидация. В тестах —
  подмена входа/токенов.
- Отзывы: апсерт своего отзыва (rating 1–5 + текст, лимиты), удаление своего; пересчёт
  AvgRating/ReviewsCount.
- Жалобы: создание, лимит «1 открытая жалоба на плагин от пользователя».
- Rate-limiting на запись (встроенный `Microsoft.AspNetCore.RateLimiting`).
- Тесты на авторизацию/роли/апсерт.

**Готово когда**: вошедший через Google/GitHub пользователь оставляет и правит отзыв; аноним
получает 401.

### Фаза 3. Подача плагинов и модерация

**Цель**: полный цикл «заявка → approve → витрина → recommend/ban».

- `POST /plugins/submit` (проверка существования пакета и `packageType` на nuget.org),
  `PUT /plugins/{packageId}` для своих.
- Модераторские эндпоинты: очередь pending, approve/reject, recommend/unrecommend, ban/unban
  (ban скрывает с витрины и отдаётся клиенту Mars как «запрещён»).
- Очередь жалоб: список, resolve (с опцией сразу забанить).
- Скрытие/показ отзывов модератором.
- DiscoveryJob: поиск `packageType=MarsPlugin` на nuget.org, новые — в очередь с IsDiscovered.
- Тесты жизненного цикла статусов и прав ролей.

**Готово когда**: разработчик подаёт пакет → модератор одобряет → плагин в витрине; ban убирает
его из выдачи; discovery находит тестовый пакет.

### Фаза 4. Mars: установка плагинов с nuget.org

**Цель**: Mars ставит плагин по NuGet-id с корректным резолвом зависимостей.

- Опция `PluginCatalogOption` (`CatalogUrl`, `Enabled`) — регистрация по образцу
  `MainOptions.cs`, форма в настройках админки.
- Установщик `NuGetPluginInstaller` (Mars.Plugin, NuGet.Protocol): скачать пакет + зависимости
  из nuget.org во временную область → разложить в `data/plugins/<id>/` (формат как у ZIP:
  `<имя>.dll` + `<имя>.runtimeconfig.json` рядом), закрыть заглушку
  `PluginZipInstaller.InstallPlugin` общей логикой раскладки.
- Проверка статуса в каталоге перед установкой (banned → запрет с причиной); если каталог
  выключен — установка по id всё равно работает (только nuget.org).
- REST `PluginController.InstallFromCatalog(packageId)` (+ клиент в `Mars.WebApiClient`),
  роль Admin/Developer; ответ «установлено, требуется рестарт» (рестарт — существующим
  механизмом, если есть, иначе сообщение админу).
- Тесты: раскладка скачанного nupkg в `data/plugins`, запрет banned.

**Готово когда**: по NuGet-id плагин ставится в `data/plugins` со своими зависимостями и
подхватывается после рестарта; banned-пакет не ставится.

### Фаза 5. Mars: витрина маркетплейса в админке

**Цель**: страница «Маркетплейс»: поиск, карточки, рейтинги, установка в один клик.

- Страница `src/AppAdmin/Pages/...` (маршрут `/marketplace`, пункт в сайдбаре): список плагинов
  из каталога (поиск, фильтр recommended, сортировка), карточка плагина (описание, скриншоты
  позже, рейтинг, отзывы, версия, совместимость с текущей версией Mars), кнопка «Установить»
  (Фаза 4), статусы установленных/доступных обновлений (сравнение установленных версий с
  LatestVersion каталога).
- Режим без каталога (`Enabled=false`) — страница не показывается.
- Авторизация для «написать отзыв»: OAuth-приложения зарегистрированы на URL каталога
  (Mars-инстансы self-hosted, их callback'и у провайдеров не зарегистрировать), поэтому логин
  живёт на стороне каталога: админка открывает попап `<каталог>/auth/login`, после входа токен
  возвращается в админку (postMessage из попапа либо одноразовый код → обмен на токен). Без входа
  отзывы только читаются. Если флоу окажется громоздким — написание отзывов переносим в отдельную
  фазу, чтение остаётся.

**Готово когда**: в админке видно витрину с реальными данными каталога; плагин ставится в один
клик; вошедший пользователь может оставить отзыв.

### Фаза 6. Деплой и документация

- Публикация образа `mdimai666/mars-plugincatalog` (Docker Hub, по образцу publish-скриптов Mars);
  деплой на инфраструктуру рядом с mars-dotnet.org (compose/traefik — по месту).
- Каталог по умолчанию прописывается в дефолтные настройки Mars (appsettings шаблон + setup).
- Документация: страница в `docs/dev_docs/` (конвенции `ai/DocsGuide.md`) — как подать плагин,
  правила модерации, API каталога; обновление `ai/PluginCreationGuide.md`.
- Публичный веб-сайт каталога — отдельная задача после стабилизации API.

## Сквозное и риски

- **Безопасность установки**: плагин .NET — произвольный код без изоляции; бан в каталоге +
  модерация — первая линия защиты. Установка только под ролью Admin/Developer, с подтверждением.
- **Владение пакетом**: nuget.org не даёт достоверно проверить владельца при подаче →
  approve модератором обязателен; в будущем — верификация через GitHub-репо пакета.
- **Совместимость версий**: MarsVersionMin/Max задаёт автор при подаче; витрина Mars фильтрует
  по текущей версии. Формат — простой диапазон (`1.0` / `1.0-2.*`).
- **Автономность Mars**: каталог недоступен/выключен — Mars продолжает ставить ZIP вручную и (с
  Фазы 4) по прямому NuGet-id; каталог не становится точкой отказа.
- **Отзывы**: 1 аккаунт = 1 отзыв на плагин; агрегаты пересчитываются при изменении; скрытые
  модерацией не учитываются.
- После фаз с js/css в админке Mars — не забывать bump версии статики
  (см. конвенцию `MarsAppVersion`).

## Итог

Фаза 0–3 = каталог-сервер (новый репо, ~небольшой сервис: модель + read-API + вход Google/GitHub
со своим JWT + модерация + синк nuget.org). Фаза 4–5 = потребитель в Mars (установщик
NuGet.Protocol + витрина).
Фаза 6 = деплой/доки. Рецепты и скиллы — следующий план, переиспользуют этот же сервис как
отдельный тип сущности.
