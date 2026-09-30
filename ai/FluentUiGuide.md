# FluentUiGuide — как верстать UI в Mars (FluentUI Blazor v5)

Хаб для любой UI-работы: админка (`src/Mars.Admin`, `src/Admin/*`), фронты модулей
(`*.Front`), docs-сайт, devstands. Читать ПЕРЕД правками; факты библиотеки не раскапывать
заново (маршрут ниже). Гайд для агента: буллеты с путями, «Инварианты», «Грабли»,
«Отклонено», рецепты.

## Маршрут фактов (не раскапывать библиотеку)

- **`ai/FluentV5Reference.md`** — данные пакета 5.0.0: живые токены со значениями, карта
  `::part`, rendering-факты (host/shadow), параметры используемых компонентов, мёртвые
  v4-параметры, тихие грабли. Первый источник для «какой токен/part/параметр есть у X».
- **`ai/CssRefactoringGuide.md`** — CSS-архитектура: три слоя (`--mars-*` → important-мост
  на `html:root` → Fluent-токены), карта less-файлов, dark-хук `body[data-theme="dark"]`,
  грабли LESS (версии, интерполяция, `*/` в комментариях), рецепт компиляции.
- **`ai/FluentV5MigrationPlan.md`** — история миграции v4→v5 и почему решения такие
  (по закрытии инициативы схлопывается сюда по `ai/PlanLifecycleGuide.md`).
- **MCP `fluentui-blazor`** — только API/миграция-маппинг («какие параметры у компонента»,
  «чем заменён X»). Runtime-CSS не знает; описывает dev-ветку и расходился со stable —
  при расхождении верить Reference/пакету.
