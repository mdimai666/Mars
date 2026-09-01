# Mars.Plugin

Хост плагинной подсистемы Марса: загрузка плагинов из конфигурации и `data/plugins`,
статика и фронт-манифесты плагинов, миграции, установка из zip.

## Подключение в composition root

```csharp
// CreateBuilder
builder.AddPlugins();

// Configure (после app.Build())
app.UsePlugins();
app.ApplyPluginMigrations();
```

`AddPlugins` загружает и инстанцирует плагины до сборки контейнера (они регистрируют
свои сервисы и MVC-части), поэтому `PluginManager` создаётся сразу и кладётся
в контейнер синглтоном.

## Источники плагинов

- Секция `Plugins` конфигурации: ключ — имя, значение `{ AssemblyPath, ContentRootPath }`
  (dev-режим, когда сборка плагина лежит рядом с кодом).
- Папка `data/plugins/<имя>` — развёрнутая публикация плагина (`<имя>.runtimeconfig.json`
  рядом с `<имя>.dll`). Установка — загрузкой zip через админку (`PluginController.UploadPlugin`).
- Ключи/папки, начинающиеся с `_`, пропускаются.

## Точка входа плагина

Плагин наследует `Mars.Plugin.Abstractions.MarsPlugin` и объявляется атрибутом сборки
`[assembly: MarsPluginAttribute(typeof(MyPlugin))]`. Переопределяемые хуки:
`ConfigureWebApplicationBuilder` (до сборки контейнера) и `ConfigureWebApplication`
(роуты/статика/пайплайн). Миграции — `IPluginDatabaseMigrator`.

## Что сервится для каждого плагина

`/_plugin/<keyName>/`:
- статика из `wwwroot` плагина;
- `_front_plugins.json` — фронт-манифест для админки (генерирует `PluginManifestProvider`,
  фильтруя общие с Марсом ассеты);
- `/health`.
