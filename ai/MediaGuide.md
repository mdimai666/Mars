# MediaGuide — медиа и файловое хранилище

Гайд для агента: папки медиа, пайплайн загрузки/миниатюр, абстракция `IFileStorage`.
История инициатив (планы схлопнуты): `git show 3b13b36a:ai/MediaFilesPlan.md`,
`git show 3b13b36a:ai/FileStoragePlan.md`. UI страницы `/Media` и пикера —
`ai/CloudyGuide.md` §«Media»; здесь серверная механика и хранилище.

## Где что лежит

- `IFileStorage` + сахар `FileStorageExtensions` — `src/Server/Mars.Server.Abstractions/Services/`.
- Реализации: `FileStorage` (диск), `InMemoryFileStorage` (тесты) — `src/Server/Mars.Storage/Services/`.
- Медиа-модуль `src/Mars.Modules/Mars.Media.*`:
  - `Host/Services/` — `FileService` (загрузка/удаление/миниатюры), `MediaService : FileService`
    (сканирование, генерация миниатюр), `MediaFolderService` (папки).
  - `Host/Controllers/MediaController.cs` — `api/Media/*`.
  - `Abstractions/Services/IImageProcessor.cs` — обработка изображений (MagicScaler).
  - `Contracts/Files/` — DTO (`FileResponse`, `FolderResponse`, запросы; KPI-ключи `MediaKpiKeys`).
- Сущности: `FileEntity` (`files`, `FolderId?`, `FilePhysicalPath`, `Meta.Thumbnails` jsonb),
  `MediaFolderEntity` (`src/Server/Mars.Data/Entities/`, таблица `media_folders`).
- Два корня (`MainServer.cs`, `IOptions<FileHostingInfo>`): default = `{ContentRoot}/wwwroot/upload`
  (раздаётся статикой как `/upload/...`), keyed `"data"` = `{ContentRoot}/data`
  (плагины, flows, JWT-ключи). В Docker data = `/data`.

## Как работает

### IFileStorage

- **12 примитивов**: `OpenRead`, `Write(path, Stream)`, `WriteAsync`, `FileExists`,
  `DeleteFile` (идемпотентный, возвращает факт удаления), `GetDirectoryContents`,
  `GetFileInfo` (`IFileInfo?`), `CreateDirectory`, `DirectoryExists`, `DeleteDirectory`,
  `MoveFile`, `MoveDirectory`. Сахар (`ReadAllText/ReadAllBytes/Write(string)/Write(byte[])`) —
  extension-методы, НЕ члены интерфейса.
- Форма — по образцу Orchard Core `IFileStore`: примитивы в интерфейсе, сахар снаружи,
  идемпотентность вместо пар `Delete/DeleteIfExist`, никаких физических путей в медиа-коде.
- **Сахар — extensions, не default interface methods**: `IFileStorage` подменяется через
  NSubstitute, а прокси перехватывает члены с дефолтной реализацией — тело DIM не выполняется,
  получается молчаливый `null`.
- **data-корень не трогаем**: `PhysicalPath` там легален — `AssemblyLoadContext.LoadFromAssemblyPath`,
  `AssemblyDependencyResolver` и wwwroot-композитинг плагинов без реального пути не работают.
- `InMemoryFileStorage` выровнен с дисковой семантикой: листинг — только непосредственные дети
  (сравнение родителя, не префикса — иначе `Media` матчит `MediaThumbs/*`); `DeleteDirectory` —
  префикс `path + '/'`, `IOException` на непустом при `recursive: false`; `CreateDirectory`/
  `Write`/`MoveFile` регистрируют все родительские сегменты; `OpenRead` — поток только на чтение.
- Медиа-пайплайн БЕЗ `FileStream` и абсолютных путей: авто-ресайз — `ProcessImage` в
  `MemoryStream` → `Write`; габариты — из готовых байт (`ImageSize(ms)`), не из настроек
  ресайза; миниатюры — один `OpenRead` источника вне цикла + `Position = 0` перед каждой
  конфигурацией; сканирование — рекурсивный обход `GetDirectoryContents`.
- Миниатюры зеркалят путь: `MediaThumbs/{тот же подпуть}`, меты в `FileEntity.Meta.Thumbnails`.
  `FileUrl` миниатюр запекается в jsonb при записи — при смене схемы отдачи станет мёртвым
  грузом (Orchard хранит только path и считает URL на отдачу).

### Папки (DB-first)

- Папка = запись `MediaFolderEntity` + физический каталог, создаются вместе. Каталоги без
  записи в БД в UI НЕ показываются — регистрируются через «Сканировать файлы» (`ScanFiles`).
