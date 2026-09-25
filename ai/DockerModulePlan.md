# Mars.Docker — финализация модуля

Ветка: `ai/docker-finalize`. Цели: рабочий CRUD контейнеров/образов в админке,
конфиг источника Docker из appsettings, примитивы RunOnce/Exec для нод и плагинов,
в будущем — ноды, XActions, AiChat-тулсет и рецепты маркетплейса (пример: плагин
ставит образ python и регистрирует ноду исполнения Python).

## Решения пользователя (2026-09-24)

- Конфиг (endpoint демона, registry-источник) — **appsettings**, не DB-опция.
- Группировка контейнеров в UI — **по compose-проекту** (label `com.docker.compose.project`),
  остальные — standalone; образы — группировка по repository с тегами (как Docker Desktop).
- В ядре **оба примитива**: `RunContainerOnce` (one-shot, `--rm`-семантика) и `ExecInContainer`.
- **Compose отложен**; управление дисками/сетями в UI не делаем — редактор простой.

## Модули

- `src/Mars.Modules/Mars.Docker.Contracts` — wire-DTO (зеркала Docker.DotNet c суффиксом `1`).
- `src/Mars.Modules/Mars.Docker.Abstractions` — Query-DTO + `DockerQueryMapping`.
- `src/Mars.Modules/Mars.Docker.Host` — `IDockerService`/`DockerService` (Docker.DotNet),
  `DockerController` (`api/Docker/*`), `MainDocker.AddMarsDocker`. Feature-флаг `DockerAgent`
  (`FeatureFlags.cs`, appsettings: false / Development: true), подключение — `MarsWebAppStartup.cs`.
- `src/Mars.Modules/Mars.Docker.Front` — RCL: клиент `client.Docker()` (Flurl) + компоненты;
  страницы-обёртки в `src/Mars.Admin/Builder/DockerViews/` (`/builder/docker`).

## Этап 1 — ядро (бэкенд)

- `DockerOptions` (Host, секция `Docker`, паттерн `PluginCatalogOption`): `Endpoint`
  (пусто = локальный сокет), `RegistryUrl` (пусто = Docker Hub), `RegistryUser/Password`,
  `DefaultRunTimeoutSeconds`. Привязка в `AddMarsDocker(IConfiguration)`, клиент из endpoint.
- `CreateContainer` — реализовать (был NotImplementedException): простой DTO
  (Image, Name, Cmd, Env, порты, WorkingDir, RestartPolicy, AutoRemove) — старый
  30-польный mirror-request заменён простым.
- `RunContainerOnce` — создать контейнер, attach stdin/stdout/stderr, start, дождаться
  выхода (таймаут → kill), собрать stdout/stderr/exit code, удалить. Результат —
  `DockerRunResultResponse`.
- `ExecInContainer` — exec API (create/start-and-attach/inspect на exit code).
- `PullImage(name, tag, progress, ct)` — registry-префикс из опций (эвристика: имя уже
  содержит registry, если первый сегмент с `.`/`:`/`localhost`), AuthConfig из опций,
  прогресс `IProgress<string>`.
- `GetContainerLogs(id, tail)` — stdout/stderr отдельно.
- Контроллер: + CreateContainer, ListImages/ListTableImages, PullImage, DeleteImage,
  RunOnce, Exec/{id}, GetLogs/{id}; InspectContainer переведён на `ContainerInspectResponse1`.

## Этап 2 — админка (завершён)

- Вкладки Containers / Images (volumes без UI).
- Контейнеры: группировка по compose-проекту, standalone — по статусу; починить
  `ListContainers`/`ListTableContainers` в клиенте; реализовать Delete (был NotImplemented).
- Образы: группировка по repository (теги свёрнуты), Pull с индикатором, Delete.
- Create container: простая форма (образ/имя для pull, имя, порты, env, команда).
- Detail: состояние, порты, логи + inspect JSON (как сейчас).

