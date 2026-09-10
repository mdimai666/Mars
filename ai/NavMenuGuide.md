# Mars NavMenu Guide for Agent

## Обзор

**Меню (NavMenu)** в Mars — именованные наборы пунктов (ссылки, заголовки, разделители) с иерархией
(`ParentId`), ролями доступа и CSS-атрибутами. Используются в двух контурах:

- **Публичный фронт** — меню сайта (шапка, футер, произвольные), отдаются SPA/SSR через
  `InitialSiteDataViewModel`.
- **Админ-панель** — системное **dev-меню** (сайдбар админки, slug `dev`). Генерируется кодом,
  но редактируется в админке: при первом сохранении копия кладётся в БД и дальше мерджится
  с дефолтным состоянием.

## Хранение и слои

| Слой | Где | Назначение |
|---|---|---|
| Сущность | `Mars.Host.Data/Entities/NavMenuEntity.cs` | Таблица `nav_menus`: Title, Slug, Class, Style, Roles, RolesInverse, Disabled, Tags |
| Пункты | `Mars.Host.Data/OwnedTypes/NavMenus/NavMenuItem.cs` | Owned-коллекция, **JSON-колонка** (`OwnsMany` + `ToJson()`): Id, ParentId, Title, Url, Icon, Roles, Disabled, IsHeader, IsDivider, OpenInNewTab |
| Репозиторий | `Mars.Host.Repositories/NavMenuRepository.cs` | CRUD + `ListAllActiveDetail` (только активные меню и пункты) |
| Сервис | `Mars.Host/Services/NavMenuService.cs` | Синглтон, кэш, dev-меню, публичная фильтрация |
| Генератор дефолта | `Mars.Host/Services/DevMenuFactory.cs` | Дефолтное dev-меню + merge |
| API | `Mars.WebApp/Controllers/NavMenuController.cs` | CRUD (роль Admin), admin-список, reset |
| Админка | `src/AppAdmin/Pages/NavMenuViews/` | Список `ManageNavMenuPage`, редактор `EditNavMenuPage` |
| Публичная отдача | `ViewModelController.InitialSiteDataViewModel` → `InitialSiteDataViewModelHandler` | Меню для фронта/админки |

## Публичная отдача и тег `system`

- `GET vm/ViewModel/InitialSiteDataViewModel?devAdminPageData=...` (без авторизации):
  - `GetAppInitialDataMenus(includeDevMenu)` берёт активные меню из БД и **отфильтровывает меню
    с тегом `system`** — публичному фронту системные меню не отдаются;
  - при `devAdminPageData=true` (админка) дополнительно отдаётся dev-меню: смерженное,
    с выкинутыми `Disabled`-пунктами; если dev-меню целиком `Disabled` — не отдаётся.
- Админка строит сайдбар из этого ответа: `AdminLayout` ищет меню **по `Slug == "dev"`**
  и проверяет роли каждого пункта (`MenuRolesCheck`).
- `CentralSearchService` (глобальный поиск админки) тоже индексирует пункты dev-меню.

## Dev-меню: генерация и merge

- Дефолт генерирует `DevMenuFactory.Build(postTypes)`: фиксированные пункты (Главная, Медиа,
  Записи, Типы, Меню, Разделы→Письма, Управление→Пользователи/Типы пользователей/Типы категорий,
  Плагины, Настройки) + пункт на каждый активный PostType (кроме `post`).
- **Стабильные Guid пунктов**: каждый пункт имеет строковый ключ (`home`, `media`,
  `post-type:{typeName}`, `divider-1`…), Id = SHA256-производная от ключа. На этом держится
  merge — не менять на `Guid.NewGuid()`. Id самого меню `9596ffe0-…`, секций «Разделы»/«Управление»
  — исторические константы в фабрике.
