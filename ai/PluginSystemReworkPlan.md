# План: реворк системы плагинов

> **Статус: Фазы 0, 1, 2 выполнены 2026-09-01; Фаза 2 ждёт пользовательской проверки,
> дальше — Фаза 3 (раскладка, инсталлеры, nuget-установка).**
> Решения: 2026-08-31.
> Задача-источник: [Prompts/PluginSystemReworkPrompt.md](./Prompts/PluginSystemReworkPrompt.md).
> Связанные документы: [PluginServer/PluginDistributionPlan.md](./PluginServer/PluginDistributionPlan.md)
> (стратегия распространения), [PluginServer/PluginCatalogPlan.md](./PluginServer/PluginCatalogPlan.md)
> (каталог-сервер; этот реворк — фундамент его Фаз 4–5), [PluginCreationGuide.md](./PluginCreationGuide.md).

Цели реворка: установка плагинов из NuGet (все плагины в `data/plugins`), починка и
упрощение паковки (`PluginPublishScript`), ревизия именования/структуры `Mars.Plugin.*`,
единая роль `Mars.deps.json`, подготовка к маркетплейсу из `PluginDistributionPlan.md`.

## Принятые решения (2026-08-31)

1. **NuGet-пакет плагина — классический лейаут `lib/` + резолв при установке.**
   Пакет пакуется стандартным `dotnet pack` со всеми зависимостями в nuspec;
   `NuGetPluginInstaller` в Mars резолвит замыкание через NuGet.Protocol и копирует
   в `data/plugins/<id>/` только библиотеки, которых **нет** в замыкании Mars
   (фильтрация по `Mars.deps.json`). Как следствие: для nuget-потока автору плагина
   больше не нужна Release-акробатика `Private=false`/`ExcludeAssets=runtime`.
2. **Инструмент паковки — `Mars.Plugin.Sdk` как nuget с MSBuild-таргетами**
   (подробнее в разделе «Инструмент паковки»). Альтернатива (dotnet tool) описана
   там же — на случай, если таргет-модель не уживётся.
3. **Оба источника загрузки остаются** (секция `Plugins` в appsettings + `data/plugins`).
   Причина: в управляемом облаке понадобится принудительно внедрять плагины конфигурацией,
   без возможности удаления арендатором. Вводится политика источника: плагины из
   конфигурации — `Locked` (админка не даёт выключить/удалить).
4. **Все установленные плагины живут в `data/plugins/<packageId>/`** в единой раскладке,
   независимо от источника (zip / nuget / конфиг указывает на папку или сборку для dev).
5. **Изоляция через AssemblyLoadContext** — отдельная фаза этого плана (моя рекомендация,
   явного ответа не было; фаза автономна и может быть перенесена на отдельный план —
   но до её выполнения сохраняется риск конфликта версий сторонних зависимостей между
   плагинами, см. «Риски»).

## Как сейчас (as-is)

Загрузка:
- Два источника: секция `Plugins` appsettings (`PluginConfig{AssemblyPath, ContentRootPath}`)
  и скан `data/plugins` (детект папки по `<имя>.runtimeconfig.json` + `<имя>.dll`).
  `_имя` / `_папка` пропускаются. Загрузка только при старте, всё через `Assembly.LoadFrom`
  в дефолтном контексте — изоляции нет.
- `PluginManager` — `internal`, создаётся вручную в `ApplicationPluginExtensions.AddPlugins`,
  сам собирает `LoggerFactory` и `FileStorage` мимо DI.
- `app.UsePlugins()`: мапит каждый плагин в `/_plugin/<keyName>` (статика `wwwroot`,
  манифест `_front_plugins.json`, `/health`); `ApplyPluginMigrations()` вызывает
  `IPluginDatabaseMigrator` плагинов.
- Установка: `PluginController.UploadPlugin` → `PluginZipInstaller` — распаковка zip в
  `data/plugins/<имя файла>/` работает, но `InstallPlugin` — заглушка; установки из nuget нет.

Структура (9 проектов в `src/Plugin`, наслоение):
- `Mars.Plugin.PluginHost` — пустой (единственный файл целиком закомментирован).
- Дубль парсерных DTO: `Mars.Plugin/PluginProvider/Dto` и `Mars.Plugin.PluginPublishScript/Dto`
  (`DependiesJsonDto`, `ProjectDependencies`, `StaticwebassetsEndpointsManifestJson`).
