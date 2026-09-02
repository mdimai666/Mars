# Система плагинов

> Сводное описание системы после реворка (план `ai/PluginSystemReworkPlan.md`
> выполнен 2026-09-01 и схлопнут в этот гайд). Полный план с историей фаз и
> as-is-анализом — в истории git: добавлен в `5527ba76`, финальная версия —
> `git show 2fd32dab:ai/PluginSystemReworkPlan.md`.

Плагины устанавливаются из NuGet и zip, живут в единой раскладке `data/plugins`,
управляются из админки (включение/отключение/обновление/удаление) и из CLI
(`Mars.exe plugin ...`), изолируются через AssemblyLoadContext. Стратегия
распространения — `ai/PluginServer/PluginDistributionPlan.md`,
каталог-сервер — `ai/PluginServer/PluginCatalogPlan.md` (его Фазы 4–5 строятся на
этой системе); авторам плагинов — `ai/PluginCreationGuide.md` и `docs/dev_docs/Plugins/`.

## Проекты

| Проект | Роль |
|---|---|
| `Mars.Plugin.Abstractions` | API автора: `MarsPlugin`, `MarsPluginAttribute`, `PluginSettings`, `IPluginDatabaseMigrator`; сервисные DTO и `IPluginService` (конвенция модулей) |
| `Mars.Plugin.Contracts` | HTTP-модели установки и управления |
| `Mars.Plugin` | Хост: `PluginManager`, загрузчики, Zip/NuGet-инсталлеры, реестр, `PluginManifestProvider` |
| `Mars.Plugin.Front.Abstractions` | Манифестные модели, общие серверу, SDK и WASM (`MarsFrontPluginManifest`, DTO `deps.json`/endpoints) |
| `Mars.Plugin.Front` | WASM-загрузчик плагинов админки |
| `Mars.Plugin.Kit.Host` / `Mars.Plugin.Kit.Front` | Мета-пакеты — точка входа автора плагина |
| `Mars.Plugin.Sdk` | Инструмент паковки, публикуется как nuget `mdimai666.Mars.Plugin.Sdk` |

## Манифесты Марса — один источник

При сборке/публикации Марса генерируются два артефакта:
- `Mars.deps.json` — замыкание сборок WebApp: фильтр «уже есть в Марсе» при
  установке из nuget и «шаренный с хостом» набор для ALC;
- `Mars.Admin.staticwebassets.endpoints.json` — статика админки: рантайм-фильтр
  фронт-ассетов плагинов.

В репо не коммитятся; оба пакуются в nuget `mdimai666.Mars.Plugin.Sdk` той же
версии, что и релиз Марса (`MarsAppVersion` в `Directory.Build.props`).
Классификация «пакет Марса» — по членству в замыкании, не по префиксу имени.

Важно: `Mars.deps.json` содержит ТОЛЬКО сборки из пакетов — сборки shared
framework (`Microsoft.AspNetCore.App` и др.) в него принципиально не попадают.
Поэтому замыкание для фильтрации при установке дополняется на лету именами из
`TRUSTED_PLATFORM_ASSEMBLIES` хост-процесса (`MarsClosure`).

## Инструмент паковки — `mdimai666.Mars.Plugin.Sdk`

Один консольный инструмент в пакете: `tools/Mars.Plugin.Sdk.dll` + `build/*.targets`.
Таргеты вызывают его через `$(Pkgmdimai666_Mars_Plugin_Sdk)` — путь и версия из
рестора, без хардкода. Автор добавляет:

```xml
<PackageReference Include="mdimai666.Mars.Plugin.Sdk" Version="..." PrivateAssets="all" />
```

Потоки:
- `dotnet publish -c Release` → автоматически `pack zip`: отсечение сборок из
  замыкания Марса, чистка `wwwroot/_framework`/`_content`, запись дескриптора
  `mars-plugin.json`, сборка `<PackageId>-<Version>.zip` рядом с папкой публикации.
  Фронт-манифест в пакет не кладётся — сервер генерирует его на лету.
- `dotnet msbuild -t:MarsPluginPackNuget -c Release` → `pack nuget`: классический
  лейаут — сборки в `lib/` (зависимости в nuspec — Марс резолвит и отфильтрует при
  установке), фронт-ассеты в `mars/front/`, дескриптор в `mars/`.

