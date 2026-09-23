# AiChat — метаполя и картинка поста (план)

Инициатива: дать ИИ-агенту доступ к метаполям постов и картинке поста (фича `PostImage`);
заодно закрыть расхождения `AiChatGuide.md` после реструктуризации 2026-08-30.
Смежные гайды: [AiChatGuide.md](./AiChatGuide.md), [MetaFieldsGuide.md](./MetaFieldsGuide.md),
[FormEngineGuide.md](./FormEngineGuide.md).

Статус: **Фаза 1 выполнена** (2026-09-23). Фазы 2–3 — по требованию пользователя делаются
тщательно, с продумыванием деталей: перед реализацией каждой — отдельный раунд детализации.

## As-is (аудит 2026-09-23)

- После реструктуризации AiChat жив: `Mars.AiChat.Host` компилируется; `ContentToolset`
  создаёт `MarsPostTools` на запуск (CreatePost/GetPost/ListPosts через `IPostService`);
  `PageSkillRouter` роутит сегмент `/Post/` → скилл `mars-posts`; мост открытой страницы —
  `EditPostView : IAiChatPageHandler` (`src/Mars.Admin/Pages/PostsViews/EditPostView.razor.cs`).
- Метаполя агенту недоступны:
  - мост `SetField` принимает только `AgentEditableFields` = title/slug/excerpt/tags/categories/content;
  - `GetFields()` не возвращает значения метаполей; `GetInfo()` — плоские ключи полей без
    типов/кратности/вариантов (агент слеп);
  - серверный `CreatePost` отправляет `MetaValues = []`.
- Картинка поста: фича `PostImage` → указатель `post_types.image_field_key` на Image-метаполе,
  значение — Guid файла. `AddMedia` (внешний URL → file id) и `ListMedia` у агента есть,
  положить id в поле было нечем.
- Механика значений уже готова: `PostFormValueStore` → `MetaValueStore`
  (`src/Admin/Mars.Admin.Framework/Components/MetaFieldViews/MetaValueStore.cs`) пишет
  канонические CLR-значения в EAV-строки `PostEditModel.MetaValues`; канон форматов —
  `FormValueCodec` (`Mars.Forms.Contracts`). Серверный JSON-путь постов —
  `IPostJsonService` (`Meta` — словарь JsonNode, конвертация и валидаторы внутри `PostJsonService`).
- `ai/AiChatGuide.md` устарел (имена проектов `Shared`/`Host.Shared`, пути `Mars.Host.Shared`,
  `ai/FrontReworkPlan.md`) — исправлен в фазе 1.

## Решения (2026-09-23, пользователь)

1. Пути правки — все три, не сужать:
   - мост открытой страницы (`SetOpenPageField` + метаполя) — основной путь;
   - серверный `CreatePost` с метаполями — через JSON-путь `IPostJsonService.Create`
     (`CreatePostJsonQuery.Meta`), не изобретая конвертацию;
   - серверный `UpdatePost` через `IPostJsonService` — read-modify-write внутри одного вызова
     инструмента. Прежний запрет «серверный update затирает метаполя» снят: полная замена
     формируется из свежепрочитанного `PostJsonDto`, патч накладывается только на переданные
     поля. Last-write-wins — осознаётся, предупреждение в описании инструмента.
2. Редактируемые системные поля моста расширяются `status` (slug статуса) и `lang`;
   `created_at` — не добавляем.
3. Парсер «текст → значение» — тонкий слой поверх `FormValueCodec`, не самостоятельная
   реализация: `FormValueText` в `Mars.Forms.Contracts` (текст → канонический JsonNode →
   CLR кодека). Сделано в фазе 1.
4. Картинка поста — без отдельного инструмента: значение Image-поля = Guid файла из
   `AddMedia`/`ListMedia`; знание — в `GetInfo` (imageFieldKey + признак фичи) и рецепт в скилле.
5. Вне объёма: запись WYSIWYG-контента (нет публичного сеттера), серверные инструменты
   категорий/пользователей, серверная проверка «файл существует» при установке Image-поля.

## Фазы

### Фаза 1 — хвосты аудита + парсер ✅ (2026-09-23)

- [x] `ai/AiChatGuide.md`: `Mars.AiChat.Contracts`/`.Abstractions` вместо `Shared`/`Host.Shared`;
      `IFrontFilesService` — `Mars.SiteEngine.Abstractions/Services` (+ реализация
      `Mars.SiteEngine.Host/Services`); ссылка на `ai/FrontsGuide.md`; roadmap-пункт
      «редактирование поста без страницы» → указатель на этот план.
