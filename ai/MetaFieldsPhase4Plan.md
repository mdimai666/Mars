# План мета-полей — Фаза 4: контент как фича-поле, вторая волна редакторов

> **Статус: Фаза 4 выполнена 2026-08-24 (ред. 2 — маркер автора значения
> отклонён, вместо него `PostTypeEntity.Options`).**
> Ветка `ai/metafields-rework`. Продолжение `ai/MetaFieldsPhase3Plan.md`
> (Фазы 1–3 выполнены 2026-08-23/24) и `ai/MetaFieldsPhase2Plan.md`
> (Фазы А–Д выполнены).
> **Итог реализации**: миграция `20260823215434_PostContentFeatureField`
> (data-перенос редактора/codeLang в поле `content`, ключи первой волны →
> `core.input.*`, колонка `post_types.options`, снос `post_content_type`);
> ключи редакторов `core.wysiwyg.quilljs`/`core.code.monaco`/
> `core.blockeditor.editorjs`; `PostContentSettings` снесён полностью
> (включая исторические миграции — тип заменён на string); обёртки редакторов
> `MetaValueWysiwygEditor`/`MetaValueCodeEditor`/`MetaValueBlockEditor`
> (WYSIWYG/Code коммитятся периодическим опросом — у Blazored.TextEditor и
> штатного пути Monaco нет события изменения); пресеты «Текст (WYSIWYG)»,
> «Текст (Editor.js)», «Код»; защита поля контента в `FormMetaField`
> (бейдж, без удаления, лок типа и ключа). Точечные тесты зелёные: юнит
> `Test.Mars.Host` 388/388 (+8 новых), интеграционные типы/трансформер/нода/
> вью-сервис 18/18, `WebApiClient` типы 18/18 и посты/PostJson зелёные.

## Контекст

На выходе Фазы 3 имеем:
- реестр редакторов первой волны: `MetaFieldEditorCatalog` (color/url/email/
  date/time/datetime) + фронт-локатор `MetaFieldEditorLocator` («ключ →
  компонент + совместимые типы»), рендер через `RowMetaValue`/`DynamicComponent`,
  контракт редактора — `MetaValueEditModel` + `ValueChanged`; выбор редактора в
  настройках поля (`Options.editor`); пресеты пикера `MetaFieldTypePresets`;
- механизм «фича → поле» после редизайна PostImage: `FeatureFieldsCatalog`
  (маркер `Options.featureKey`), свободная тулза
  `PostTypeFeatureFields.ApplyFeaturePostImage` (пайплайн не использует),
  валидаторы (фича включена → указатель валиден), поле создаёт клиент;
- контент: owned-jsonb `post_content_type` на `post_types` (`PostContentType` +
  `CodeLang`); 4 редактора хардкодом в `EditPostView` (WysiwygEditor/CodeEditor2/
  BlockEditor1/textarea), значение — отдельным `post.Content` в транспорте;
  рендер шаблонов — `PostTransformer` → `IPostContentProcessorsLocator`
  (keyed-DI по типу контента; единственный процессор —
  `BlockEditor1PostContentProcessor`, ключ `"BlockEditor"`); `CodeLang` реально
  нигде не читался (админка хардкодила handlebars);
- компоненты тяжёлых редакторов: `WysiwygEditor` (Blazored.TextEditor/Quill) уже
  в AppFront.Main; `CodeEditor2` (Monaco) — AppFront.Main уже ссылается на
  `MarsCodeEditor2`; `BlockEditor1` (Editor.js, `EditorJsBlazored`) — ссылки из
  AppFront.Main нет; `IAppMediaService` — в AppFront.Main.

Пробелы: контент существует вне реестра редакторов; смена типа контента молча
ломает старые значения; плагины не могут подключить свои редакторы (ключи
однословные, без пространства имён).

## Принятые решения (2026-08-24)

1. **Поле контента — фиксированный ключ `content`, без указателя-колонки**
   (в отличие от картинки: там указатель, потому что картинкой может быть любое
   Image-поле; контент — всегда своё поле). Поле фичи `Features.Content` с
   маркером `Options.featureKey = "content"`; сервер находит его по ключу.
   Включение фичи в админке: поля нет → создаётся сразу, без диалога
   (кандидатов нет и быть не может). Валидаторы (оба, Create/Update): фича
   включена → поле `content` существует и имеет тип String/Text. Защита поля
   (бейдж «фича», запрет удаления, лок пикера типа) считается из
   «ключ == content && фича включена»; ключ `content` запрещён к переименованию.
   Свободная тулза `PostTypeFeatureFields.ApplyFeatureContent(fields, enable)` —
   зеркало PostImage (для сервисного кода/тестов/миграций):
   `enable` и поля нет → создаёт (Type=Text, маркер, редактор по умолчанию);
   `!enable` → поля не трогает.
