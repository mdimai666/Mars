# План: рефакторинг IFileStorage и уход от FileStream в медиа

> **Статус: блоки 1–4 выполнены 2026-09-09.** Осталось: решение по мусору в `wwwroot/upload`
> и по предсуществующему падению `MarketplaceTests` (не связано с этой задачей).
> Задача-источник: запрос пользователя «починить долг IFileStorage и подготовиться к S3» (2026-09-07).
> Предшественник: `ai/MediaFilesPlan.md`, раздел «Не в v1» — долг «устранение прямых обращений к диску
> мимо `IFileStorage` (`FindAllFiles`, `ImageProcessor`, авто-ресайз в `WriteUpload`)» взят оттуда.
> Объём сужен пользователем 2026-09-09: S3-провайдер не пишем, async-миграцию не делаем,
> плагин-стек не переписываем, модель отдачи файлов не меняем.

## Принятые решения (2026-09-07 … 2026-09-09)

1. **S3 сейчас не внедряем — только готовность.** После рефакторинга в медиа-пайплайне не остаётся
   ни абсолютных путей, ни `FileStream`, поэтому будущий объектный провайдер ложится без переделки медиа.
2. **Имя и пакет не меняем**: `IFileStorage` остаётся в `Mars.Server.Abstractions`, второй интерфейс
   не заводим, реализация живёт в `Mars.Storage`.
3. **Форма — по образцу `IFileStore` из Orchard Core** (проверено по
   `src/OrchardCore/OrchardCore.FileStorage.Abstractions/IFileStore.cs` и провайдерам
   `FileStorage.FileSystem` / `.AmazonS3` / `.AzureBlob`): берём принципы — примитивы в интерфейсе,
   сахар снаружи, идемпотентность вместо пар `Delete`/`DeleteIfExist`, никаких физических путей
   в медиа-коде. Async и `IFileStoreCapabilities` (`HasHierarchicalNamespace`, `SupportsAtomicMove`)
   не добавляем — они нужны только при реальном S3.
4. **Сахар — extension-методы, а не default interface methods.** `IFileStorage` подменяется через
   NSubstitute (`tests/Mars.Nodes.Tests/Services/NodeServiceUnitTestBase.cs:99`), а прокси перехватывает
   члены с дефолтной реализацией — тело DIM не выполняется, получается молчаливый `null`.
5. **data-корень (плагины, flows, JWT-ключи) не трогаем.** `PhysicalPath` там легален: корень дисковый,
   а `AssemblyLoadContext.LoadFromAssemblyPath` (`PluginLoadContext.cs:42`), `AssemblyDependencyResolver`
   и `wwwroot`-композитинг плагинов без реального пути не работают. Локальный путь из абстракции наружу
   не отдаём — так же делают другие (Orchard не может отдать вовсе, Umbraco разворачивает направление:
   `CanAddPhysical` + `AddFile(path, physicalPath)`).
6. **`InMemoryFileStorage` выравниваем с дисковой семантикой до переноса тестов** — иначе тесты на
   in-memory будут врать (сейчас расхождения в `GetDirectoryContents`, `DeleteDirectory`,
   `CreateDirectory` и в доступности потока на запись).
7. **Габариты изображения берём из результата обработки, а не из запрошенных настроек** (см. блок 3, п. 6).

## Как сейчас (as-is)

- `IFileStorage` — 17 членов, почти всё синхронное (async только `WriteAsync`); реализации
  `FileStorage` (диск, корень из `IOptions<FileHostingInfo>`) и `InMemoryFileStorage` (тесты).
- Два корня (`src/Server/Mars.Server/MainServer.cs:153-168`): default = `{ContentRoot}/wwwroot/upload`
  (раздаётся `UseStaticFiles` как `/upload/...`), keyed `"data"` = `{ContentRoot}/data`.
  В тестах на `InMemoryFileStorage` заменён только keyed `"data"` (`ApplicationFixture.cs:117`).
- Медиа-пайплайн обходит хранилище в трёх местах:
  авто-ресайз `FileService.WriteUpload:265-272` (`new FileStream(fileAbsolutePath, FileMode.CreateNew)`),
  миниатюры `FileService.GenerateThumbnailsAndGetFileMeta:361,390-392` и
  `MediaService.GenerateThumbnails:224-232` (`ProcessImage(string abs, string abs, cfg)`),
  сканирование диска `MediaService.FindAllFiles:281-316` (`Directory.GetFiles/GetDirectories`).
  В двух местах стоят маркеры `//TODO: заменить на _fileStorage` (`FileService.cs:267`, `:391`).
