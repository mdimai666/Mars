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
    MyMarsPlugin - основной проект плагина
    MyMarsPlugin.Shared - общий код для backend и frontend
    MyMarsPlugin.Front - код для frontend-части
```

## Примеры
Примеры реализованных плагинов:

| Название                                              |  Описание |
|---|---|
| https://github.com/mdimai666/Mars.PlayAudioNodePlugin | Для проигрывание звука из ресурса.
| https://github.com/mdimai666/Mars.TelegramPlugin      | Интеграция Телеграм бота