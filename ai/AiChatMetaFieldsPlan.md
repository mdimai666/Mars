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

### Фаза 2 — серверные инструменты (детализировать перед стартом)

Набросок (детали продумываются отдельно):

- [ ] `MarsPostTools.CreatePost` + опциональный `metaJson` (JSON-объект «ключ поля → значение»):
      перевод на `IPostJsonService.Create`; статус/язык/редактор контента — как сейчас, из
      `GetEditModelBlank`; адаптация контента под редактор сохраняется.
- [ ] Новый `UpdatePost(postId, …)`: `GetDetail` → `PostJsonDto`, патч только переданных полей
      (title, contentText, tagsCsv, excerpt, status, metaJson — слияние по ключам, не замена
      словаря) → `UpdatePostJsonQuery`. Описание инструмента — с предупреждением last-write-wins
      и правилом «сначала прочитать».
- [ ] `GetPost`: отдавать метаполя (`PostJsonDto.Meta`) и `imageFieldKey` типа.
- [ ] Регистрация в `ContentToolset.Build`; DI — `IPostJsonService` уже зарегистрирован (проверить).
- [ ] Открытые вопросы к детализации: как `PostJsonService` конвертирует `Meta` (JsonNode →
      EAV) и какие у него валидаторы; нужен ли `FormValueText` на этом пути (модель отдаёт
      JSON — вероятно, нет); формат `contentText` → контент по редактору при update
      (переиспользовать `BuildBlockEditorJson`/`BuildHtml`); уведомление хаба
      `PostListChanged` при update.

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

### Фаза 4 — скилл `mars-posts` (+ `mars-media` при необходимости)

- [ ] Метаполя: сначала `GetOpenPageInfo`/`GetPost` (типы, кратность, варианты), не угадывать;
      Select — только ключи вариантов; формат множественных (JSON-массив / CSV).
- [ ] Картинка поста: рецепт `AddMedia(url)` или `ListMedia` → file id → Image-поле
      (`imageFieldKey` или явный ключ) — и на странице (`SetOpenPageField`), и без неё
      (`CreatePost`/`UpdatePost` с metaJson).
- [ ] Статус — slug из списка статусов типа; `UpdatePost` — читать перед записью (last-write-wins).

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