2. **Транспорт значения контента не меняется**: контент ходит отдельным
   `post.Content` (админка и JSON-API); поле `content` — только определение
   редактора. Из потока мета-значений поле исключается как Query: формы
   (`FormMetaValueItems`), обогащение пустых значений (`PostService`,
   `PostCategoryService` не затрагивается — контент только у постов),
   `MetaValuesValidator`, `MetaValuesGeneratorService`, грид-колонки
   (`PostMetaColumnsService` — значения всё равно не в `meta_values`),
   `PostJsonService` (чтение/запись). Секция контента в `EditPostView` остаётся
   на своём месте; редактор выбирается по `Options.editor` поля.
3. **Ключи редакторов — трёхчастная схема `<происхождение>.<семейство>.<реализация>`**:
   встроенные — `core.*`, плагины в будущем — `plugin.*`.
   - Вторая волна: WYSIWYG → `core.wysiwyg.quilljs` (Quill/Blazored.TextEditor),
     Код → `core.code.monaco` (CodeEditor2), Блочный → `core.blockeditor.editorjs`
     (Editor.js). Совместимые типы полей — String/Text.
   - Первая волна переименовывается (data-миграция по `meta_fields.options`):
     color → `core.input.color`, url → `core.input.url`, email → `core.input.email`,
     date → `core.input.date`, time → `core.input.time`, datetime → `core.input.datetime`.
   - Обычный текст — «без редактора» (дефолтный редактор типа), ключа нет.
   - `PostTypeConstants.DefaultPostContentTypes` удаляется целиком; константы
     переезжают в `MetaFieldEditorCatalog` с новыми значениями.
   - Процессоры `IPostContentProcessor` (keyed-DI) регистрируются под новыми
     ключами; `PostTransformer` берёт ключ напрямую — маппинг не нужен.
4. **Маркер автора значения — НЕ делаем** (обсуждение 2026-08-24: пер-постовое
   хранение отклонено; общих настроек на посту нет). Формат значения контента
   всегда выводится из текущего редактора типа (`Options.editor` поля `content`) —
   поведение как сегодня; дыра при смене редактора типа принимается и при
   потребности закрывается позже.
   Вместо него: **`PostTypeEntity.Options`** (nullable jsonb) — общая точка
   расширения для настроек уровня типа (в будущем — настройки заголовка, тегов
   и т.п.); поведение как у `Options` мета-полей. Заводится в миграции этой фазы
   вместе с пустым каталогом `PostTypeOptionsCatalog` (Mars.Shared: конвенция
   ключей + типизированные ридеры по мере появления ключей). Пер-постовые вещи —
   мета-поля, не опции.
5. **Язык кода** — типизированный `Options.codeLang`
   (`MetaFieldEditorCatalog.CodeLangOption()/GetCodeLang()`): селектор языков в
   настройках поля (список `CodeEditor2.Language.Array` доступен — ссылка есть)
   и стартовый язык редактора в форме поста (вместо хардкода handlebars).
   Дефолт — `handlebars` (`MetaFieldEditorCatalog.DefaultCodeLang`).
6. **Пресеты пикера**: «Текст (Editor.js)», «Текст (WYSIWYG)», «Код» — все тип
   Text; «Код» сразу с `codeLang = handlebars`. `Preset` получает `CodeLang`,
   применение в `FormMetaField`.
7. **`PostContentSettings` сносится полностью** (ломающе, альфа): owned-тип и
   колонка `post_content_type` (data-миграция), свойство сущности, вся DTO-цепочка
   (`PostContentSettingsDto`, `Create/UpdatePostContentSettingsRequest`,
   `PostContentSettingsResponse`, поля `PostTypeRequest`/`PostTypeResponse`,
   маппинги), UI-карточка на странице типа, `PostContentSettingsEditModel`.
8. **Конвенции**: миграция одна (схема + data; data — raw SQL по jsonb); новых
   сущностей/FK нет; миграции `PluginExample` не синхронизируются (как в
   Фазах 1/3 — снапшот плагина уже отстаёт от модели).

## Шаги

### Шаг 1 — каталоги и константы (Mars.Shared)

- `MetaFieldEditorCatalog`: ключи+заголовки `Wysiwyg`/`Code`/`BlockEditor`
  в `All`; ключи первой волны → `core.input.*`; `CodeLangOption()/GetCodeLang()`;
  `DefaultCodeLang = "handlebars"`.
- `FeatureFieldsCatalog`: `Content = "content"`, `ContentFieldKey = "content"`,
  заголовок поля; `GetFeatureKeyFor`/`GetFeatureName` для Content.
- `PostTypeConstants.DefaultPostContentTypes` — удалить, все использования
  перевести на новые ключи.