- `NavMenuService.DevMenu()` = копия из БД (если есть), смерженная с дефолтом
  (`DevMenuFactory.Merge`):
  - пункты из БД переопределяют дефолтные (включая `Disabled` — так «удаляют» дефолтные пункты);
  - отсутствующие в БД дефолтные пункты добавляются в дефолтную позицию — поэтому новый PostType
    сам появляется в сохранённом dev-меню;
  - кастомные пункты (Id не из дефолта) сохраняются как есть;
  - у пунктов проставляется вычисляемый флаг `IsSystem`, у меню — `IsPersisted`
    (`false`, пока копии нет в БД и отдаётся чистый дефолт).

## Системные меню: правила

- Помечаются тегом **`system`** (`DevMenuFactory.SystemTag`).
- **Upsert**: `Update` для dev-меню при отсутствии записи создаёт её с фиксированным Id;
  сервер принудительно удерживает тег `system` и slug `dev` (`EnforceSystemMenuInvariants`).
- **Удаление запрещено** (`Delete`/`DeleteMany` бросают `UserActionException`).
- **Сброс к дефолту**: `POST api/NavMenu/{id}/reset` — удаляет копию из БД, меню снова
  отдаётся генерируемым состоянием.
- **Список для админки**: `GET api/NavMenu/admin/list/offset` (`ListForAdmin`) подмешивает
  несохранённые системные меню виртуальной записью (первая страница, с учётом поиска) —
  dev-меню видно в списке и до первого сохранения. Публичные `list/offset`/`list/page` этого не делают.

## Редактор в админке (`EditNavMenuPage`)

- `StandartEditContainer` с `CanCreate=false` и `CanDelete=false` для системных меню
  (параметр `CanDelete` реально учитывается кнопкой удаления контейнера).
- Пункты: **системные нельзя удалить** — только переключатель «отключить/включить»
  (`Disabled`, без подтверждения); **кастомные** удаляются (`DFluentDeleteButton` с подтверждением
  в панели свойств, ссылка «удалить» в дереве; удаление забирает и дочерние пункты).
- После Save/Delete/Reset страница вызывает
  `ViewModelService.TryUpdateInitialSiteData(forceRemote: true, devAdminPageData: true)` —
  иначе сайдбар админки не обновится до перезагрузки.
- Кнопка «Сбросить к дефолту» в сайдбаре редактора видна только когда меню `IsSystem && IsPersisted`.

## Кэш

`NavMenuService` держит в `IMemoryCache` два ключа (TTL 24 ч):

- `NavMenuService::NavMenu.dev` — смерженное dev-меню;
- `NavMenuService::NavMenu.activeMenus` — активные меню из БД.

`ClearActiveMenusCache()` сбрасывает оба; вызывается на любом CRUD. Событие
`PostTypeAnyOperation` дополнительно инвалидирует кэш dev-меню (пункты зависят от PostTypes).

## Грабли

- **EF `SetValues` не копирует owned-коллекции.** `Entry(entity).CurrentValues.SetValues(new {…})`
  копирует только скалярные свойства; `MenuItems` (навигация на owned JSON) молча не сохранялась
  при Update (баг 2026-08-14: «новый пункт меню не сохранился»). В `NavMenuRepository.Update`
  пункты присваиваются явно: `entity.MenuItems = MapToItems(query.MenuItems)`. Не убирать.
- **Slug `dev` — контракт с админкой.** `AdminLayout` ищет меню по slug; сервер запрещает его
  менять для dev-меню. Если появится второе системное меню — потребителю нужен свой резолвер.
- **Merge опирается на стабильные Id.** Дефолтные пункты никогда не удаляются физически —
  «удаление» это `Disabled`; отсутствующий в БД дефолтный пункт считается новым и добавляется.
- **Регрессионный тест** персистентности пунктов: `tests/Mars.Integration.Tests/Controllers/NavMenus/UpdateNavMenuTests.cs`
  (`UpdateNavMenu_MenuItems_ShouldBePersisted`, нужен Docker с postgres:14).
