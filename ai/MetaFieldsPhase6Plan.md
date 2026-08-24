# План мета-полей — Фаза 6: Single-тип

> **Статус: реализована 2026-08-24 (точечные тесты зелёные; до коммита —
> обкатка пользователем).** Продолжение `ai/MetaFieldsPhase3Plan.md`
> (пункт «вне скоупа: Single-тип»), Фазы 4–5 — `ai/MetaFieldsPhase4Plan.md` /
> `ai/MetaFieldsPhase5Plan.md`. Ветка `ai/metafields-rework`. Без миграций
> (маркер — строка в `EnabledFeatures`).
> **Итог реализации**: фича `Single` в `PostTypeConstants.Features`
> (+ перевод «Единственная запись» в `AppRes`); `IPostRepository` +=
> `CountByTypeAsync(postTypeId)` и `GetFirstByTypeAsync(typeName)`;
> `PostService.GetOrCreateSingleAsync(type)` — отдаёт первый пост типа,
> нет — создаёт из бланка через обычный `Create` (Title = Title типа,
> слаг серверный `TextTool.TranslateToPostSlug` с числовым суффиксом при
> коллизии, дефолты полей материализуются через
> `ModifyMetaValueDetailQuery.GetBlank`, кроме Query/контент-фичи/
> Disabled-полей); эндпоинт `GET api/Post/single/{type}` (Authorize);
> запрет второго поста — `CreatePostQueryValidator` (все пути создания,
> включая JSON-запись — она идёт через `PostService.Create`); запрет
> удаления — `DeletePostQueryValidator` (+ `DeleteMany` через него же);
> запрет включения фичи типу с 2+ постами — `UpdatePostTypeQueryValidator`;
> админка: `ManagePostView` для Single рендерит `EditPostView` вместо
> грида (без «Добавить» и настроек колонок), бланк-маршрут
> `/dev/EditPost/{type}` редиректит на единственную запись
> (`EditPostPage`); `WebApiClient` — `client.Post.Single(type)`;
> бейдж «Single» в списке типов. Тесты: юнит
> `PostSingleValidatorTests` 7/7, интеграционные `PostSingleTests` 5/5
> (создание при первом открытии; дефолты; второй пост → 400; удаление →
> 400; включение при 2 постах → 400), контракт `SinglePostTests` 1/1.

Single-тип — тип поста с ровно одной записью (аналог Strapi single type,
Directus singleton, Payload Globals, ACF Options page): «Настройки сайта»,
«Главная страница», «О нас». Позиционирование: контентная модель, не
отдельная сущность — единственный пост живёт в обычной таблице `posts`,
отличается только поведениями вокруг типа.

## Контекст

Разведка 2026-08-24 (состояние после Фазы 5):
- механизм фич типа готов: `PostTypeConstants.Features` (Mars.Shared:
  `Content`, `Status`, `ModifyCreatedDate`, `Language`, `Tags`, `Excerpt`,
  `Category`, `PostImage` + массив `All`), хранение — строки в
  `post_types.enabled_features`; чекбоксы в `EditPostTypePage` рендерятся
  циклом по `Features.All`; серверная целостность фич —
  `CreatePostTypeQueryValidator`/`UpdatePostTypeQueryValidator`; клиентская
  синхронизация — `PostTypeEditModel.ToggleFeature`;
- админка постов: `/dev/Post/{typeName}` → `ManagePostPage` →
  `ManagePostView` (грид + кнопка «Добавить» = ссылка на
  `/dev/EditPost/{typeName}`); редактирование — `EditPostPage` →
  `EditPostView` (параметры `ID`, `PostTypeName`, `OnSaved`,
  `NavigateAfterCreate`); `EditPostView` уже вкладывается как компонент —
  `ChildPostEditorDrawer` (drawer детей из Фазы 2);
- бланк поста: `PostService.GetPostBlank(PostTypeDetail)` /
  `GetEditModelBlank(type)` — пустой пост, первый статус типа, мета-значения
  с дефолтами полей; создание — `PostService.Create(CreatePostQuery)` с
  валидацией;
- валидация постов: `GeneralPostQueryValidator` (общая для create/update:
  slug, тип, фичи `Status`/`Category`), `CreatePostQueryValidator`
  (+ `ValidateMetaValues`), `DeletePostQueryValidator`/
  `DeleteManyPostQueryValidator`; доступ к данным в валидаторах —
  `IMetaModelTypesLocator` + репозитории через DI;
- меню: `DevMenuFactory.Build` генерирует пункт на каждый публичный тип с
  `Url = /dev/Post/{typeName}` (стабильный `Id`), сохранённая копия
  мержится по `Id` — для Single менять фабрику не нужно;
- защита-прецедент: `DeletePostTypeQueryValidator` с
  `undeletableTypes = ["post", "block", "page"]`.

UX-исследование (проверено по докам 2026-08-24): сходящийся паттерн —
флаг на типе контента; навигация сразу в форму редактирования (списка и
кнопки «Добавить» нет); запись либо «существует всегда» (Payload Globals,
ACF Options page — нет удаления), либо создаётся при первом сохранении
(Strapi); Directus при включённом «Singleton» открывает страницу записи
вместо списка. Выбран вариант «существует всегда» + запрет удаления.

