# Mars.Plugin.Sdk

Инструмент паковки плагинов Марса. Публикуется как nuget `mdimai666.Mars.Plugin.Sdk`
версии релиза Марса и несёт в себе актуальные манифесты Марса (`Mars.deps.json`,
`Mars.Admin.staticwebassets.endpoints.json`) — они берутся из вывода сборки
`Mars.WebApp` при паке, в репозитории не коммитятся.

## Использование (проект плагина)

```xml
<PackageReference Include="mdimai666.Mars.Plugin.Sdk" Version="<версия Марса>" PrivateAssets="all" />
```

Таргеты из пакета (`build/Mars.Plugin.Sdk.targets`) подключаются автоматически, путь
к инструменту строится из `$(Pkgmdimai666_Mars_Plugin_Sdk)` — без хардкода версий.

- `dotnet publish -c Release` — после публикации автоматически отсекает сборки,
  которые уже есть в Марсе, пишет фронт-манифест (`_front_plugins.json`) и дескриптор
  (`mars-plugin.json`), собирает `<PackageId>-<Version>.zip` рядом с папкой публикации.
- `dotnet msbuild -t:MarsPluginPackNuget -c Release` — дополнительно собирает
  `<PackageId>.<Version>.nupkg` классического лейаута: собственные сборки в `lib/`,
  фронт-ассеты и дескриптор в `mars/`, зависимости в nuspec (Марс резолвит и
  отфильтрует их при установке — план: `ai/PluginSystemReworkPlan.md`).
- Debug-сборка — генерирует дев-манифест рядом с артефактами.

## Локальная сборка пакета

Перед `dotnet pack` решение должно быть собрано в той же конфигурации
(нужны манифесты из вывода `Mars.WebApp`):

```
dotnet build Mars.slnx -c Release
dotnet pack src/Plugin/Mars.Plugin.Sdk -c Release -o <фид>
```