- **nuget-пакет / GitHub** — ОТКРЫВАТЬ ТОЛЬКО если факта нет в гайдах. Новую находку
  СРАЗУ дописывать в `FluentV5Reference.md` (амортизация). Кэш пакета:
  `C:\Users\D\.nuget\packages\microsoft.fluentui.aspnetcore.components\5.0.0\`.
- Быстрый путь для точечного бага: snippet из DevTools от пользователя (computed-стили
  элемента) — половина диагностики не нужна.

## Инструменты — `tools/ui/` (подробности в `tools/ui/README.md`)

Playwright-core + системный Edge (`channel: 'msedge'`, браузеры не скачиваются).
**Конвенция: агент работает БЕЗ скриншотов** — assertions и computed-замеры; `--shots`
только по просьбе пользователя.

- `serve.ps1` — старт сервера: `pwsh -NoProfile -File tools/ui/serve.ps1 [-Port 5005]`.
  Порт задаётся env `Urls` (`--urls`/ASPNETCORE_URLS перебивает CliSocketServer).
- `probe.js` — точечный замер computed-стилей/боксов/CSS-переменных элемента.
  Стандартный первый шаг для «почему это выглядит не так»:
  `node probe.js --page /dev/Post --sel ".fluent-data-grid th" [--vars --filter mars]`.
- `crawl.js` — обход 35 страниц админки: консоль-ошибки + DOM-assertions
  (`assertions.json`), диалог ноды (dblclick/ESC). Exit 1 при падениях.
  **Починил UI — добавь assertion** в `assertions.json` (это регрессионная сетка).
- `tokens.js` — снапшот всех CSS-переменных темы с `<html>` (inline/computed/adopted).
- `diff.js` — pixel-diff двух папок скриншотов: v4-база (`2026\Mars` master-копия,
  порт 5003) ↔ v5 (5005). Числовой отчёт, агент картинки не смотрит.
- `sweep.js` — grep src/docs/devstands на мёртвые v4-параметры (список — блок
  `sweep-dead-params` в `FluentV5Reference.md`, иначе встроенный). Только репорт.
- Auth: `tools/ui/auth.local.json` `{"user","pass"}` (в .gitignore); после первого логина
  скрипты переиспользуют `auth.json` (storageState).
- **Галерея компонентов**: `/dev/builder/style-gallery`
  (`src/Mars.Admin/Builder/StyleGalleryViews/`, скрытая dev-страница, в навигации нет) —
  все используемые Fluent-компоненты во всех вариантах, вкл. регрессионный DataGrid с
  `ItemSize="76"`+Virtualize и оба паттерна диалогов. Секции/варианты несут атрибуты
  `data-gallery-section`/`data-gallery-variant` для assertions. Страница входит в PAGES
  crawl.js — при визуальных правках прогонять crawl, при новых компонентах — добавлять
  секцию в галерею.

## Типовой цикл правки стилей

1. `probe.js` — замер факта: какой токен/правило/inline виноваты (inline на элементе =
   компонент пишет сам — CSS перебивается только `!important`).
2. Правка ТОЛЬКО `.less` (какой файл за что — `CssRefactoringGuide.md` §«Структура»).
3. Компиляция (исключение визуального цикла — обычно компилирует пользователь):
   в `src/Mars.Admin/wwwroot/css`:
   `npx --package less@4.1.3 lessc --source-map-map-inline style.less style.css` + дописать BOM.
4. `probe.js` / `crawl.js` — перемер. Новые wwwroot-ФАЙЛЫ попадают в отдачу только после
   пересборки; правки существующего `style.css` подхватываются на лету.
5. После коммита css/js — bump `MarsAppVersion` в `Directory.Build.props` (cache-busting).

## Инварианты вёрстки v5

- `FluentMenu`: пункты ТОЛЬКО внутри `FluentMenuList` (иначе тихий слом — в попап улетает
  первый пункт); инлайн-`<style>` не должен быть первым child меню.
- В Icon-параметры NEVER не передавать `null` (`Activator.CreateInstance` краш).
- `FluentBadge`: текст в `Content` (ChildContent = обёрнутый элемент); дефолт серый
  (css-оверрайд `fluent-badge[color="brand"]:not(.badge-accent)` в `fluent-ui.less`),
  акцент — класс `badge-accent`; `Subtle` на светлых поверхностях невидим.
- `FluentDataGrid`: рендерит `table.fluent-data-grid` (не custom element); `ItemSize`
  (виртуализация) в v5 inline-растягивает `th` — глобальный оверрайд `height:auto!important`
  в `fluent-ui.less`; провайдеры обязаны клампить `Count` (`Take=0` → 400 от API).
- Диалоги: шим `src/Admin/Mars.Admin.Framework/Dialogs/`; инлайн-диалоги — `@ref` +
  `ShowAsync()/HideAsync()` в `OnAfterRenderAsync` + `OnStateChange(DialogState.Closed)`
  (паттерн — `AIToolChatModal`, `NodeEditContainer1`); ширина — через
  `::part(dialog)` (`--dialog-width` мёртв).
- Навигация: `FluentNav`/`FluentNavCategory`/`FluentNavItem`/`FluentNavSectionHeader`
  (Menu2 в FW); фон пунктов сайдбара гасится `--nav-bg-color: transparent` (layout.less).
- Типографика: `FluentText` c `Size300..900`/`As=TextTag.H1..H6` (таблица маппинга v4
  Typo → v5 Size — в `FluentV5MigrationPlan.md`, этап стилей); у `FluentLabel` НЕТ
  Color/Typo (уйдут в AdditionalAttributes молча).
- Иконки: `FluentIcon` default = `currentColor` (осознанно); акцент — точечно `Color`.
- Тёмная тема: только v5 `IThemeService` + CSS-хук `body[data-theme="dark"]`
  (dark-производные — `html:has(body[data-theme="dark"])`); `body.dark` мёртв.
- Селекторы (E2E/харнесс): у `fluent-button` нет внутреннего `<button>` (клик по хосту);
  `name` живёт на хосте `fluent-text-input`, настоящий `input` — в shadow (`[name=x] input`).
- Списочные: `FluentSelect`/`Autocomplete`/`Combobox` — два параметра типов `TOption`/`TValue`;
  `FluentOption` литералы `Value="@("...")"`; у `FluentCombobox` лямбды `OptionText`/`OptionValue`
  ОБЯЗАНЫ быть null-safe (`p => p?.Id ?? 0`) — v5 вызывает их с null (NRE при рендере).

## Грабли

- Админка — Blazor WASM: «логин/шелл» решается на клиенте ПОСЛЕ старта приложения;
  в харнессе ждать появления `.admin-layout, fluent-nav` или формы логина, проверка URL
  сразу после `domcontentloaded` — гонка (auth.json сохранялся пустым).
- `reboot.css` библиотеки грузится ПОСЛЕ `style.css` — правила на голый тег (`a`)
  перебиваются; повышать специфичность (`body a`).
- LESS: `*/` внутри css-комментария закрывает комментарий; голая интерполяция `@{param}`
  в значениях custom properties падает на less 4.1 (Web Compiler) — использовать
  `~"var(--x-@{param})"`; проверять компиляцией на less@4.1.3.
- `FluentGrid.Spacing` default в v5 = 0 (был 3) — молчаливое визуальное изменение.
- Удалённые v4-параметры не ловит сборка (splatting в AdditionalAttributes) — детектор
  `sweep.js`; структурные контракты (обёртки/слоты) не ловит ничто — assertions в crawl.
- FluentInputFile: при заданном `OnInputFileChange` встроенная загрузка отключается
  (авто-режим — делегат не задавать вовсе).
- В `<style>`-блоках внутри .razor текст css-комментариев парсится Razor'ом: угловые
  скобки (`<dialog>`, `<button>`) ломают сборку (RZ9980) — в комментариях писать без `<>`.
- IMask/Sortable завендорены в `src/Mars.Admin/wwwroot/js/` и подключены в `index.html`
  ДО monaco `loader.js` (AMD-loader иначе ломает UMD-глобалы v5).

## Отклонено

- Стилизация через v5 `Theme`-мутацию (`CreateCustomThemeAsync`) для стайлера — перебивается
  important-мостом на `html:root`; скаляры стайлера — CSS-переменные (см. план, шаг 3).
  Theme-мутация осталась только для brand-цвета через `IThemeService`.
- Переход на нейминг Fluent-токенов в нашем css — отклонён; источник истины `--mars-*`,
  Fluent натягивается мостом (план «Mars-токены главнее Fluent»).
- Нейминг `UiLayoutGuide.md`/`AdminUiGuide.md` для этого гайда — выбран `FluentUiGuide.md`.

## Рецепты

- **Новая страница с гридом**: провайдер с клампом `req.Count is > 0 ? req.Count : Default`;
  `ItemSize` = ожидаемая высота строки (заголовок не растянет — оверрайд в fluent-ui.less);
  после рендера — добавить страницу в `PAGES` crawl.js при необходимости.
- **Новое меню**: `FluentMenu` + `Trigger` + `FluentMenuList` внутри (образец —
  `XActionsDropDown` в FW).
- **Новый диалог**: компонент с `[CascadingParameter] IDialogInstance Dialog`, разметка
  `FluentDialogBody` (TitleTemplate/ChildContent/ActionTemplate); вызов через шим
  `DialogServiceCompatExtensions.ShowDialogAsync<T>`.
- **Проверка UI-правки**: `serve.ps1` → `probe.js` (факт) → `crawl.js` (регрессия) →
  точечные тесты фронта из `ai/TestingGuide.md`.
