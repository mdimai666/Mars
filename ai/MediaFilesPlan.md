# План: папки в медиа

> **Статус: ВСЕ шаги 1–6 выполнены 2026-08-21.** Осталось: пользовательская проверка в UI,
> компиляция less, bump MarsAppVersion после коммита css.
> Задача-источник: запрос пользователя «поддержка папок в медиа» (2026-08-20).
> Контекст: сейчас медиа — плоский список всех файлов; загрузка всегда в `Media/{год}`.

## Принятые решения (2026-08-20)

1. **Модель папок — DB-first, строго только БД.** Папка = запись `MediaFolderEntity` + физический
   каталог, создаются вместе. Физические каталоги без записи в БД НЕ показываются в UI —
   регистрируются через «Сканировать файлы» (расширить `ScanFiles`). Файлы — только из БД.
2. **Привязка файлов** — FK `FileEntity.FolderId` (nullable) + денормализованный
   `FilePhysicalPath` хранится как сейчас и обновляется при переносах/переименованиях
   в одной транзакции с FK (классика: adjacency list + денормализованный путь, без подзапросов).
3. **Переименование папки в v1 — только «на месте»** (меняется имя и последний сегмент пути,
   родитель тот же). Механика: `MoveDirectory` + транзакционное обновление путей.
   Перенос папки в другую папку — позже (та же механика).
4. **Старые файлы** — миграция создаёт папки из существующих каталогов (`Media/2026` → «2026»)
   и привязывает файлы (`folder_id`).
5. **Вложенность произвольная** (`ParentId`-дерево); хлебные крошки в UI.
6. **Задел под S3**: папка = путь-префикс; все операции через `IFileStorage`
   (добавлены `MoveFile`/`MoveDirectory`; для S3 это copy+delete по префиксу).
7. **Загрузка в корне** сохраняет поведение `Media/{год}` (папка года — find-or-create);
   загрузка в открытой папке пишет в неё.

## Как сейчас (as-is)

- Файлы: `FileEntity` (`files`), `FilePhysicalPath` вида `Media/2026/img.jpg`; физически
  `wwwroot/upload/...`, раздаются статикой как `/upload/...` (`StartupHostFiles`).
- `MediaService : FileService` (`src/Mars.Host/Services/`): загрузка всегда в
  `MediaDirByYear = Media/{год}`; `ExecuteAction`: `ScanFiles` (рекурсивный обход диска
  `Directory.*` мимо `IFileStorage`, регистрация найденных файлов в БД), `GenerateThumbnails`.
- Миниатюры зеркалят путь: `MediaThumbs/{тот же подпуть}`, меты в `FileEntity.Meta.Thumbnails` (jsonb).
- `IFileStorage` (реализации `FileStorage`, `InMemoryFileStorage`) уже имеет
  `CreateDirectory/DirectoryExists/DeleteDirectory/GetDirectoryContents` — не хватало Move.
- Контракты плоские: `TableFileQueryRequest/ListFileQueryRequest` без папок;
  репозиторий ищет `ILike` по `FileName`, сортировка `CreatedAt/FileName/FileSize`.
- UI: `FluentMediaFilesList.razor(.cs)` (плоская сетка + загрузка) используется в админке
  (`ManageMediaPage`, `@page "/Media"`) и в пикере `ModalMediaSelect`/`FSelectMedia`.
  Стили — `.less` в AppAdmin (компиляция вручную пользователем).

---

## Шаг 1 — Данные ✅

- `MediaFolderEntity` (`src/Mars.Host.Data/Entities/`): `Id`, `CreatedAt`, `ModifiedAt`,
  `Name` (= имя физического каталога), `Path` (уникальный, от корня upload, напр. `Media/2026`),
  `ParentId?` + `Parent`/`Children`, `CreatedBy` (без FK на users), `Icon?` (задел), `Files`.
- `FileEntity.FolderId?` + навигация `Folder`.
- DbSet `MediaFolders` в `MarsDbContext` и `PluginDbContextBase` (плагин-контекст подхватывает
  сущность автоматически через `ListMarsAllEntities`).
- Конфигурации: `Mars.Host.Data.PostgreSQL/Configurations/MediaFolderEntityConfiguration.cs`
  (таблица `media_folders`, unique-индекс по `path`, `Parent → Children` Restrict,
  `Files → Folder` SetNull) + аналог в `Mars.Host.Data.InMemory`.
