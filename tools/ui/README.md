# tools/ui — харнесс визуальной проверки админки (FluentUI v5)

Playwright-core + системный Edge (channel `msedge`, браузеры не скачиваются).
Конвенция: **агент работает без скриншотов** — assertions и computed-style-замеры;
скриншоты (`--shots`) — только для пользователя. Гайд по вёрстке: `ai/FluentUiGuide.md`.

## Установка (разово)

```
cd tools/ui && npm install
```

## Auth (разово)

Создать `tools/ui/auth.local.json` (не в git): `{"user":"<login-email>","pass":"<password>"}`
или задать env `MARS_UI_USER` / `MARS_UI_PASS`. После первого логина скрипты сохраняют
`auth.json` (storageState, тоже не в git) и дальше ходят по нему.

## Сервер

```
pwsh -NoProfile -File tools/ui/serve.ps1            # v5-приложение, порт 5005
pwsh -NoProfile -File tools/ui/serve.ps1 -Watch     # dotnet watch: auto-rebuild+restart на правки
pwsh -NoProfile -File tools/ui/serve.ps1 -Port 5003 # (в копии master-репо 2026\Mars — v4-база для diff)
```

Порт задаётся env `Urls` (не `--urls`/ASPNETCORE_URLS — их перебивает CliSocketServer).

`-Watch` убирает ручной цикл «остановить → пересобрать → запустить»: любая правка
C#/razor/css вызывает автоматическую пересборку+перезапуск (watch сам останавливает
приложение перед сборкой — конфликта занятых сборок нет), браузер пользователя
обновляется browser-refresh middleware. `--non-interactive` — без вопросов о перезапуске.

По умолчанию watch работает с `--no-hot-reload` — проверено 2026-11-12 на WASM-админке:

- hot reload deltas доходят только до ЖИВОГО браузера; свежая загрузка страницы
  (crawl/probe/responsive открывают новый контекст) берёт старые WASM-сборки;
- scoped `.razor.css` referenced-проекта watch не применяет вовсе
  (`Scoped CSS bundle file 'Mars.Admin.bundle.scp.css' not found`) и не пересобирает.

Поэтому для харнесса надёжен auto-rebuild. `-Watch -HotReload` — opt-in deltas-режим
(быстрее для живого браузера, но харнесс видит старое до пересборки). Правки `.less`
watch не компилирует — `build-css.ps1` по-прежнему отдельный шаг (существующий
`style.css` отдаётся на лету).

**Грабля:** watch может молча не заметить правку. Ждать реакции в логе
(`File changed`/`Exited`) ≤10 секунд; нет реакции → перезапустить watch.
Сама пересборка WASM после реакции — ~40–60 с.

## Скрипты

| Скрипт | Назначение |
|---|---|
| `crawl.js` | Обход 35 страниц: консоль-ошибки + DOM-assertions из `assertions.json`, диалог ноды (dblclick/ESC), контекстное меню дерева front editor (right-click/ESC, геометрия попапа). Exit 1 при падениях. `--shots --out <dir>` — скриншоты. |
| `responsive.js` | Прогон страниц по viewport'ам `[1600,1200,900,768,390]`: нет горизонтального overflow, ключевые контролы досягаемы (scrollIntoView + hit-test), iframe ненулевой. Контролы — `responsive.json`. Exit 1 при падениях. |
| `probe.js` | Точечный замер: `--page /dev/Post --sel ".fluent-data-grid th"` → computed-стили + rect (JSON в stdout). `--vars --filter mars` — CSS-переменные элемента. |
| `tokens.js` | Дамп всех CSS-переменных темы с `<html>` (inline + computed + adoptedStyleSheets). Снапшот/diff тем. |
| `diff.js` | Pixel-diff двух папок скриншотов (`--a out-v4 --b out-v5`), числовой отчёт + diff-картинки в `diff/`. |
| `sweep.js` | Grep по src/docs/devstands на мёртвые v4-параметры (список — из `ai/FluentV5Reference.md`, блок `sweep-dead-params`, иначе встроенный). Только репорт. |
| `build-css.ps1` | Компиляция `style.less` → `style.css` (lessc 4.1.3 + BOM, оба входа: Mars.Admin и FormEditor). `-Check` — детектор рассинхрона less↔css (exit 1 если style.css устарел), ничего не пишет. |

Примеры:

```
node crawl.js --host http://localhost:5005
node responsive.js --host http://localhost:5005
node responsive.js --pages /dev/Post,/dev/tech/frontend/editor/admin --widths 1600,768,390
node probe.js --page /dev/Post --sel body --sel ".fluent-data-grid th"
node tokens.js --filter "colorNeutralBackground"
node sweep.js
```

## Геометрические assertions (crawl.js)

В `assertions.json` у `box`-проверок, кроме min/max-размеров, есть геометрия:

```json
{ "name": "toolbar within viewport", "selector": ".fe-toolbar", "box": { "withinViewport": true } }
{ "name": "panels do not overlap", "selector": ".files-panel", "box": { "noOverlap": [".editor-panel", ".stage"] } }
```

- `withinViewport` — бокс целиком в viewport (допуск 1px). Ловит «уехавшие» попапы
  (баг-образец: контекстное меню открывалось на top=1252 при viewport 1000px).
- `noOverlap` — нет пересечения (>1px по обеим осям) с элементами других селекторов;
  сам элемент и его предки/потомки пропускаются.
- Интерактивные проверки геометрии: панель ноды (`--skip-nodes` чтобы пропустить) и
  контекстное меню дерева front editor (`--skip-front-menu`).

## Responsive-прогон (responsive.js)

Для каждой страницы × ширина из `[1600,1200,900,768,390]` (высота 1000, на <600 — 844):

1. нет горизонтального overflow (`scrollWidth <= innerWidth + 1`);
2. ключевые контролы досягаемы: видны, скроллятся в центр viewport, бокс в
   горизонтальных границах, hit-test центра не перекрыт другим элементом;
3. iframe ненулевой (превью front editor).

Контролы — `responsive.json` (`all` + `pages` с glob-ключами, как в assertions.json).
Элемент контрола: строка-селектор или `{ "sel": "...", "minWidth": 901, "optional": true }`
(`minWidth` — не проверять уже заданной ширины, для контролов, скрытых на брейкпоинтах).
Страницы по умолчанию — репрезентативный набор (шелл, списки, профиль, tech-зона,
front editor, галерея); `--pages` переопределяет. Отчёт: `out/responsive-report.json`.

## Типовой цикл правки стилей

1. Сервер: `serve.ps1 -Watch` (hot reload; ручной перезапуск не нужен).
2. `probe.js` — замер факта (какой токен/правило виноваты).
3. Правка `.less` (ТОЛЬКО less, см. `ai/CssRefactoringGuide.md`).
4. Компиляция: `pwsh -NoProfile -File tools/ui/build-css.ps1 [-Entry admin|formeditor]`
   (или вручную по рецепту `ai/CssRefactoringGuide.md` — результат идентичен).
5. `probe.js` / `crawl.js` — перемер; при правках раскладки — `responsive.js`.
   После коммита css/js — bump `MarsAppVersion`.

Отчёты пишутся в `tools/ui/out/` (не в git).