Конвенции пакета:
- `<packageType>MarsPlugin</packageType>` — чистый фильтр на nuget.org;
- `marsVersionMin`/`marsVersionMax` — совместимость с хостом, проверяется при
  установке и загрузке;
- entry-сборка — по конвенции `<PackageId>.dll`;
- `mars-plugin.json` (id, версия, entry, `MarsVersion`) — рантайм читает только его.

Перед `dotnet pack` SDK решение должно быть собрано той же конфигурацией —
манифесты берутся из вывода `Mars.WebApp`.

## Установка и раскладка

Каждый установленный плагин — `data/plugins/<packageId>/`: entry-dll, сторонние
зависимости (только отсутствующие в замыкании Марса), `wwwroot/`, `mars-plugin.json`,
собственный `.deps.json`. Детект папок — по дескриптору (обратная совместимость с
`.runtimeconfig.json`).

Инсталлеры:
- **Zip** (`PluginZipInstaller`): валидация, проверка совместимости по
  `marsVersionMin/Max`, переименование папки в `<PackageId>`, перенос с ретраями
  мимо временных файловых локов (антивирус/индексатор на Windows). Zip обязан быть
  самокомплектным — отсечение делается при паковке.
- **NuGet** (`PluginNugetInstaller`, NuGet.Protocol): настраиваемые фиды
  (nuget.org по умолчанию + свой список — задел под каталог), резолв замыкания
  зависимостей, копирование только отсутствующих в Марсе библиотек, раскладка
  фронт-ассетов из `mars/front/`.

Скорость установки (механизмы, не убирать — см. чек-лист ниже):
- пакет из замыкания Марса (по `Mars.deps.json` targets + `TRUSTED_PLATFORM_ASSEMBLIES`)
  не резолвится и не скачивается вовсе — до него и его транзитивных зависимостей
  нет дела;
- скачивание идёт через глобальную папку nuget (`~/.nuget/packages`, как у
  `dotnet restore`): пакет@версия уже на диске → чтение локальное, без сети;
- кэш резолва плавающих версий `data/plugins/.resolve-cache.json` (TTL 1 час):
  повторные установки не спрашивают фид «какая версия подходит».

Метаданные для списка (имя/описание/иконка):
- при установке из nuget дескриптор дополняется метаданными пакета
  (`PackageMetadataResource`: Title/Description), иконка пакета извлекается в
  `wwwroot/` (`IconFile` в дескрипторе; сервится через `/_plugin/<key>/`);
- zip: Title/Description/иконку в `wwwroot/` пишет `Mars.Plugin.Sdk` при паковке
  (`--Title` таргетов из `<Title>` в csproj плагина);
- `PluginInfo` предпочитает поля дескриптора, атрибуты сборки — фолбэк; список
  ДО рестарта (реестровые записи) тоже берёт их из дескриптора.

Эндпоинты: `POST api/Plugin/InstallFromNuget` и upload zip; роль
`Admin`/`Developer`; ответ «установлено, нужен рестарт». Blocklist (опция
`BlockedPackageIds`) запрещает установку по `packageId`.

### Была проблема: дубли сборок при установке из nuget

До 2026-09-02 установка тянула в папку плагина всё, что не описано в
`Mars.deps.json`: сборки shared framework (`Microsoft.AspNetCore.*`,
`Microsoft.Extensions.*` — их в deps.json нет), спутники `.xml`/`.pdb` и
локализации перекрытых пакетов, мусор упаковки (`_._`, чужие `.deps.json`,
`runtimeconfig.json`). Пример: 357 файлов вместо ~150, установка минут 2–3.
Фильтр `ExtractPackage`/`MarsClosure` это убирает (357 → 157 на тестовом плагине).

**Чек-лист при правках установки плагинов** (фильтр/резолв/раскладка) — чтобы
дубли не вернулись:

1. `Mars.exe plugin install <тестовый-плагин>` → осмотреть
   `data/plugins/<id>`: отсутствуют сборки Марса (в т.ч. `Microsoft.AspNetCore.*`,
   `Microsoft.Extensions.*`), нет спутников `.xml`/`.pdb` к ним, нет
   `_._`/`*.deps.json`/`runtimeconfig.json`.