### Шаг 2 — сервер: фича-поле, валидаторы, хелперы (Mars.Host.Shared)

- `PostTypeFeatureFields.ApplyFeatureContent(fields, enable)`.
- `CreatePostTypeQueryValidator`/`UpdatePostTypeQueryValidator`: фича включена →
  поле `content` существует и является String/Text.
- Хелперы поля контента: `PostTypeDetail.ContentField()`/`ContentEditorKey()`
  (Mars.Host.Shared — для `PostTransformer`, процессоров, серверных сервисов);
  расширение на `PostTypeDetailResponse` (Mars.Shared — для админки и AiChat).

### Шаг 3 — общие настройки типа и рендер контента

- `PostTypeEntity.Options` (nullable jsonb, «точка расширения») + колонка в
  миграции; пустой каталог `PostTypeOptionsCatalog` (Mars.Shared) — конвенция
  ключей, типизированные ридеры добавляются с первым ключом.
- `PostTransformer`: процессор резолвится по `ContentEditorKey()` типа
  (редактор поля `content`; пусто = обычный текст, рендера нет) — вместо
  `PostContentSettings.PostContentType`.
- `BlockEditor1PostContentProcessor`: ключ `KeyredHandler` →
  `core.blockeditor.editorjs`; guard — по `ContentEditorKey()` типа.

### Шаг 4 — миграция и снос `PostContentSettings`

- Одна миграция (схема + data):
  1. `meta_fields.options->>'editor'`: ключи первой волны → `core.input.*`;
  2. типы с включённой фичей Content: поле `content` — INSERT (Type=Text,
     маркер `featureKey`, редактор из `post_content_type`: WYSIWYG/Code/
     BlockEditor → новые ключи, PlainText → без редактора; для Code —
     `codeLang` = CodeLang ?? handlebars); если поле с ключом `content` уже
     есть — используем его (дописываем редактор/маркер в его Options);
  3. `ALTER TABLE post_types ADD options` (jsonb, null) — точка расширения;
  4. `DROP COLUMN post_content_type` на `post_types`.
- Класс `PostContentSettings` и свойство `PostTypeEntity.PostContentType` —
  удалить. Единственная компиляционная ссылка в исторических миграциях —
  `20250614175650_ReplaceJsonbToAddToJson.cs` (typeof/AlterColumn<T>, 2 места):
  поправить на строковый эквивалент (снапшоты/дизайнеры строковые, компилируются).
- DTO-цепочка: `PostContentSettingsDto`, `Create/UpdatePostContentSettingsRequest`,
  `PostContentSettingsResponse` — удалить; `PostTypeRequest`/`PostTypeResponse` —
  без `PostContentSettings`; маппинги `PostTypeMapping` (Mars.Host.Shared и
  Mars.Host.Repositories), `PostTypeRequestExtensions` — пересобрать.
- Исключение поля контента из мета-потоков: `PostService.EnrichWithBlank…`,
  `MetaValuesValidator`, `MetaValuesGeneratorService`, `PostMetaColumnsService`,
  `PostJsonService` (условие: фича включена && ключ поля == `content`).
- Потребители: `MarsPostTools` (Mars.AiChat.Host), `SeedPostData` (фабрика
  сидов), фикстуры `Mars.Test.Common` (`RequestCustomize`, `EntitiesCustomize`),
  xmldoc `IPostContentProcessorsLocator`, `ai/AiChatGuide.md`.

### Шаг 5 — фронт: вторая волна редакторов (AppFront.Main)

- Реестр `MetaFieldEditorLocator` += 3 записи (совместимые типы String/Text):
  обёртки `MetaValueWysiwygEditor`, `MetaValueCodeEditor`,
  `MetaValueBlockEditor` над существующими компонентами (контракт
  `MetaValueEditModel` + `ValueChanged`; значение — StringText/StringShort по
  типу поля; у Code — селектор языка из `Options.codeLang`; у BlockEditor —
  запрос картинок через `IAppMediaService`).
- ProjectReference `AppFront.Main → EditorJsBlazored`.
- `FormMetaField`: селектор языка при `editor == core.code.monaco`.
- Тяжёлые редакторы инициализируются как есть (ленивость — после редизайна форм).

### Шаг 6 — админка (AppAdmin)

- Страница типа: карточку `PostContentSettings` и `PostContentSettingsEditModel`
  удалить; `ToggleFeature(Content, on)` → поле `content` создаётся сразу
  (`PostTypeEditModel.CreateFeatureContentField` — зеркало серверной тулзы,
  редактор по умолчанию `core.blockeditor.editorjs`), диалог не нужен.
- Форма поля: защита поля контента («ключ == content && фича включена»): бейдж
  «фича», без кнопки удаления, пикер типа заблокирован; ключ `content` —
  заблокирован к переименованию.