- DTO/маппинги/`IPluginService` размазаны между `Mars.Plugin.Abstractions` и `Mars.Plugin.Contracts`.
- Опечатки в именах: `InstatitePlugin`, `DependiesJsonDto`, `Preapare`.
- Мёртвый код: приватные `MarsDeps()`/`MarsEndpoints()` в `PluginManifestProvider`
  (никогда не вызываются), `PluginExampleData` под `#if USE_EXAMPLE_PLUGINS`,
  `isDebug = true` хардкодом в `GenerateManifest`.
- `NuspecHelper` не используется в рантайме, но на него есть тесты — задел для паковки.

`Mars.deps.json` (замыкание сборок Mars.WebApp):
- Закоммичен в `src/Plugin/Mars.Plugin.PluginPublishScript/Mars.deps.json`, копируется из
  bin WebApp только в Debug-сборке, пакуется в nuget скрипта → вечно отстаёт от реальной
  версии Mars.
- «Что принадлежит Mars» задублировано хардкод-списком `MarsNugetsDefinition`
  в `PreparePublishData` — обновляется вручную.
- Используется только скриптом паковки; на рантайме фильтрация фронт-ассетов идёт по
  `Mars.Admin.staticwebassets.endpoints.json` рядом с `Mars.Plugin.dll`
  (`PluginManifestProvider.MarsDevAdminEndpoints`, бросает `FileNotFoundException` при отсутствии).

`Mars.Plugin.PluginPublishScript` (экспериментальный):
- Консольный exe, вызывается из csproj плагина таргетами `AfterTargets="Publish"/"CoreBuild"`
  с хардкодом пути и версии в команде (`$(NuGetPackageRoot)...\0.6.2-alpha.25\...` в README,
  `0.7.4-alpha.10` в шаблоне `MyMarsPlugin`); в `MyMarsPlugin.csproj` таргеты отключены
  (`AfterTargets="111Publish"` / `"111CoreBuild"`). README: «Костыль. Потом переделать»,
  в коде `//TODO: публикация сломалась`.
- Делает: отсечение сборок из замыкания Mars из publish-вывода, чистку
  `wwwroot/_framework` и `_content`, генерацию `_front_plugins.json`, сборку фактического
  содержимого будущего zip.

Фронт (админка, WASM):
- `WebAssemblyPluginFrontExtensions.AddRemotePluginAssemblies`: `/api/Plugin/RuntimePluginManifests`
  → манифест каждого плагина → скачивание перечисленных `.wasm` → запуск
  `IWebAssemblyPluginFront`. Баг: каждая сборка грузится дважды
  (`Assembly.Load(bytes)` и следом `AssemblyLoadContext.Default.LoadFromStream` тех же байтов).
- Есть предохранитель `?safe=1` при ошибке загрузки.

Найденные баги (фиксятся в Фазе 0):
- Таргет `CopyAppAdminStaticWebAssets` в `Mars.WebApp.csproj` копирует при publish файл
  под старым именем `AppAdmin.staticwebassets.endpoints.json`; WASM-проект после
  переименования называется `Mars.Admin` и генерирует `Mars.Admin.staticwebassets.endpoints.json`
  (проверено: `AssemblyName` не переопределён) → в релизной сборке файла нет,
  `PluginManifestProvider` при плагинах со статикой уронит старт.
- Двойная загрузка wasm (выше).

## Целевая архитектура

### Проекты: 9 → 6

| Было | Становится |
|---|---|
| `Mars.Plugin.Abstractions` | API для автора плагина: `MarsPlugin` (бывш. `WebApplicationPlugin`), атрибут, `PluginSettings`, `IPluginDatabaseMigrator` |
| `Mars.Plugin.Contracts` | Все DTO/маппинги/ответы/опции (забирает своё из Abstractions) |
| `Mars.Plugin` | Хост: `PluginManager`, загрузчики, инсталлеры (Zip + NuGet), `PluginManifestProvider` |
| `Mars.Plugin.Front.Abstractions` | Манифестные модели, общие серверу, скрипту паковки и WASM (`MarsFrontPluginManifest` + DTO `deps.json`/endpoints — сюда же, одно место вместо двух копий) |
| `Mars.Plugin.Front` | WASM-загрузчик плагинов админки |
| `Mars.Plugin.Kit.Host` / `Mars.Plugin.Kit.Front` | Остаются — мета-пакеты, точка входа автора плагинов |
| `Mars.Plugin.PluginHost` | Удаляется (пустой) |
| `Mars.Plugin.PluginPublishScript` | Переименовывается в `Mars.Plugin.Sdk` (см. ниже) |