## Этап 2.5 — интерактивность (2026-09-24)

- Список контейнеров — **иерархический FluentDataGrid** (нативный `HierarchicalGridItem`
  из FluentUI 4.14.4, колонка с `HierarchicalToggle`): группы по compose-проекту
  свёрнуты по умолчанию, одиночные контейнеры без группы — плоскими строками; колонки
  Name 4fr / Image 2fr / Ports 1fr / Started 1fr / Actions auto. Имена подэлементов:
  без ведущего `/` и без префикса группы. Start/stop — одна toggle-кнопка; у группы —
  свой toggle (есть running → stop all, иначе → start all).
- Create-поток: чекбокс «Start after creation» (по умолчанию вкл) → после создания
  переход на страницу контейнера с `?once=1`.
- Разовые контейнеры (вывод перед выходом):
  - обычный (без auto-remove): `WaitContainer(id, timeout)` — wait + логи + exit code;
    страница с `once=1` ждёт выход и перечитывается;
  - **auto-remove**: демон удаляет контейнер вместе с логами, поэтому фронт снимает
    AutoRemove-флаг и зовёт `StartAndCapture(id, timeout, removeAfterExit)` — start →
    wait → логи → результат в **IMemoryCache** (ключ `docker:run-result:{id}`, TTL 10 мин)
    → удаление контейнера уже нами. Страница при not-found забирает
    `GET RunResult/{id}` и рисует «Run result» (stdout/stderr/exit code).
- `DockerStateBadge.ExitCode`: exited(0) зелёный, exited(≠0) красный, без кода — серый.
- Detail: мета-панель (id/image/command/exit code+OOM/error/ports/created/started/finished),
  toggle start/stop, reload логов через ReloadToken после действий; панель логов —
  свитч «Auto refresh» (3с, только running). Inspect — CodeEditor2 с
  `ContainerCssStyle="height:100%..."` (дефолт 80vh вылезает из бокса).
- **Вебсокеты (live-состояние) отложены**; дизайн согласован: docker events stream
  (`System.MonitorEventsAsync(ContainerEventsParameters, IProgress<Message>)`, фильтр
  type=container — ловит ВСЕ внешние изменения) → `DockerEventsBackgroundService` →
  `IHubContext<ChatHub>`/`AdminHubEvents` → `ClientHub` → debounced reload страницы.
  Пользователь отверг поллинг-вариант как избыточный.

## Этап 3 — ноды (завершён; раскладка Contracts+Host по живому паттерну SemanticKernel, не отдельный проект)

- Декларации — `Mars.Docker.Contracts/Nodes` (группа `docker`, TypeId `docker.*Node`,
  цвет `#2496ED`, иконка `_content/Mars.Docker.Front/nodes/docker.svg`):
  `DockerPullNode` (image/tag), `DockerRunNode` (one-shot: image/command/env/stdin
  (пусто = payload msg)/timeout/keep — база Python-сценария), `DockerExecNode`
  (container id-или-имя/command/env/workdir/user/timeout), `DockerStateNode`
  (start/stop/restart/pause/unpause/delete), `DockerDeleteImageNode`.
  **Грабля:** у базового `Node` уже есть свойство `Container` (принадлежность подсхеме) —
  поле контейнера в нодах названо `ContainerName`.
- Имплементации — `Mars.Docker.Host/Nodes` (`INodeImplement<T>`, `IDockerService` из
  `RNS.ServiceProvider`); контейнер резолвится по полному id (64 hex) или имени
  (`DockerNodeHelper.ResolveContainerId`).