- Форма поста (`EditPostView`): секция контента остаётся, редактор — по
  `Options.editor` поля контента (ветки на новые ключи; обычный текст —
  textarea); у Code стартовый язык из `codeLang`; `BeforeSave` и ИИ-обработчик
  (`GetInfo`/`GetFields`/`SetContentValue`/`ExtractPlainText`) — на новых ключах.

### Шаг 7 — пресеты пикера (AppFront.Main)

- `MetaFieldTypePresets.All` += «Текст (Editor.js)», «Текст (WYSIWYG)», «Код»
  (тип Text, у «Кода» `codeLang = handlebars`); `Preset` += `CodeLang`,
  применение пресета в `FormMetaField` пишет его в Options.

## Верификация (правило: точечно, не весь сьют)

- сборка `Mars.slnx`;
- юнит `Test.Mars.Host` точечно: `PostTypeFeatureFieldsTests` (+ контент),
  валидаторы типов (фича без поля / поле неверного типа), `PostTransformer`
  (резолв процессора по редактору поля контента), исключение поля контента в
  `EnrichWithBlank…`;
- интеграционные точечно: `CreatePostTypeTests`/`UpdatePostTypeTests` (маппинг
  реквестов), `PostTypeViewServiceTests`, `PostTransformerTests`,
  `AppEntityCreateNodeTests`, тесты с `PostContentSettingsDto` в данных
  (`PostTypeViewServiceSqlTests`, генераторы значений);
- **контракты `WebApiClient` обязательно**: тесты, создающие типы
  (`ListMetaValueRelationModelsTests` и др.) — реквест меняется ломающе;
- миграция — накатить на локальную БД, проверить data-перенос существующих типов
  (поле `content`, редактор/codeLang в Options, маркер);
- админка — визуально при разработке, отдельным прогоном не проверять.

## Вне скоупа

- маркер автора/формата значения контента (отклонён 2026-08-24: формат всегда =
  текущий редактор типа; при потребности вернуться к пер-постовому хранению);
- конкретные ключи `PostTypeEntity.Options` (настройки заголовка/тегов и т.п.) —
  появляются вместе с фичами, которые их используют;
- конвертация значения при смене редактора;
- ленивая инициализация тяжёлых редакторов — после редизайна форм;
- уникальность (Фаза 5);
- Single-тип, режим карточек в секциях детей;
- редактирование WYSIWYG ИИ-агентом (сейчас не поддерживается — так и остаётся);
- ~~опрос-коммит в обёртках тяжёлых редакторов~~ — сделано 2026-08-24:
  механизм `IHeavyMetaValueEditor`/`FormMetaValue.PullAsync` (см. «Вектор
  развития», п. 3).

## Вектор развития (зафиксировано обкаткой 2026-08-24)

1. **Системные поля — через механизм метаполей.** К контенту и заголовку
   должны применяться те же правила, что и к метаполям: валидаторы, порядок,
   скрытие, генераторы. Контент уже стал фича-полем (эта фаза) — следующий шаг:
   заголовок и остальные системные поля.
2. **Все поля формы объекта — как метаполя.** Вектор: каждое поле (включая
   системные) рендерится и настраивается единым механизмом — переупорядочивание,
   скрытие, генераторы, валидаторы, редакторы из реестра.
3. **Тяжёлые редакторы без опроса — сделано 2026-08-24.** Контракт
   `IHeavyMetaValueEditor` (`GetValueAsync`/`SetValueAsync`/`CommitAsync`,
   значение `string?`): тяжёлые обёртки сами регистрируются в каскадном
   `FormMetaValue` (`RegisterHeavyEditor`/`PullAsync`), значение забирается
   в модель только при сохранении формы (`PullAsync` в `BeforeSave` поста,
   категории, пользователей и в ИИ-`GetFields`). Обёртки WYSIWYG/Code —
   на pull-контракте (таймеры опроса убраны; у кода Ctrl+S остался
   промежуточным коммитом); блочный редактор реактивный, интерфейс — для
   единообразия и ИИ. Лёгкие инлайн-редакторы остались реактивными.
4. **Динамическое подключение редакторов — сделано 2026-08-24.**
   `AppFront.Main` больше не зависит от `EditorJsBlazored`:
   `MetaFieldEditorLocator.Register(key, component, fieldTypes)` — открытый
   статический реестр; обёртка `MetaValueBlockEditor` перенесена в админку
   (`AppAdmin/Components`) и регистрируется в `Program` при старте. Ключ
   `core.blockeditor.editorjs` в каталоге есть всегда; где компонент не
   зарегистрирован — дефолтный редактор (мягкая деградация). Это же — точка
   расширения для плагинных редакторов (`plugin.*`).
