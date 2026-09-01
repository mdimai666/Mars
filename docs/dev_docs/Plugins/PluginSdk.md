# Mars.Plugin.Sdk — упаковка плагинов

`mdimai666.Mars.Plugin.Sdk` — инструмент сборки и упаковки плагинов Марса. Пришёл на смену
экспериментальному `mdimai666.Mars.Plugin.PluginPublishScript`. Публикуется на nuget.org
в версии релиза Марса и несёт в себе актуальные манифесты Марса, поэтому плагин всегда
упаковывается против той версии платформы, для которой предназначен.

## Что внутри пакета

| Папка в пакете | Содержимое |
|---|---|
| `tools/` | сам инструмент (консольное приложение) и `Mars.deps.json` — замыкание сборок Марса |
| `mars/` | `Mars.deps.json` и `Mars.Admin.staticwebassets.endpoints.json` — манифесты версии релиза |
| `build/` | MSBuild-таргеты, которые NuGet подключает к проекту плагина автоматически |

Таргеты вызывают инструмент по пути `$(Pkgmdimai666_Mars_Plugin_Sdk)\tools\Mars.Plugin.Sdk.dll`:
свойство `$(Pkg...)` NuGet генерирует сам из восстанова, поэтому версия в таргетах не
хардкодится и всегда совпадает с версией подключённого пакета.

## Как подключить

В csproj хост-проекта плагина (например, `MyMarsPlugin.csproj`):

```xml
<ItemGroup>
    <PackageReference Include="mdimai666.Mars.Plugin.Sdk" Version="<версия Марса>" PrivateAssets="all" />
</ItemGroup>
```

Версия пакета = версия Марса, против которого разрабатывается плагин
(например, `0.8.1-alpha.4`). Больше ничего настраивать не нужно: никакие свои
`<Target>` с вызовом инструмента в проект плагина добавлять не надо — они приходят
вместе с пакетом. Настраивать `Private=false`/`ExcludeAssets=runtime` для ссылок на
Марс тоже не требуется — отсечение выполняется самим инструментом при паковке.

## Как работать

### Zip (установка через админку)

```
dotnet publish <проект плагина> -c Release
```

Таргет `MarsPluginPackZip` срабатывает автоматически после публикации и делает:

1. отсекает из вывода сборки всё, что уже есть в Марсе (сборки по `Mars.deps.json`,
   общие `_framework`/`_content` фронт-ассеты, символы);
2. генерирует фронт-манифест `wwwroot/_front_plugins.json`;
3. пишет дескриптор `mars-plugin.json`;
4. собирает `<PackageId>-<Version>.zip` рядом с папкой публикации
   (`bin/Release/net10.0/`).

Сторонние зависимости, которых нет в Марсе, остаются в папке плагина и попадают в zip —
плагин самокомплектный.

### Nuget (для распространения через фид/маркетплейс)

```
dotnet msbuild <проект плагина> -t:MarsPluginPackNuget -p:Configuration=Release
```

Таргет `MarsPluginPackNuget` выполняет ту же подготовку и дополнительно собирает
`<PackageId>.<Version>.nupkg` рядом с папкой публикации.

### Отладка

При обычной Debug-сборке таргет `MarsPluginDebugManifest` генерирует дев-манифест рядом
с артефактами сборки — отдельно ничего запускать не нужно.

## Что получается

### Содержимое папки плагина / zip (плоская раскладка)

```
MyPluginCompany.MyMarsPlugin.dll              # входная сборка
MyPluginCompany.MyMarsPlugin.Front.dll        # свои сборки плагина
MyPluginCompany.MyMarsPlugin.Shared.dll
MyPluginCompany.MyMarsPlugin.deps.json
MyPluginCompany.MyMarsPlugin.runtimeconfig.json
MyPluginCompany.MyMarsPlugin.staticwebassets.endpoints.json   # читает рантайм Марса
mars-plugin.json                                               # дескриптор
wwwroot/
    _front_plugins.json                       # фронт-манифест
    _framework/...                            # wasm только самого плагина
    ...прочие статические файлы плагина
```

### Содержимое nupkg (классический лейаут)

```
lib/net10.0/        # только собственные сборки плагина
mars/front/         # фронт-ассеты (то же, что wwwroot в zip)
mars/mars-plugin.json
icon.png            # если задан PackageIcon
```

Сторонние зависимости в файл не кладутся — они объявлены в `dependencies` nuspec
(вместе с версиями пакетов Марса). При установке Марс сам зарезолвит замыкание и
скопирует в папку плагина только то, чего у него нет.

### Дескриптор `mars-plugin.json`

```json
{
  "PackageType": "MarsPlugin",
  "PackageId": "MyPluginCompany.MyMarsPlugin",
  "Version": "0.0.1",
  "EntryAssembly": "MyMarsPlugin.dll",
  "MarsVersion": "0.8.1-alpha.4",
  "CreatedAtUtc": "..."
}
```

`MarsVersion` — версия Марса, инструментом которой упакован плагин (нижняя граница
совместимости). В nupkg дополнительно пишется `<packageType>MarsPlugin</packageType>` —
по нему пакеты плагинов фильтруются на фиде.

## Установка и управление

Все установленные плагины живут в `data/plugins/<PackageId>/` в единой раскладке
(входная сборка + дескриптор + `wwwroot`), независимо от источника.

- **Из zip** — загрузка через админку (`Plugins` → «Upload from zip-file»).
- **Из nuget** — по `PackageId` (и опциональной версии) через админку
  («Install from NuGet») или `POST /api/Plugin/InstallFromNuget`. Марс скачивает пакет,
  резолвит транзитивные зависимости и раскладывает только то, чего нет в замыкании Марса.
- **Жизненный цикл** (админка, `Plugins`): включить/отключить (Enable/Disable),
  обновить до последней версии (Update), удалить (Delete). Плагины, внедрённые через
  конфигурацию инстанса (секция `Plugins` appsettings), помечены `Locked` — из админки
  их нельзя отключить или удалить.
- Изменения применяются после рестарта сервера (плагины грузятся при старте).
- Источники nuget и блок-лист (`BlockedPackageIds`) настраиваются в опции
  `PluginManagerSettingsOption` (по умолчанию — nuget.org).

Изоляция: каждый плагин загружается в собственный `AssemblyLoadContext` — свои версии
сторонних библиотек не конфликтуют ни с хостом, ни между собой; сборки Марса
резолвятся из хоста (тип-идентичность).

## Для разработчиков Марса

`Mars.Plugin.Sdk` пакует манифесты из вывода сборки `Mars.WebApp`, поэтому перед
`dotnet pack` решение должно быть собрано в той же конфигурации (иначе пак упадёт
с понятной ошибкой). В `pack-local-nugets.ps1` и в CI (`nuget-publish.yml`) эта сборка
уже выполняется автоматически.