- Следствие: default-корень нельзя заменить на `InMemoryFileStorage` (комментарий
  `ApplicationFixture.cs:116` неточен — дело не в `StreamWriter`, а в байпасах выше), медиа-тесты
  пишут в реальный `wwwroot/upload` контент-рута и оставляют мусор; в `UploadMediaTests:209-213` —
  ручной «костыль» удаления 205-МБ файла.
- `IProcessImageResult.FileSize` и `ProcessingTime` не читаются нигде (проверено grep'ом),
  `_result()` всегда кладёт `FileSize = 0`.
- Перегрузка `IImageProcessor.ProcessImage(ReadOnlySpan<byte>, Stream, cfg)` не используется нигде.

---

## Блок 1 — чистка интерфейса (17 членов → 12 примитивов + 4 extension) ✅

Выполнено 2026-09-09. Итоговые имена: `OpenRead`, `Write(path, Stream)`, `WriteAsync`, `FileExists`,
`DeleteFile`, `GetDirectoryContents`, `GetFileInfo`, `CreateDirectory`, `DirectoryExists`,
`DeleteDirectory`, `MoveFile`, `MoveDirectory`; сахар — `FileStorageExtensions`
(`ReadAllText`, `ReadAllBytes`, `Write(path, string)`, `Write(path, byte[])`).
Побочно: в `FaviconGeneratorHandler` поток из хранилища теперь освобождается (`using`),
в `FileService.DeletePhysicalFile` убран избыточный `FileExists`-guard, удалён мёртвый
закомментированный `ReadAsStream` в `FileServiceReadNodeImpl`.
Проверка: сборка чистая; `Mars.Server.Tests` (Files) 55/55, `Mars.Nodes.Tests` 418/418,
`Mars.Plugin.Tests` 49/49 (+1 пропущенный и раньше).

| Было | Стало | Зачем |
|---|---|---|
| `void Read(path, out Stream)` | `Stream OpenRead(path)` | out-параметр не асинхронизируется в принципе |
| `void Delete(path)` + `bool DeleteIfExist(path)` | `bool Delete(path)` — идемпотентный, возвращает факт удаления | `Delete` и так проверял `Exists`; дубль |
| `ReadAllText`, `Read→byte[]`, `Write(string)`, `Write(byte[])` — по отдельной реализации в каждом классе | `FileStorageExtensions` над `OpenRead`/`Write(path, Stream)` | именно здесь реализации расходятся; провайдер обязан уметь только примитивы |
| `IFileInfo FileInfo(path)` + `return null!` | `IFileInfo? FileInfo(path)` | `FileService:281` делает `fi.Length` без проверки — NRE на отсутствующем файле |
| `throw new Exception("path must be relative")` (`FileStorage:165`) | `ArgumentException` | — |

Не добавляем: `Copy`, `OpenWrite`, async-двойники, флаг `overwrite`. Запись результата обработки
покрывается `Write(path, MemoryStream)` — это же единственная форма, которую объектное хранилище
умеет нативно (`PutObject` из потока).

Call sites `OpenRead` (4): `PluginRegistry.cs:88`, `MediaService.cs:191`, `FaviconGeneratorHandler.cs:92`,
`FileServiceReadNodeImpl.cs:105`. Call sites `DeleteIfExist` (2): `FileService.cs:96`, `:328`.

## Блок 2 — выравнивание `InMemoryFileStorage` с дисковой семантикой ✅

Выполнено 2026-09-09: листинг отдает только непосредственных детей (сравнением родителя пути,
а не префиксом — иначе `Media` матчил `MediaThumbs/*`), `DeleteDirectory` работает по префиксу
`path + '/'` и бросает `IOException` на непустом каталоге при `recursive: false` (как
`Directory.Delete`), `CreateDirectory`/`Write`/`MoveFile` регистрируют все родительские сегменты,
`OpenRead` отдает поток только на чтение, `Exists` у `FileSystemFileInfo` смотрит в живое состояние,
внутри `ConcurrentDictionary` + lock на составных операциях.
Добавлено 9 тестов в `InMemoryFileStorageTests`; попутно починен `FileServiceTests.WriteFile_*` —
он был написан под старое рекурсивное поведение `GetDirectoryContents("")`.
Проверка: `Mars.Server.Tests` (Files) 64/64, `Mars.Nodes.Tests` 418/418, `Mars.Plugin.Tests` 49/49.

| Метод | Диск (`FileStorage`) | InMemory сейчас | Чиним |
|---|---|---|---|
| `GetDirectoryContents` | только непосредственные дети (`Directory.EnumerateFiles`) | `dir.StartsWith(subpath)` → все потомки, `Media` матчит `MediaThumbs/*` | непосредственные дети (контракт `IFileProvider`) |
| `DeleteDirectory` | `Directory.Delete(path, recursive)` | `key.StartsWith(path)` без разделителя → `DeleteDirectory("Media")` сносит `MediaThumbs/*`; при `recursive: false` непустая папка удаляется молча | префикс `path + "/"`; `IOException` на непустой при `recursive: false` |
| `CreateDirectory` | создаёт промежуточные (`mkdir -p`) | добавляет только точный путь → `DirectoryExists` родителя расходится | создавать все сегменты |
| `Read`/`OpenRead` | `File.OpenRead` — только чтение | `new MemoryStream(bytes)` — записываемый поток поверх внутреннего массива | `new MemoryStream(bytes, writable: false)` |

Плюс: `FileSystemFileInfo.Exists => true` всегда и `Length => 0` для отсутствующего файла;
потокобезопасность (синглтон шарится между параллельными тест-коллекциями, внутри голые
`Dictionary`/`HashSet`).

## Блок 3 — медиа без `FileStream` и абсолютных путей ✅

Выполнено 2026-09-09. Все четыре точки байпаса закрыты, оба `//TODO: заменить на _fileStorage`
удалены. Авто-ресайз пишет через `MemoryStream` → `_fileStorage.Write`, размер берётся из
`resizedStream.Length` (вызов `GetFileInfo` в этой ветке больше не нужен), габариты — через
`ImageSize(resizedStream)` по готовым байтам, а не из `result.Settings`; в `UploadMediaTests`
добавлен assertion «габариты в мете == реальные габариты записанного файла».
Миниатюры в обоих сервисах: один `OpenRead` источника вне цикла + `Position = 0` перед каждой
конфигурацией (паттерн `FaviconGeneratorHandler`). `FindAllFiles`/`ScanFiles` схлопнуты в один
приватный обход `GetDirectoryContents`, путь ребёнка собирается как `dir + '/' + entry.Name` —
`PhysicalPath` не используется. `IFileRepository.ListAllAbsolutePaths` → `ListAllRelativePaths`
(проекция `FilePhysicalPath` в SQL, без вычисления абсолютных путей).
Из `IImageProcessor` удалён `ProcessImage(string, string, cfg)`, остальные перегрузки стали `void`,
типы `IProcessImageResult`/`ProcessImageResult` удалены из `Mars.Media.Contracts` (никто не читал).
Проверка: сборка чистая; интеграционные медиа-тесты 21/21 на реальном диске и реальном MagicScaler.

1. **`FileService.WriteUpload:265-272`** (авто-ресайз): `ProcessImage(fileStream, ms, cfg)` в
   `MemoryStream` → `_fileStorage.Write(path, ms)`; размер из `ms.Length` — уходит вызов `FileInfo`
   и риск NRE. Буферизация безопасна: выход ограничен конфигом ресайза (`1200×1200 Max`),
   а крупные не-картинки идут `Write(path, stream)` сквозняком, как сейчас.
2. **`FileService.GenerateThumbnailsAndGetFileMeta:361,390-392`**: один `OpenRead(filePathFromUpload)`
   вне цикла + `Position = 0` перед каждой миниатюрой → `ProcessImage(Stream, MemoryStream)` →
   `Write(thumbPath, ms)`. Тот же паттерн уже работает в `FaviconGeneratorHandler:110`.
3. **`MediaService.GenerateThumbnails:224-232`**: то же.
4. **`MediaService.FindAllFiles:281-316`**: рекурсивный обход через `GetDirectoryContents` вместо
   `Directory.GetFiles/GetDirectories`. Следствие: `FileRepository.ListAllAbsolutePaths:243-258`
   перестаёт вычислять абсолютные пути — сверка идёт по относительным ключам (метод переименовать).
5. **`IImageProcessor`**: удалить `ProcessImage(string, string, cfg)` — единственный поставщик
   абсолютных путей. Из `IProcessImageResult` удалить `FileSize` и `ProcessingTime` (никто не читает).
   Перегрузку `ReadOnlySpan<byte>` оставить.
6. **Габариты изображения — проверить и исправить.** `_result()` возвращает `result.Settings.Width/Height`,
   то есть запрошенный размер, а авто-ресайз настроен как `1200×1200 Max`. Если MagicScaler не
   корректирует `Settings` под фактический вывод, для картинки 400×300 в `meta.ImageInfo` уезжает
   1200×1200. Существующий тест этого не ловит (`Width.Should().NotBe(0)`). Решение: брать габариты
   из готовых байт (`ImageSize(ms)`, поток seekable) и добавить assertion на реальные размеры.
   **Итог проверки (по исходникам MagicScaler):** бага, скорее всего, не было — `WriteOutput` в
   `src/MagicScaler/Magic/MagicImageProcessor.cs` конструирует `new ProcessImageResult(ctx.UsedSettings, ctx.Stats)`,
   т.е. отдаёт настройки после `ctx.FinalizeSettings()`, а не сырой экземпляр вызывающего; для `Max`
   пайплайн пересчитывает целевой размер сам. Правку всё равно оставили: она снимает зависимость от
   внутренностей библиотеки, а assertion работает как регрессионная защита.

## Блок 4 — тесты на InMemory ✅

Выполнено 2026-09-09. Default-корень в `ApplicationFixture` заменён на `InMemoryFileStorage`,
неточный комментарий про `StreamWriter` удалён. Assertions в `UploadMediaTests`/`DeleteMediaTests`
переведены с `File.Exists`/`File.ReadAllText`/`new FileInfo(...).Length` на `_fileStorage.*`;
«костыль» очистки 205-МБ файла оставлен как обычная гигиена (освобождает память), но без TODO.
В `FileFixtureCustomizeExtension` удалена мёртвая переменная `thumbFilepathAbsolutePath`.

**Грабля:** `HttpInFormSaveFilesNodeTests.Execute_IFileStorage_SavesFile` писал через `IFileStorage`,
но проверял `File.Exists` на диске — и проходил только потому, что файл `media/{год}/q/filefield/file1.txt`
оставался от прежних дисковых прогонов. Проявилось после очистки мусора. Переведён на
`fileStorage.FileExists(result[0])`. Вывод: «зелёный» тест с проверкой диска может быть зелёным
за счёт старого мусора, поэтому чистку и перевод на InMemory надо делать вместе.

**`E2EServerFixture` (tests/Mars.E2E.Tests) сознательно оставлен на диске**: E2E-тесты водят
браузер против живого приложения и медиа там отдаётся статикой по HTTP — с in-memory хранилищем
файлы были бы недоступны.

Итог полного прогона `tests/Mars.Integration.Tests`: **375 passed / 0 failed / 4 skipped**.
По пути починено не связанное с хранилищем падение `MarketplaceTests.Status_CatalogDisabled_ReturnsDisabled`:
тест исходит из выключенного каталога, а коммит `725b761a` (2026-09-07) включил в
`src/Mars.WebApp/appsettings.json` `"PluginCatalog": { "Enabled": true, "Url": "https://catalog.mdimai666.ru" }`,
который тестовый хост и грузит (content root = `src/Mars.WebApp`). По решению пользователя каталог
выключен в конфигурации фикстуры (`PluginCatalog:Enabled = false` в in-memory-наборе рядом с
`ConnectionStrings:DefaultConnection`) — тот же механизм переопределения appsettings.
`appsettings.Test.json` не создавали.

- `ApplicationFixture.cs:115-117`: включить `Replace(Singleton<IFileStorage, InMemoryFileStorage>)`
  для default-корня, удалить неточный комментарий. Проверить то же в `E2EServerFixture.cs:83`.
- Переписать assertions с диска на хранилище: `File.Exists(FileAbsolutePath(...))` →
  `_fileStorage.FileExists(...)`, `File.ReadAllText` → `_fileStorage.ReadAllText(...)`,
  `new FileInfo(...).Length` → `_fileStorage.FileInfo(...).Length`.
  Файлы: `UploadMediaTests:86-89, 122-127, 130-140`, `DeleteMediaTests:45-100`, `MediaFolderTests:29-222`.
- Убрать «костыль» в `UploadMediaTests:209-213` — 205-МБ тест больше ничего не пишет на диск.
- Добавить в `tests/Mars.Server.Tests/Files/` тесты на выровненную семантику из блока 2.
- Мусор, оставленный тестами в `src/Mars.WebApp/wwwroot/upload`, удалять только по явному
  подтверждению пользователя.

## Верификация (точечная, по конвенции)

```
dotnet build Mars.slnx
dotnet test tests/Mars.Integration.Tests --filter "FullyQualifiedName~Medias|FullyQualifiedName~MediaFolder|FullyQualifiedName~HttpInFormSaveFilesNode"
dotnet test tests/Mars.Server.Tests --filter "FullyQualifiedName~Files"
dotnet test tests/Mars.Nodes.Tests
```

Фактические прогоны (2026-09-09): сборка `Mars.slnx` — 0 ошибок; `Mars.Server.Tests` (Files) 64/64;
`Mars.Nodes.Tests` 418/418 (там `Substitute.For<IFileStorage>()` — перенос сахара в extension-методы
не сломал); `Mars.Plugin.Tests` 49/49 + 1 пропущенный и раньше; `Mars.Integration.Tests` полностью
**375/0/4** (полный прогон понадобился, потому что замена хранилища в `ApplicationFixture` затрагивает
все тесты проекта, а не только медиа; Docker для Testcontainers PostgreSQL был доступен).

## Мусор в `src/Mars.WebApp/wwwroot/upload` — убран ✅

Прежние прогоны медиа-тестов писали в реальный `wwwroot/upload` контент-рута. 2026-09-09 по команде
пользователя удалены тестовые артефакты: **584 файла / 16,33 МБ** (354 `.webp`-миниатюры, 115 `.txt`,
115 `.jpg`) — семейства имён из тестов: `file1*` (`UploadMediaTests`, `DeleteMediaTests`,
`MediaFolderTests`, `HttpInNode*Tests`), `text1*` (`Mars.WebApiClient.Integration.Tests`),
`creation_of_space1*` (пример картинки + её миниатюры), каталог `q/filefield`
(`HttpInFormSaveFilesNodeTests`). Каталог остался: 2660 файлов / 142,3 МБ — медиа рабочего
dev-инстанса, не тронуты. Каталоги не удалялись (могут быть зарегистрированы как папки в БД инстанса).
Новые прогоны медиа-тестов диск больше не трогают.

## Не в этой задаче (бэклог)

- S3-провайдер (`Mars.Storage.S3`, AWSSDK.S3 + `ServiceURL`/`ForcePathStyle` для MinIO-совместимых),
  async-миграция интерфейса, `IFileStoreCapabilities`.
- Async-миграция упирается в синхронные конструкторы `KeyMaterialService`, `PluginManager`,
  `PluginRegistry`, `NodeService` и в pre-Build pipeline плагинов (до `builder.Build()`).
- Перевод плагин-стека на встроенный `IFileProvider` (он только для чтения — установка плагина
  пишет, поэтому полной заменой не является).
- Смена модели отдачи файлов. Справочно: Orchard Core отдаёт медиа самим приложением
  («Media is still served by the Orchard Core website… The URL generated by the AssetUrl helpers
  points to the Orchard Core website»), отдельной фичей кладёт кэш ресайзнутых картинок в другой бакет.
- `PostController.Upload:266` всегда падает: передаёт `userId = Guid.Empty`, а `WriteUpload` бросает
  `ArgumentException` (`FileService:234`).
- `NotifyService.cs:29` принимает `IFileStorage` и не использует его; шаблон письма читается сырым
  `File.ReadAllText("Res/mail_templates/template2.html")` относительно CWD процесса — мимо любого корня.
- `FileUrl` миниатюр запекается в jsonb при записи (`FileService:414-429`) — при смене схемы отдачи
  станет мёртвым грузом (Orchard хранит только path и считает URL на отдачу: `MapPathToPublicUrl`).
- Стоимость S3 (справка от 2026-09-07, тарифы Cloudflare R2 дословно с их страницы): хранение
  $0.015/ГБ-мес, Class A $4.50/млн, Class B $0.36/млн, **исходящий трафик $0**. Вывод: запросы —
  копейки, деньги в egress, а egress определяется моделью отдачи. MinIO на своём VPS — ноль стоимости
  запросов.