- [x] `FormValueText` (`src/Mars.Modules/Mars.Forms.Contracts/FormValueText.cs`):
      `TryToClr(raw, descriptor)` (одиночные → канонический CLR, множественные →
      `IReadOnlyList<object?>`, SelectMany → массив ключей), `TryToNode`, `TryToListNode`.
      Форматы: bool — true/false; числа — invariant; decimal → строка-узел (канон wire);
      дата — ISO-8601 → «O»; Relation/File/Image — Guid; Object — JsonNode.Parse;
      множественные/SelectMany — JSON-массив (строгий, канон кодека) или CSV (терпимый);
      пустой текст — «снять значение»; ключи Select/SelectMany сверяются с
      `descriptor.Choices` (ошибка со списком доступных).
- [x] Тесты: `tests/Mars.Forms.Tests/Codec/FormValueTextTests.cs` — 138/138 зелёных
      (весь проект Mars.Forms.Tests).
- [x] Сборка `Mars.slnx` — зелёная.

### Фаза 2 — серверные инструменты (детализирована 2026-09-23)

#### Находки детализации — баги JSON-пути записи постов (публичный `PostJsonController`)

- **B1. File/Image не пишутся**: `MetaFieldUtils.MetaValueFromJson`/`MetaValueFromString`
  (`Mars.Cms.Abstractions/Utils/MetaFieldUtils.cs`) не имеют веток File(101)/Image(102) →
  `NotImplementedException`; `MultiValuesFromJsonArray` в `PostJsonService` массивы для них
  заявляет, но падает на элементе. Фикс: ветки → `ModelId` (как Relation);
  `MetaFieldTypeToType` их уже маппит.
- **B2. Мульти-значения не-ссылочных типов не пишутся**: `MultiValuesFromJsonArray` бросает
  «array value supported only for Relation/File/Image». Фикс: разрешить массив любому типу
  (индексация есть; валидатор «одинарное поле допускает только одно значение» защитит).
- **B3. `PostJsonDto` теряет `Excerpt`/`LangCode`**: их нет в `PostSummary`/`PostJsonDto`,
  а `UpdatePostJsonQuery` требует → read-modify-write через JSON-API молча затирает анонс и язык.
  Фикс: добавить оба в `PostJsonDto` + `ToJsonDto`/`ToJsonDtoSummary` (`PostDetail` их имеет)
  + `PostJsonResponse`/WebApiClient (расширение ответа; интеграционные тесты —
  `tests/Mars.WebApiClient.Integration.Tests/Tests/PostJsons/`).
- **B4 (семантика, не баг)**: Select/SelectMany на записи — Guid вариантов (`VariantId`),
  чтение отдаёт `MetaFieldVariantValueDto {Id, Key, Title}`. Публичный API не меняем —
  ключ → Guid резолвит инструмент AiChat по `IMetaModelTypesLocator.GetPostTypeByName(type)
  .MetaFields[].Variants`.
- **B5 (находка реализации 2.1, бэклог)**: SelectMany через wire-JSON публичного API не
  записывается: JSON-массив под ключом SelectMany уходит в `MultiValuesFromJsonArray`
  (строка на элемент → падение `GetValue<Guid[]>`), а CLR-узел `JsonValue(Guid[])` из wire
  не получается. AI-нормализатор (2.2) это обходит — строит `JsonValue.Create(Guid[])`
  (CLR-узел, `MetaValueFromJson` его читает). Починка wire-формы SelectMany — отдельная
  задача CMS, в этой инициативе не делается.
- Подтверждено: `IPostJsonService` — scoped, `IMetaModelTypesLocator` — singleton (MainCms);
  create-валидатор требует значения обязательных полей без генератора (`requireAll: true`);
  Query-поля на записи пропускаются; статус — slug, дефолт первого при пустом
  (`ResolveStatus`), при выключенной фиче Status должен быть пустым.

#### Решения фазы 2 (2026-09-23, пользователь)

- Фиксы B1–B3 — в рамках фазы 2 (все три).
- Discovery — отдельный инструмент `DescribePostType(type)` (паттерн `get_database_schema`),
  не встраивать в GetPost.
- `UpdatePost` сохраняет исходного автора (`UserId` = автор поста, не владелец чата).
- Тесты: два новых unit-проекта — `tests/Mars.Cms.Tests` (MetaFieldUtils, конвертация
  JSON-мета) и `tests/Mars.AiChat.Tests` (нормализатор metaJson); интеграционные PostJson —
  расширяем существующий `Mars.WebApiClient.Integration.Tests`.

#### Шаг 2.1 — фиксы CMS (B1–B3) + тесты ✅ (2026-09-23)

- [x] `MetaFieldUtils`: ветки File/Image в `MetaValueFromJson`, `MetaValueFromString`,
      `MetaValueFromObject` (объединены с Relation: `t is Relation or File or Image`).
- [x] `PostJsonService.MultiValuesFromJsonArray` — массив для любого типа; `multiKeys`
      в `UpdateJsonMetaValuesToModifyDto` — любое поле со значением-массивом.