Именование:
- `WebApplicationPlugin` → `MarsPlugin`, `WebApplicationPluginAttribute` → `MarsPluginAttribute`.
  Итог (2026-09-01): старые имена удалены без шимов — своих плагинов мало
  (`MyMarsPlugin`, `TelegramPlugin`, `PlayAudioNodePlugin`), все обновляемы.
- Опечатки: `InstatitePlugin` → `InstantiatePlugin`, `DependiesJsonDto` → `DependenciesJsonDto`.
- `PluginData` → `LoadedPlugin` (внутреннее). `PluginInfo`/`PluginConfig` остаются.

### Один источник манифестов Mars вместо коммиченного `Mars.deps.json`

При сборке/публике Mars генерируются два артефакта:
- `Mars.deps.json` — замыкание сборок WebApp (отсечение общих сборок; фильтрация при
  установке из nuget; «шаренный с хостом» набор для ALC);
- `Mars.Admin.staticwebassets.endpoints.json` — статика админки (фильтрация фронт-ассетов
  плагинов на рантайме — какие библиотеки фронту грузить самому).

Оба пакуются в nuget `mdimai666.Mars.Plugin.Sdk` **той же версии, что и весь релиз Mars**
(`MarsAppVersion` из `Directory.Build.props`). Ничего не коммитится в репо;
хардкод-список `MarsNugetsDefinition` удаляется — источник правды один и всегда в версии релиза.

### Инструмент паковки — `mdimai666.Mars.Plugin.Sdk` (выбранный вариант)

Один консольный инструмент в пакете: `tools/Mars.Plugin.Sdk.dll` + `build/*.targets`.
Автор плагина добавляет:

```xml
<PackageReference Include="mdimai666.Mars.Plugin.Sdk" Version="..." PrivateAssets="all" />
```

Таргеты вызывают инструмент через `$(Pkgmdimai666_Mars_Plugin_Sdk)` — свойство, которое
NuGet сам генерирует для прямого PackageReference: путь и версия берутся из рестора,
хардкод версии в теле таргета исключён (главная болезнь текущего скрипта).

Команды инструмента:
- `pack zip` (после `dotnet publish`, Release): отсекает сборки из замыкания Mars,
  чистит `wwwroot/_framework`/`_content`, генерирует `_front_plugins.json`, собирает zip;
- `pack nuget` (после `dotnet pack`): допаковывает в nupkg фронт-ассеты плагина
  (очищенные от общих с Mars) в папку пакета (конвенция `mars/front/`) и метаданные
  (см. конвенции ниже).

Метаданные/конвенции пакета (Фаза 0 `PluginDistributionPlan`):
- `<packageType>MarsPlugin</packageType>` — чистый фильтр на nuget.org;
- `marsVersionMin`/`marsVersionMax` (nuspec/`AssemblyMetadata`) — совместимость с хостом,
  проверяется при установке и при загрузке (модель `engines` из VS Code, HACS/Obsidian);
- entry-сборка — по конвенции `<PackageId>.dll` либо явное поле;
- при установке любой источник материализует в папке плагина `mars-plugin.json`
  (id, версия, источник, совместимость, дата) — рантайм читает только его.

**Альтернатива (записана по просьбе пользователя): `dotnet tool`.** Тот же инструмент
как `mdimai666.mars-plugin-sdk`, вызов явными командами `dotnet mars-plugin pack zip|nuget`,
версия — в `.config/dotnet-tools.json` репо плагина (`dotnet tool restore` на любой машине/в CI).
Плюсы: прозрачность, дебаг, нет магии в сборке; минус: нет «кнопки Publish» в VS —
только терминал/CI. Переключение на этот вариант = смена обвязки вызова, ядро то же.

### Установка и раскладка

Единая раскладка `data/plugins/<packageId>/`: entry-dll, сторонние зависимости (только те,
которых нет в замыкании Mars), `wwwroot/`, `mars-plugin.json`, собственный `.deps.json`.