- `Path` уникален от корня upload (напр. `Media/2026`); `ParentId`-дерево произвольной
  вложенности; `FileEntity.FolderId` (nullable) + денормализованный `FilePhysicalPath`,
  обновляются в одной транзакции при переносах/переименованиях.
- Переименование папки — только «на месте» (тот же родитель): `MoveDirectory` + транзакционная
  перепись путей папки/вложенных, файлов и миниатюр (включая физический перенос миниатюр).
- Удаление — только пустой папки (нет файлов/подпапок в БД и каталог пуст на диске).
- Загрузка в корне сохраняет `Media/{год}` (папка года — find-or-create); в открытой папке —
  в неё. Миграция `AddMediaFolders` создала папки из существующих каталогов (рекурсивный CTE).
- API: `GET/POST api/Media/folders`, `PUT folders/{id}/rename`, `DELETE folders/{id}`,
  `POST api/Media/move-files`; `folderId` в `Upload` и list-запросах (null = все,
  `Guid.Empty` = корень).

## Тесты

- `tests/Mars.Server.Tests/Files/` — `FileStorageTests`, `InMemoryFileStorageTests`,
  `FileStorageMoveTests` (реальный диск, temp), `FileServiceTests`, `ReadFileStorageTests`.
- `tests/Mars.Integration.Tests/Controllers/Medias/` — Upload/Delete/ListFile/MediaFolder.
- Интеграционные тесты работают на `InMemoryFileStorage` (оба корня в `ApplicationFixture`);
  assertions — через `_fileStorage.*`, НЕ `File.Exists`/`FileInfo` по диску.
- **E2E (`E2EServerFixture`) сознательно на диске**: медиа отдаётся статикой по HTTP,
  с in-memory файлы были бы недоступны браузеру.
- Прогон: `dotnet test tests/Mars.Server.Tests --filter "FullyQualifiedName~Files"` +
  `dotnet test tests/Mars.Integration.Tests --filter "FullyQualifiedName~Medias"`.

## Грабли

- «Зелёный» тест с проверкой диска может быть зелёным за счёт старого мусора от прежних
  прогонов (прецедент: `HttpInFormSaveFilesNodeTests` писал через `IFileStorage`, проверял
  `File.Exists` и проходил на остатках). Чистку и перевод на InMemory делать вместе.
- Мусор в `wwwroot/upload` от исторических прогонов удалён 2026-09-09 (584 файла);
  новые прогоны диск не трогают. Рабочие медиа dev-инстанса не удалять; каталоги не
  удалять (могут быть зарегистрированы как папки в БД).
- Габариты из `result.Settings` — зависимость от внутренностей MagicScaler; брать из
  готовых байт (assertion «габариты в мете == реальные» в `UploadMediaTests`).
- `MarketplaceTests` исходил из выключенного каталога, а appsettings WebApp его включил —
  в фикстуре каталог выключен in-memory-переопределением (`PluginCatalog:Enabled = false`).

## Инварианты

- Медиа-код не использует абсолютные пути и `FileStream` — только `IFileStorage`
  (готовность к S3 без переделки медиа).
- `IFileStorage` остаётся в `Mars.Server.Abstractions`, имя и пакет не меняем;
  второй интерфейс не заводим.
- Не добавлять в интерфейс: `Copy`, `OpenWrite`, async-двойники, флаг `overwrite`,
  `IFileStoreCapabilities` — только при реальном S3.
- Физические каталоги без записи в БД не показываются в UI.

## Отклонено

- S3-провайдер сейчас НЕ внедряется — только готовность интерфейса (Move = copy+delete
  по префиксу). Async-миграция интерфейса отложена: упирается в синхронные конструкторы
  `KeyMaterialService`, `PluginManager`, `PluginRegistry`, `NodeService` и pre-Build
  pipeline плагинов.
- Перевод плагин-стека на встроенный `IFileProvider` — он только для чтения, установка
  плагина пишет; полной заменой не является.
- Смена модели отдачи файлов (статика `/upload/...`) — не меняли. Справка: Orchard отдаёт
  медиа самим приложением; стоимость S3 определяется egress (Cloudflare R2: хранение
  $0.015/ГБ-мес, исходящий трафик $0; MinIO на своём VPS — ноль стоимости запросов).

## Бэклог

- Права доступа на папки, значки папок (`Icon` в сущности есть), перенос папки в другую
  папку, drag-and-drop, глобальный поиск файлов по всем папкам.
- S3-провайдер (`Mars.Storage.S3`, AWSSDK.S3 + `ServiceURL`/`ForcePathStyle` для MinIO).
- Известные долги: `PostController.Upload` передаёт `userId = Guid.Empty` → `WriteUpload`
  бросает `ArgumentException`; `NotifyService` читает шаблон письма сырым `File.ReadAllText`
  относительно CWD (мимо любого корня) и принимает неиспользуемый `IFileStorage`.