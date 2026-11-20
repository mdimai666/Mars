# FirstPartyPluginsGuide — плагины расширения функционала от автора Mars

Гайд для агента: «тесные» first-party плагины, расширяющие функционал платформы.
Их ведёт автор Mars в отдельных репо рядом с Mars; они глубоко интегрированы
с платформой (ноды, админ-страницы, XActions), распространяются через каталог/nuget
(`packageType=MarsPlugin`) и версионируются против `MarsVersion`. От сторонних
плагинов отличаются поддержкой автора, Cloudy-стилем UI и местом в роадмапе продукта.

- Механика плагинов (установка, ALC, SDK, манифесты) — `ai/PluginSystemGuide.md`.
- Создание/обновление/релиз — хаб `C:\Users\D\Documents\VisualStudio\2026\MarsPluginMaster\QWEN.md`
  (гайды PluginCreateGuide/PluginUpdateGuide/PluginReleaseGuide) и `ai/PluginCreationGuide.md`.
- Каталог и распространение — `ai/PluginSystemGuide.md` §«Каталог плагинов».

## Mars.DockerPlugin (существует)

Docker-функциональность вынесена из core Mars в плагин 2026-11-14 (история:
`git show 3b13b36a:ai/DockerModulePlan.md`).

- Репо: `C:\Users\D\Documents\VisualStudio\2026\Mars.DockerPlugin` (из шаблона
  MyMarsPlugin), PackageId `mdimai666.Mars.DockerPlugin`. Структура: Host+Abstractions
  (`IDockerService`/`DockerService` на Docker.DotNet, `api/Docker/*`), Front (UI,
  определения нод, админ-страницы `/tech/docker`), Shared (DTO),
  `tests/Mars.DockerPlugin.Tests` (гейт `MARS_DOCKER_TESTS=1`).
- В Mars удалены проекты `Mars.Docker.*`, страницы DockerViews и `tests/Mars.Docker.Tests`;
  feature-флаг `DockerAgent` упразднён — **установка плагина = включение**.
- Ноды переопределяют `TypeId` стабильными строками (`module.docker.*Node`:
  Pull/Run/Exec/State/DeleteImage) — rename namespace'ов в `Mars.DockerPlugin.*`
  НЕ ломает существующие flows.
- Сторона Mars: ссылка Docker в `CloudyTechSideBar` условная — по наличию
  `Mars.DockerPlugin.Front` в `PluginLoadAssemblies`.
- Решения пользователя: конфиг демона/registry — appsettings (секция `Docker`),
  не DB-опция; группировка контейнеров в UI по compose-проекту
  (label `com.docker.compose.project`), образов — по repository с тегами;
  в ядре оба примитива `RunContainerOnce` + `ExecInContainer`; compose отложен;
  управление дисками/сетями в UI не делаем.
- Грабли (подробности в репо плагина): маппинги Docker.DotNet → DTO должны быть
  null-безопасными; stdin через attach не работает на Windows (Docker.DotNet#618);
  follow-логи, открытые до старта контейнера, на Windows пустые; у базового `Node`
  уже есть свойство `Container` — поле контейнера в нодах названо `ContainerName`.
- Будущее: live-состояние через docker events stream → hub → debounced reload
  (поллинг отклонён), AiChat `DockerToolset`, compose через docker CLI.

## Планируемые (роадмап)

- **EShop** — плагин интернет-магазина.
- **Умный дом** — плагин автоматизации умного дома.
- **Кластерный запуск** — объединение инстансов Mars в единую сеть.

Детали появятся при старте инициатив; на каждый плагин — своя секция в этом гайде
(репо, состав, решения, грабли).

## Как добавить новый first-party плагин

1. Создать из шаблона MyMarsPlugin (`prepare.ps1`) — по PluginCreateGuide хаба
   MarsPluginMaster; репо — sibling каталог рядом с Mars.
2. Интеграция с платформой: ноды (`INodesLocator` + `INodeImplementFactory`),
   админ-страницы в Cloudy-стиле (`ai/CloudyGuide.md`), XActions (`ai/XActionsGuide.md`).
3. Версионирование против `MarsVersion` (обновление платформы — по PluginUpdateGuide
   хаба, карта миграции namespace'ов там же).
4. Релиз — PluginReleaseGuide хаба (тег `v<PluginVersion>`, CI Trusted Publishing);
   публикация в каталог — заявка + модерация.
5. Дописать секцию в этот гайд.