- [x] B3: `PostDetail` += `Excerpt`/`LangCode` (required) → маппинги `ToDetail`/
      `ToDetailWithType` (`Mars.Data.Repositories/Mappings/PostMapping.cs`);
      `PostJsonDto` += оба → `ToJsonDto`/`ToJsonDtoSummary`/`ToResponse`;
      `PostJsonResponse` += оба (wire). WebApiClient не менялся — контракт общий.
- [x] Новый `tests/Mars.Cms.Tests` (добавлен в `Mars.slnx`; `test-all.ps1` подхватывает
      автоматически по скану `tests/*.csproj`) — `MetaFieldUtilsTests`, 19/19.
- [x] Интеграционные: новый `tests/Mars.WebApiClient.Integration.Tests/Tests/PostJsons/
      PostJsonMetaTests.cs` (Image → model_id; мульти-массив строк → строки с Index;
      round-trip excerpt/lang через Get→Update) — 205/205 в проекте.
- [x] `tests/Mars.Server.Tests`: переписан `CreateJsonMetaValues_ArrayForNonRelationField_Throws`
      → `..._CreatesMultiValues` (осознанная смена поведения B2); в фикстуру
      `MetaValuesGeneratorServiceTests.Post()` добавлены новые required-члены — 473/473.
- [x] Сборка `Mars.slnx` зелёная.

#### Шаг 2.2 — нормализатор metaJson (AiChat.Host) ✅ (2026-09-24)

- [x] `Mars.AiChat.Host/Tools/MetaJsonNormalizer.cs` (internal): `TryParseObject`
      (текст аргумента → словарь; пусто → нет мета) + `TryNormalize` (терпимый вход →
      строгая форма JSON-пути). Форматы: String/Text — любой скаляр → строка;
      Bool — true/false/«true»/«false»/1/0; Int/Long — число или строка-число
      (Int — проверка диапазона); Float/Decimal — число или строка; DateTime — ISO-строка;
      Relation/File/Image — Guid-строка; Select — ключ варианта или Guid известного варианта;
      SelectMany — массив ключей или CSV → CLR-узел `JsonValue(Guid[])` (обход B5);
      множественные — JSON-массив, CSV или одиночное значение (заворачивается в массив);
      Query-поля и null-значения пропускаются; неизвестный ключ — ошибка со списком полей.
      Ошибки — «поле 'key': …», с индексом элемента для массивов.
- [x] Скаляры выдаются wire-узлами (`JsonNode.Parse`) — CLR-узлы не конвертируются в
      `GetValue<T>` (см. «Грабли»); SelectMany — исключение (CLR `Guid[]`).
- [x] Терпимость живёт в AiChat; CMS остаётся строгим. `FormValueText` здесь НЕ используется
      (канон wire-форм различается: decimal строкой vs числом, Select ключ vs Guid).
- [x] `InternalsVisibleTo(Mars.AiChat.Tests)` в `Mars.AiChat.Host.csproj`; новый
      `tests/Mars.AiChat.Tests` (в `Mars.slnx`) — `MetaJsonNormalizerTests`, 23/23;
      каждый успешный кейс проверяется round-trip'ом через `MetaFieldUtils.MetaValueFromJson`.
- [x] Сборка `Mars.slnx` зелёная, без новых warning'ов.

#### Шаг 2.3 — инструменты (`MarsPostTools`, `ContentToolset`) ✅ (2026-09-24)

- [x] Конструктор `MarsPostTools` += `IPostJsonService`, `IMetaModelTypesLocator`
      (прокинуты в `ContentToolset`; DI зарегистрирован в MainCms).
- [x] `DescribePostType(type)`: фичи, статусы (slug/title, только при фиче Status),
      редактор контента (`PostTypeDetail.ContentEditorKey()`), `imageFieldKey`,
      дескрипторы метаполей `{key, title, type, multiple, required, hidden, readOnly,
      modelName, variants[]}`; неизвестный тип — ошибка со списком типов (`PostTypesDict`).
- [x] `CreatePost` += `metaJson?`, `status?`; переведён на `IPostJsonService.Create`
      (UserId — владелец чата; LangCode/статус-дефолт — из `GetEditModelBlank`;
      редактор контента — из blank-формы; `AdaptContent` вынесен из switch).
- [x] `UpdatePost(postId, title?, contentText?, tagsCsv?, excerpt?, status?, metaJson?)`:
      пустая строка = «не менять», `-` для tagsCsv/excerpt = «очистить»;
      `GetDetail(renderContent:false)` → патч → `UpdatePostJsonQuery`
      (UserId — исходный автор `dto.Author.Id`; CategoryIds/LangCode/Slug — из прочитанного;
      Status — `dto.Status?.Key`, т.к. Key KVP = slug) → уведомление `PostListChanged`.
      Описание инструмента — last-write-wins, «сначала прочитай».