- Миграция `AddMediaFolders` (+ data-миграция: рекурсивный CTE по каталогам из
  `file_physical_path` → папки, `parent_id`, привязка `files.folder_id`).

## Шаг 2 — IFileStorage ✅

- `MoveFile(from, to)`, `MoveDirectory(from, to)` в интерфейсе;
  `FileStorage` — `File.Move`/`Directory.Move`; `InMemoryFileStorage` — перенос ключей.
- Тесты: `InMemoryFileStorageTests` + `FileStorageMoveTests` (реальный диск, temp-папка).

## Шаг 3 — Сервисы (`Mars.Host`) ✅

- `IMediaFolderService`/`MediaFolderService` (DI рядом с `IMediaService`):
  - список папок по родителю (+ цепочка предков для хлебных крошек);
  - `Create(parentId?, name)` — запись в БД + `CreateDirectory`; валидация имени
    (без `/`, `\`, `..`; уникальность в пределах родителя);
  - `Rename(id, newName)` — `MoveDirectory` + транзакция: `path` папки и всех вложенных,
    `file_physical_path`/`file_virtual_path` файлов, пути миниатюр в `Meta.Thumbnails`
    (и перенос физических файлов миниатюр);
  - `Delete(id)` — только пустая (нет файлов/подпапок в БД и каталог пуст на диске);
  - `MoveFiles(fileIds, folderId?)` — перенос файлов + их миниатюр + обновление путей и `FolderId`.
- `FileService.List/ListTable` — фильтр по `FolderId` (null = корень).
- `MediaService.WriteUploadToMedia` — опциональный `folderId`; корень — как сейчас `Media/{год}`
  с привязкой к папке года (find-or-create).
- `ScanFiles` — дополнительно регистрирует физические каталоги как папки и привязывает файлы.

## Шаг 4 — API (`MediaController`) ✅

- `GET api/Media/folders?parentId=` (+ предки для крошек), `POST api/Media/folders`,
  `PUT api/Media/folders/{id}/rename`, `DELETE api/Media/folders/{id}`,
  `POST api/Media/move-files` `{ ids, folderId? }`.
- `folderId` в `Upload` и в `list/page` / `list/offset`.
- Контракты `Mars.Shared.Contracts.Files`: `FolderResponse`, `CreateFolderRequest`,
  `RenameFolderRequest`, `MoveFilesRequest`; `FolderId` в query/response файлов.

## Шаг 5 — Клиент и фронт ✅

- `MediaServiceClient` (`Mars.WebApiClient`) — новые методы; `IAppMediaService`/`AppMediaService`.
- `FluentMediaFilesList`: текущая папка, плитки папок над сеткой, хлебные крошки,
  «Новая папка», меню папки (переименовать/удалить), «Переместить…» у файлов
  (диалог выбора папки), загрузка в открытую папку. Работает и в пикере `ModalMediaSelect`.
- Стили: правки только в `.less` AppAdmin (`MediaTable.less` и соседние), компиляция на пользователе;
  после — bump версии ассетов.

## Шаг 6 — Тесты и проверка ✅

Выполнено 2026-08-21: интеграционные `MediaFolderTests` (8 шт: создание/список, загрузка в папку,
фильтр списка, rename с переписью путей, перенос файла, удаление пустой/непустой) — зелёные;
регрессия `Controllers.Medias` + `HttpInFormSaveFilesNodeTests` (21/21) и `Test.Mars.Host` (315/315).

- Unit (`tests/Test.Mars.Host/Files`): CRUD папок на `InMemoryFileStorage` + NSubstitute-репозиторий,
  rename (перепись путей вложенных папок/файлов/миниатюр), перенос файлов.
- Интеграционные (`tests/Mars.Integration.Tests/Controllers/Medias`): эндпоинты папок,
  загрузка в папку, фильтр списка по папке, удаление пустой папки.
- Проверка точечно: сборка + тесты затронутых проектов (не весь сьют).

## Не в v1 (запланировано на потом)

- Права доступа на папки (кому отображается), значки папок в UI (поле `Icon` уже есть).
- Перенос папки в другую папку; drag-and-drop в UI.
- Глобальный поиск файлов по всем папкам (сейчас поиск — в текущей папке).
- S3-реализация `IFileStorage` (интерфейс готов: Move = copy+delete по префиксу).
- Устранение прямых обращений к диску мимо `IFileStorage` (`FindAllFiles`, `ImageProcessor`,
  авто-ресайз в `WriteUpload`) — долг для S3.