- Регистрация: сервер — `MainDocker.UseMarsDocker` (`INodesLocator` +
  `INodeImplementFactory`), вызов в `MarsWebAppStartup` под гейтом `DockerAgent`;
  фронт — `MainDockerFront.Add/UseDockerFront` (`INodesLocator` + `INodeFormsLocator`),
  вызов в `Mars.Admin/Program.cs`. Формы нод — `Mars.Docker.Front/Nodes/Forms`
  (`[NodeEditFormForNode]`, `@inherits NodeEditForm`, `FormItem2`; FluentSelect —
  `TOption`+`Items`+`OptionText`, числа — `FluentNumberField`, не FluentTextField).
- Тесты: `DockerNodesIntegrationTests` (pull → payload; run echo → DockerRunResultResponse;
  state start/stop по имени; exec в живом контейнере); RNS — NSubstitute,
  `ServiceProvider` — реальный DI с `IDockerService`. **Грабля:** параллельные классы
  тестов мешали друг другу (`sleep 60` нодового контейнера ловился предикатом
  RunOnce-kill-теста) — команды контейнеров в разных классах должны различаться.

## Этап 4 — XActions + рецепт-пример

- XActions модуля (start/stop/pull) через `XActionBuilder` — поверхность для рецептов и ИИ.
- Пример плагина `PluginPythonDocker` (рядом с `src/Plugin/PluginExample`): тянет
  `python:3.12-slim`, регистрирует `PythonNode` через `AutoHostRegisterHelper`.

## Будущее (вне текущего объёма)

- Compose (через docker CLI), AiChat `DockerToolset` (`IAiToolset` + строка DI в
  `MainAiChat`, гейт по DockerAgent) — когда ИИ начнёт управлять нодами.

## Грабли

- **Маппинги Docker.DotNet → Contracts**: у моделей Docker.DotNet массово nullable-поля
  (`ImagesListResponse.Labels`, `ContainerInspectResponse.Node` — null вне swarm,
  `ContainerState.Health` — null без healthcheck, `Mount.BindOptions/VolumeOptions/TmpfsOptions`,
  `Config.ExposedPorts/Volumes`, коллекции устройств). BCL `.ToDictionary()` на null бросает —
  все маппинги в `Mappings/` сделаны null-безопасными (null-объект → пустой инстанс,
  null-коллекция → пустая). Новые маппинги писать так же; проверяется
  `tests/Mars.Docker.Tests/DockerMappingsIntegrationTests.cs` (opt-in).
- **stdin через attach не работает на Windows** (Docker Desktop): Docker.DotNet шлёт attach
  без `Connection: Upgrade`/`Upgrade: tcp`, прокси Desktop не пробрасывает запись client→daemon
  (dotnet/Docker.DotNet #618/#664/#223). Подтверждено диагностикой 2026-09-24: сырой npipe
  с Upgrade-заголовками — работает, с `Connection: close` — нет; чтение stdout через attach
  работает в обоих случаях; на Linux (unix socket) Docker.DotNet работает. Ещё грабля:
  `MultiplexedStream.CloseWrite()` (нуль-байтовое EOF-сообщение) на npipe ненадёжен
  (go-winio#42) — EOF доставлять полным закрытием соединения при `StdinOnce`.
  Тест `RunOnce_passes_stdin_and_captures_stderr` на Windows скипается до починки.
- **Follow-логи, открытые до старта, на Windows пустые**: `GetContainerLogsAsync(Follow=true)`
  на created-контейнере (поток открыт ДО `StartContainerAsync`) не отдаёт вывод — проверка
  `StartAndCapture` поймала пустой stdout. Детерминированная схема: start → wait →
  штатные логи (контейнер существует, удаление под нашим контролем) → кэш → remove.

## Тесты

- `tests/Mars.Docker.Tests` (xUnit v3/MTP): юнит — маппинги, registry-эвристика, опции;
  интеграционные — opt-in `MARS_DOCKER_TESTS=1` (`DockerContainerFactAttribute`,
  паттерн `tests/Mars.DockerImage.Tests`): pull alpine, run-once, create/delete.
- Проверка: `dotnet build Mars.slnx` + exe тестов затронутых проектов.
