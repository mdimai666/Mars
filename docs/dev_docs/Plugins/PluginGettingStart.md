# Создание плагина

Для удобства разработки был создан репозиторий с примером плагина:
> https://github.com/mdimai666/MyMarsPlugin

## Создание плагина на основе шаблона

Для клонирования и настройки плагина рекомендуется использовать следующее наименование: добавляйте слово Plugin в конце имени. Выполните следующие команды в PowerShell:
```ps1
$newPluginName = "MyNewPlugin"; git clone https://github.com/mdimai666/MyMarsPlugin.git $newPluginName; cd $newPluginName; .\prepare.ps1 $newPluginName
```

## Структура проекта

```
MyNewPlugin/
    src/
        MyMarsPlugin/           # Backend (SDK: Microsoft.NET.Sdk.Razor)
        MyMarsPlugin.Shared/    # Общие DTO, настройки, ресурсы
        MyMarsPlugin.Front/     # Frontend (SDK: Microsoft.NET.Sdk.BlazorWebAssembly)
```

## NuGet пакеты

**Backend:**
- `mdimai666.Mars.Plugin.Kit.Host`
- `mdimai666.Mars.Plugin.Sdk`

**Frontend:**
- `mdimai666.Mars.Plugin.Kit.Front`

## Паковка

Подключите `mdimai666.Mars.Plugin.Sdk` (`PrivateAssets="all"`) — таргеты паковки приходят
вместе с пакетом:

- `dotnet publish -c Release` — отсечение сборок, которые уже есть в Марсе, фронт-манифест,
  дескриптор `mars-plugin.json` и готовый `<PackageId>-<Version>.zip`;
- `dotnet msbuild -t:MarsPluginPackNuget -c Release` — дополнительно `<PackageId>.<Version>.nupkg`.

Как устроен пакет, что он выдаёт и как с ним работать — в [PluginSdk](PluginSdk.md).

## Примеры плагинов

| Плагин | Описание | Сложность |
|--------|----------|-----------|
| [MyMarsPlugin](https://github.com/mdimai666/MyMarsPlugin) | Шаблон плагина | Базовый |
| [Mars.TelegramPlugin](https://github.com/mdimai666/Mars.TelegramPlugin) | Интеграция Telegram бота | Средний |
| [Mars.PlayAudioNodePlugin](https://github.com/mdimai666/Mars.PlayAudioNodePlugin) | Воспроизведение аудио | Сложный (host services) |
| [Mars.SberDevApiPlugin](https://github.com/mdimai666/Mars.SberDevApiPlugin) | GigaChat, SaluteSpeech | Сложный (AI интеграция) |

## Инструкция для AI-агента

Если вы используете AI-агента для создания плагина, передайте ему файл:
> [ai/PluginCreationGuide.md](../../../ai/PluginCreationGuide.md)

Этот файл содержит краткую инструкцию с примерами кода для генерации плагина.