2. Нужные плагину сторонние библиотеки на месте и работают после рестарта
   (`plugin list`: плагин грузится, фронт-манифест и статика раздаются).
3. В `data/logs` видны `Skipping '...' — already shipped with Mars` — фильтр
   реально отрабатывает (уровень `Mars.*: Debug` в `Logging:File:LogLevel`).

## Реестр и источники

`data/plugins/.registry.json` — единственный источник состояния: источник, версия,
дата, `Disabled`, отложенные отметки `PendingDelete`/`PendingStagingDir`.

Источники и политика:
- `Config` (секция `Plugins` appsettings) — принудительные плагины инстанса,
  `Locked`: админка не даёт выключить/удалить, управление только конфигурацией
  (сценарий managed-облака);
- `Zip` / `NuGet` — обычные, управляются админом.

Источник, статус и отложенные отметки видны в списке плагинов.

## Изоляция — AssemblyLoadContext

Каждый плагин грузится в свой `PluginLoadContext` с `AssemblyDependencyResolver`
по его `.deps.json`: сборки из замыкания Марса резолвятся из дефолтного контекста
(тип-идентичность с хостом), остальное — из папки плагина. Разные плагины могут
использовать разные версии сторонних библиотек.

Фронт-сборки (wasm) грузятся отдельным `Assembly.LoadFrom` — они вне замыкания
хоста и не конфликтуют.

## Жизненный цикл

Модель в духе WordPress/VS Code: install / activate / deactivate / delete; данные
плагина при удалении не трогаются. Эндпоинты `SetEnabled` / `Uninstall` / Update
(переустановка последней версии из nuget).

Все изменения применяются **после рестарта** (загрузка при старте):
- отключение — отметка `Disabled` в реестре: плагин не грузится, но остаётся в
  списке админки и может быть включён обратно;
- удаление — всегда отложенное, только отметка `PendingDelete`, файлы до рестарта
  не трогаются (единообразно на Windows и Linux); при старте папка удаляется,
  неудача оставляет отметку до следующего рестарта;
- обновление: папка свободна — замена сразу; занята залоченной сборкой — новая
  версия кладётся в `plugins/_pending_<PackageId>_<guid>` с отметкой
  `PendingStagingDir` и подменяется при старте.

Переустановка плагина отменяет его удаление (отметки сбрасываются). Отметки
применяются в начале старта, строго до чтения `data/plugins`.

## CLI-управление (`Mars.exe plugin ...`)

Плагинами можно управлять из командной строки (список, установка из nuget,
отключение/включение, удаление) — поверх того же `IPluginService`, что и админка.
Точный набор команд — `Mars.exe plugin -h`.

Как и прочие CLI-команды, выполняются в живом инстансе по UDS (или in-process);
мутирующие команды менять живой сервер — запускать только с подтверждением
пользователя.

## Ограничения

- Рестарт обязателен для применения установки/обновления/удаления/отключения;
  горячая догрузка — отдельная большая тема.
- Сборки из замыкания Марса всегда резолвятся из хоста: плагин, требующий более
  новую версию «марсовой» библиотеки, ждёт релиза Марса.
- ALC — не security-граница: плагин — произвольный .NET-код. Установка только
  `Admin`/`Developer`, blocklist; позже — подписанные пакеты и модерация каталога.
- Переименование `WebApplicationPlugin` → `MarsPlugin` прошло без шимов: плагины
  на старых именах не загрузятся.
- `Mars.Plugin.Sdk` (`pack nuget`) не пишет в nuspec `<license>`, `<repository>`
  и readme — даже если автор задал `PackageLicenseExpression`/`RepositoryUrl`
  в csproj. На nuget.org у пакетов плагинов не отображаются лицензия и
  репозиторий (warning `License missing`/`Readme missing` при push). Чинится
  в SDK, не в плагине (подтверждено на mdimai666.Mars.TelegramPlugin 0.6.1).

## Дальше

Фазы 4–5 `ai/PluginServer/PluginCatalogPlan.md`: установка по NuGet-id с проверкой
статуса в каталоге, витрина маркетплейса. Фундамент готов:
`packageType=MarsPlugin` + `mars-plugin.json` в пакетах, `NuGetPluginInstaller` с
настраиваемыми фидами, `InstallFromNuget` по id, реестр и жизненный цикл в админке.