## Принятые решения (2026-08-24)

1. **Маркер — фича `Single`** в `PostTypeConstants.Features` (константа +
   в массиве `All`): хранение в `EnabledFeatures`, чекбокс в форме типа
   подтянется сам, миграция не нужна. Не колонка и не `Options`-ключ.
2. **Автосоздание единственного поста при первом открытии** (модель
   Payload Globals / ACF, как в черновике Фазы 3): серверный
   get-or-create. Пост есть — отдаётся; нет — создаётся из бланка:
   `Title` = Title типа, `Slug` = серверная генерация
   `TextTool.TranslateToPostSlug(Title)` (сейчас дефолт слага живёт на
   клиенте — для автосоздания переносится на сервер; коллизия с
   существующим слагом типа решается числовым суффиксом), первый статус
   (если фича `Status`), мета-значения из дефолтов полей (бланк).
   Создание — обычный путь `PostService.Create` с валидацией.
3. **Навигация: редактор вместо списка.** Пункт меню остаётся
   `/dev/Post/{typeName}`; `ManagePostView` для Single-типа рендерит
   `EditPostView` единственного поста прямо вместо грида (компонент уже
   вкладывается в `ChildPostEditorDrawer`), без редиректа — стабильный
   URL без id, как Directus/Strapi. Кнопка «Добавить» и настройки грида
   для Single не показываются. `DevMenuFactory` не меняется.
4. **Второй пост запрещён** — серверное правило на всех путях создания
   (админка, JSON-запись, ноды): тип имеет фичу `Single` и пост уже
   есть → 400 «тип 'X' — single, запись уже существует». Точка —
   `CreatePostQueryValidator` (и валидаторы JSON-записи постов, если
   путь собственный); проверка наличия поста — через `IPostRepository`.
5. **Удаление единственного поста запрещено**:
   `DeletePostQueryValidator` и `DeleteManyPostQueryValidator` — ошибка
   «запись single-типа нельзя удалить» (по образцу
   `DeletePostTypeQueryValidator`). Удаление самого типа каскадит посты
   как обычно.
6. **Включение фичи типу с постами**: 0 постов — пост создастся при
   первом открытии; 1 пост — он становится единственным; 2+ — запрет в
   валидаторах типа («у типа уже есть записи»). Выключение фичи — всегда
   разрешено, посты остаются обычными.
7. **Ортогональность**: `Single` не зависит от `Visibility` (тип в меню —
   как сейчас, по `Public`) и совместим с прочими фичами (`Content`,
   `PostImage`, …). Публичное чтение (шаблоны/фронт) — без спец-механики:
   обычные списки/запросы по типу видят единственный пост; авто-создание
   только из админки.

## Шаги реализации

1. **Контракт и фича.** `PostTypeConstants.Features.Single`; валидаторы
   типа: запрет включения при 2+ постах (нужен подсчёт/проверка наличия
   постов типа — минимальный метод в `IPostRepository`, например
   `ExistAnyByTypeAsync(typeName, ct)`; переиспользовать и в шаге 2);
   бейдж «single» в списке типов админки (рядом с компонентным).
2. **Get-or-create и защиты.** `PostService.GetOrCreateSingleAsync(type,
   ct)` (+ `IPostService`); эндпоинт в `PostController`
   (`GET api/Post/single/{type}`); правило «второй пост запрещён» в
   создании; запрет удаления в обоих валидаторах удаления; проверить, что
   путь JSON-записи постов покрыт тем же правилом.
3. **Админка и клиент.** `client.Post.GetOrCreateSingle(type)` в
   `WebApiClient` (`IPostServiceClient` + реализация); `ManagePostView`:
   ветка Single — get-or-create → `<EditPostView ID=... PostTypeName=...
   NavigateAfterCreate="false" />` вместо грида, скрыть «Добавить» и
   настройки грида; защита бланк-маршрута `/dev/EditPost/{type}` для
   Single (переключение на редактирование существующего).

## Верификация (правило: точечно, не весь сьют)

Общий build `Mars.slnx`.
- Юнит `Test.Mars.Host`: get-or-create (отдаёт существующий; создаёт с
  заголовком/слагом/дефолтами), правило «второй пост», запрет удаления,
  запрет включения фичи типу с 2+ постами.
- Интеграционные `Mars.Integration.Tests`: single-сценарий (открытие →
  пост создан; повторное создание → 400; удаление → 400; включение фичи
  типу с 2 постами → 400).
- Контракты `WebApiClient` — новый метод клиента (по конвенции фаз).
- Админка (редактор вместо списка, без «Добавить») — визуально при
  разработке.

## Вне скоупа

- публичный (вне админки) get-or-create — создание только из админки;
- мультиязычные версии сингла (один сингл на все языки; станет задачей с
  приходом i18n);
- иконки/позиции пунктов меню типов (старый хинт в `PostTypeEntity`);
- «карточки» и прочие режимы отображения — из скоупа Фазы 2–3;
- переименование фичи/маркера — имя `Single` зафиксировано.