- `ZipInstaller`: закрывает заглушку `InstallPlugin` — валидация структуры, проверка
  совместимости по `marsVersionMin/Max`, запись раскладки. Самокомплектный zip
  (отсечение при паковке обязательно — резолвить при установке из zip неоткуда).
- `NuGetPluginInstaller` (NuGet.Protocol): источники настраиваются (по умолчанию
  nuget.org; список открыт для добавления своего фида — задел под BaGet, Фаза 3
  `PluginDistributionPlan`); скачивание пакета + резолв замыкания; копирование только
  отсутствующих в Mars библиотек; раскладка фронт-ассетов из `mars/front/`;
  материализация `mars-plugin.json`. Проверка `packageType=MarsPlugin`.
- Оба инсталлера используют общую логику раскладки.
- Установка — роль `Admin`/`Developer`, с подтверждением в админке; результат —
  «установлено, требуется рестарт» (плагины грузятся при старте; механизм рестарта
  уточняется при реализации: если готового нет — сообщение админу).
- **Blocklist** (опция, модель n8n): запрещённые `packageId` не ставятся и не грузятся —
  защита без каталога.
- Обновление = скачивание новой версии → замена папки → рестарт; откат = установка
  прежней версии (резервных копий в v1 не держим).

### Политика источников (облачный сценарий)

- `Source = Config` (секция `Plugins` appsettings): принудительные плагины инстанса;
  по умолчанию `Locked` — админка не показывает выключение/удаление; управление только
  конфигурацией (нужно для managed-облака).
- `Source = Zip | NuGet`: обычные, управляются админом.
- Источник и `Locked` видны в списке плагинов.

### Изоляция (фаза про ALC)

Свой `AssemblyLoadContext` + `AssemblyDependencyResolver` (по `.deps.json` плагина) на
каждый плагин — стандартный паттерн «приложение с плагинами»: сборки из замыкания Mars
резолвятся из дефолтного контекста (тип-идентичность с хостом), остальное — из папки
плагина. Это делает безопасными разные версии сторонних библиотек у разных плагинов
и является предусловием будущей выгрузки/горячей замены.

### Жизненный цикл (практики популярных продуктов)

| Заимствуем | Откуда |
|---|---|
| install / activate / deactivate / delete; данные не удаляются при деинсталляции | WordPress |
| проверка совместимости с версией хоста до установки (`engines`) | VS Code, Obsidian, HACS |
| blocklist вредоносных, verified-уровни | n8n |
| подпись пакетов (позже, вместе с «рекомендовано») | Grafana, NuGet signed packages |
| плагин = nuget-пакет, установка из админки | Umbraco 13+, Orchard Core |

В админке (`PluginsListPage` + новые диалоги): статусы установлен/включён/выключен/
несовместим/есть обновление, источник, кнопки управления по политике, установка по
NuGet-id и из zip. Отключение = пропуск загрузки при следующем старте (флаг хранится
через существующую систему Options, ключ = `packageId`).

## Фазы

Каждая фаза — самостоятельный коммит-набор; после каждой решение собирается и работает.

### Фаза 0. Быстрые фиксы найденных багов — **выполнена 2026-09-01**

- ~~`CopyAppAdminStaticWebAssets` → копирование `Mars.Admin.staticwebassets.endpoints.json`~~
  Таргет переименован в `CopyMarsAdminStaticWebAssets`, имя файла исправлено
  (эмпирически подтверждено: файл генерируется сборкой `Mars.Admin` в OutputPath WebApp;
  старый `AppAdmin.*` не существовал нигде → таргет молча не исполнялся).
- ~~Убрать двойную загрузку в `WebAssemblyPluginFrontExtensions`~~ — оставлен один
  `AssemblyLoadContext.Default.LoadFromStream`.
- ~~`isDebug` в манифесте~~ — `app.Environment.IsDevelopment()`.

Проверено: сборка `Mars.slnx` зелёная; `Mars.Plugin.Tests` (3/3) и
`Mars.Plugin.Integration.Tests` (7/7, `PluginExample` проходит полный пайплайн);
в публикации файл эндпоинтов лежит рядом с `Mars.dll`.

### Фаза 1. Структура и именование — **выполнена 2026-09-01**

Сделано:
- Удалён пустой `Mars.Plugin.PluginHost` (ссылки из `Kit.Host` и `Mars.slnx` убраны;
  киты остаются — пользователь подтвердил роль кита как «готового комплекта»).