- [x] `GetPost` → `IPostJsonService.GetDetail`: + excerpt/langCode/imageFieldKey +
      `meta` компактно (`CompactMetaValue`: варианты — key/title, FileDetail — id/name/url,
      массивы рекурсивно, скаляры и dto связей — как есть).
- [x] `DescribePostType`/`UpdatePost` зарегистрированы в `ContentToolset.Build`.
- [x] Сборка `Mars.slnx` зелёная; `tests/Mars.AiChat.Tests` 23/23.

### Фаза 3 — мост открытой страницы (детализировать перед стартом)

Набросок (детали продумываются отдельно):

- [ ] `EditPostView.GetInfo()`: дескрипторы полей `{key, title, type, multiple, required,
      editor, variants[]}` из `f.Model.Form` (включая метаполя) + `imageFieldKey` + признак
      фичи PostImage; `editableFields` — системные слоты + метаполя (+ status/lang).
- [ ] `GetFields()`: значения метаполей через `PostFormValueStore` в каноническом виде
      (Select — ключ варианта, ссылки — Guid, множественные — массив) + status/lang.
- [ ] `SetField()`: ветка «метаполе» — дескриптор из формы → `FormValueText.TryToClr` →
      `SetValue`/`SetList` стора; status/lang — через тот же стор; ошибки парсера возвращать
      модели как есть (они человекочитаемые). Системные слоты и live-редакторы контента не ломать.
- [ ] Открытые вопросы к детализации: как стор/дескрипторы соотносятся с `_commits`
      (тяжёлые редакторы метаполей — WYSIWYG/код/блочный — пишутся отложенно: нужен ли
      `CommitAllAsync` перед чтением, как писать через `FormLiveEditors`); превью картинки
      после установки значения (перерендер плиток); ReadOnly/Hidden-поля — отклонять или
      пропускать; категории — оставить как есть (Guid-CSV) или перевести на стор.

### Фаза 4 — скилл `mars-posts` (+ `mars-media`) ✅ (2026-09-24)

- [x] `ai-skills/mars-posts/SKILL.md` переписан: DescribePostType перед записью
      («не выдумывай ключи/варианты/статусы»), CreatePost/UpdatePost/GetPost/ListPosts,
      форматы metaJson (Select — ключ, ссылки — Guid, множественные — массив, readOnly —
      не передавать, required — при создании), картинка поста (AddMedia/ListMedia → Guid →
      imageFieldKey через metaJson), last-write-wins и приоритет моста открытой страницы
      над серверным UpdatePost при открытой форме.
- [x] `ai-skills/mars-media/SKILL.md`: буллет «картинка поста — значение Image-поля (Guid),
      не тег в контенте; рецепт — в mars-posts».
- [x] Frontmatter-формат сохранён (name/description/tags) — каталог скиллов парсит его.

### Фаза 5 — проверка

- [ ] `dotnet build Mars.slnx` + точечно: `tests/Mars.Forms.Tests` (парсер), затронутые
      Cms-тесты при изменении JSON-пути.
- [ ] Headless-полигон для серверных инструментов:
      `Mars.dll aichat send -m "создай пост … с картинкой из url …"` (нужно настроенное
      ИИ-подключение; page bridge без браузера не проверяется).
- [ ] Мост — вручную в админке пользователем (браузер без прямой команды не открывать).

## Грабли

- `Mars.Forms.Tests.exe --filter …` не поддерживается (MTP, «unknown option») — запускать
  exe проекта целиком.
- JSON-массив в `FormValueText` — строгая каноническая форма (`FormValueCodec`): числа —
  JSON-числами, decimal — строкой; «1» строкой для Int отклоняется. CSV-путь терпимый.
- `MetaValueStore` молча превращает неизвестный ключ варианта в `Guid.Empty` — поэтому
  сверка с `Choices` сделана в `FormValueText`, а не оставлена стору.
- **`JsonValue.GetValue<Guid>()` не работает на CLR-узле из строки** (`JsonValue.Create("guid")`
  → `InvalidOperationException`): конвертация есть только у JsonElement-узлов (пришедших из
  wire-JSON) и у `JsonValue.Create(Guid)`. Нормализатор 2.2 должен выдавать либо
  `JsonValue.Create(guid)`, либо узел из `JsonNode.Parse("\"guid\"")`. То же касается
  `GetValue<Guid[]>` для SelectMany (CLR-узел `JsonValue.Create(Guid[])` — работает).
- Юнит-тесты internal-методов `PostJsonService` уже живут в `tests/Mars.Server.Tests/
  Services/PostJsonServices/` (`PostJsonServiceTestBase`, InternalsVisibleTo) — новые тесты
  конвертации класть туда, а не в Mars.Cms.Tests.