- Парсерные модели (`deps.json`, endpoints-json) переехали из дублей
  (`Mars.Plugin/PluginProvider/Dto` и `Mars.Plugin.Sdk/Dto`) в одно место —
  `Mars.Plugin.Front.Abstractions`; опечатка имени файла `DependiesJsonDto` устранена
  (класс уже назывался правильно).
- **Отклонение от плана:** DTO/маппинги/`IPluginService` НЕ переносились в
  `Mars.Plugin.Contracts` — текущее размещение в `Abstractions` совпадает с конвенцией
  модулей (`Mars.Options.Abstractions`, `Mars.Docker.Abstractions`: сервисные DTO живут
  в `*.Abstractions`, HTTP-модели — в `*.Contracts`).
- `PluginManager` — зависимости в конструкторе (`ILogger<PluginManager>`,
  keyed-`IFileStorage("data")` из уже зарегистрированных сервисов, без сборки
  провайдера); ручной `FileStorage`/`LoggerFactory` внутри удалены; для хостов без
  `MainServer` keyed-хранилище регистрируется в `AddPlugins` тем же экземпляром.
- Переименования: `WebApplicationPlugin`/`WebApplicationPluginAttribute` →
  `MarsPlugin`/`MarsPluginAttribute` (без шимов — удалены по решению пользователя
  2026-09-01), `PluginData` → `LoadedPlugin`, `InstatitePlugin` → `InstantiatePlugin`;
  `PluginExample` и шаблон `MyMarsPlugin` переведены на новые имена.
- Мёртвый код: `PluginExampleData` + ветки `#if USE_EXAMPLE_PLUGINS` удалены,
  комментированный дамп атрибутов из `PluginInfo` убран.
  **Отклонение:** `NuspecHelper` НЕ удалён/не перенесён — его использует
  `UploadPluginTests` (генерация тестового nuspec); пригодится в Фазе 3.
- Перезаписан `README` `Mars.Plugin` под реальное API.

Проверено: `dotnet build Mars.slnx` зелёная; `Mars.Plugin.Tests` 3/3 (+1 скип),
`Mars.Plugin.Integration.Tests` 7/7 (PluginExample грузится по новому атрибуту,
миграции применяются), точечные `GetPluginTests|UploadPluginTests` 6/6.

### Фаза 2. Манифесты и `Mars.Plugin.Sdk` — **выполнена 2026-09-01**

Сделано:
- Проект переименован `Mars.Plugin.PluginPublishScript` → `Mars.Plugin.Sdk`
  (`mdimai666.Mars.Plugin.Sdk`); коммиченный `Mars.deps.json` удалён, пакуется из вывода
  сборки `Mars.WebApp` при паке (ошибка, если решение не собрано);
  `MarsNugetsDefinition` заменён проверкой префикса `mdimai666.*`.
- Пакет несёт: `tools/` (инструмент + `Mars.deps.json`), `mars/` (оба манифеста),
  `build/mdimai666.Mars.Plugin.Sdk.targets` (автоимпорт, путь через `$(Pkg...)`).
- Команды инструмента: `pack zip` (стрип + манифест + дескриптор + zip, автоматически
  после `publish -c Release`), `pack nuget` (классический лейаут `lib/` + `mars/front`,
  зависимости в nuspec, `packageType=MarsPlugin` — таргет `MarsPluginPackNuget`),
  `debug-manifest` (после Debug-сборки). Дескриптор `mars-plugin.json`
  (id/версия/entry/`MarsVersion`) пишется в обоих потоках.
- Исправлены корневые причины «публикация сломалась»: стрип удалял
  `<Плагин>.staticwebassets.endpoints.json`, который на рантайме читает
  `PluginManifestProvider` (теперь сохраняется); скан фронт-сборок падал
  `ReflectionTypeLoadException` на отсутствующих в папке плагина сборках Марса
  (теперь берётся загружаемое подмножество типов).
- Мёртвые `MarsDeps()`/`MarsEndpoints()` и серверный дубль `ProjectDependencies` удалены.
- Шаблон `MyMarsPlugin` переведён на Sdk (старые отключённые таргеты с хардкодом версий
  удалены); `PluginExample` не пакуется — менять нечего. В `nuget-publish.yml` и
  `pack-local-nugets.ps1` добавлена сборка решения перед паком.

Грабли (зафиксированы в csproj/таргетах): пустой snupkg в Release роняет пак `NU5017`
(`IncludeSymbols=false`); tools-only пакет требует `IsTool`/`IncludeBuildOutput=false`;
многострочные свойства ломают `Exec` — санитизация в таргетах. Известные недочёты:
в папке плагина остаются общие с админкой `_framework/*.wasm` (мёртвый вес, рантайм-фильтр
их не отдаёт; паковое отсечение — позже); пак из полностью чистого состояния одним
`dotnet pack` нестабилен — всегда сначала сборка, затем пак (в доках и скриптах так и есть).

Проверено: пакет в `_LocalNugets` версии релиза; на `MyMarsPlugin` работают publish→zip,
`MarsPluginPackNuget`→nupkg, Debug-манифест; сборка `Mars.slnx` и `Mars.Plugin.Tests` зелёные.

### Фаза 3. Раскладка, инсталлеры, nuget-установка

- `mars-plugin.json` + единая раскладка; детект папок по дескриптору
  (обратная совместимость с `.runtimeconfig.json`).
- Общая логика раскладки; `ZipInstaller.InstallPlugin` реализован (валидация +
  проверка совместимости).
- `NuGetPluginInstaller` (NuGet.Protocol): источники (nuget.org по умолчанию +
  настраиваемый список), резолв замыкания, фильтрация по `Mars.deps.json`,
  раскладка фронт-ассетов.
- Опции: источники, blocklist. REST: `Install(packageId)` / upload zip; клиент
  `Mars.WebApiClient`; роль `Admin`/`Developer`; ответ «установлено, нужен рестарт».

**Готово когда**: тестовый плагин ставится и из локального фида `_LocalNugets`, и с
nuget.org (тестовый пакет), раскладывается в `data/plugins`, подхватывается после
рестарта; banned-по-blocklist пакет не ставится.

### Фаза 4. Изоляция через AssemblyLoadContext

- Свой ALC + `AssemblyDependencyResolver` на плагин; шаренные сборки — из дефолтного
  контекста (список из `Mars.deps.json`).
- Интеграционный тест: два плагина с разными версиями одной сторонней библиотеки.

**Готово когда**: тест на конфликт версий проходит; существующие плагины грузятся как раньше.

### Фаза 5. Жизненный цикл и админка

- `Source`/`Locked`-политика (плагины из конфигурации неудаляемы),
  отключение/включение через Options, удаление (только файлы; про данные —
  предупреждение, модель WordPress), обновление версии, статусы и источник в
  `PluginsListPage`, диалог установки по NuGet-id.

**Готово когда**: полный цикл «нашёл → поставил → отключил → обновил → удалил»
проходится из админки; `Locked`-плагин не даёт себя выключить/удалить.

### Фаза 6. Документация и каталог

- Обновить `ai/PluginCreationGuide.md` и `docs/` (новый Sdk, конвенции пакета,
  политика источников).
- Дальше — Фазы 4–5 `PluginCatalogPlan.md` (установка по NuGet-id с проверкой статуса
  в каталоге, витрина маркетплейса): этот реворк для них фундамент, пересмотра не нужно.

## Риски и ограничения

- **Переименование `WebApplicationPlugin` → `MarsPlugin`**: внешние плагины на старых
  именах не загрузятся (шимы удалены по решению пользователя 2026-09-01); все свои
  плагины и шаблоны переведены на новые имена в Фазе 1, внешние обновляются при выпуске.
- **Конфликт версий сторонних библиотек с хостом**: сборки из замыкания Mars всегда
  резолвятся из хоста (тип-идентичность важнее); плагин, требующий более новую версию
  «марсовой» библиотеки, должен ждать релиза Mars. Документируется как ограничение.
  До Фазы 4 тот же конфликт возможен и между двумя плагинами (дефолтный контекст).
- **Рестарт обязателен** для применения установки/обновления (загрузка при старте);
  горячая догрузка — отдельная большая тема, вне этого плана.
- **Безопасность**: плагин .NET — произвольный код без изоляции (ALC — не
  security-граница). Установка только `Admin`/`Developer`, blocklist, позже —
  подписанные пакеты и модерация каталога.
- **Тирсквоттинг** на nuget.org: организационно зарезервировать префикс `Mars.*` /
  `mdimai666.*` (задача вне кода, Фаза 0 `PluginDistributionPlan`).
