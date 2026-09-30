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
pwsh -NoProfile -File tools/ui/serve.ps1 -Port 5003 # (в копии master-репо 2026\Mars — v4-база для diff)
```

Порт задаётся env `Urls` (не `--urls`/ASPNETCORE_URLS — их перебивает CliSocketServer).

## Скрипты

| Скрипт | Назначение |
|---|---|
| `crawl.js` | Обход 35 страниц: консоль-ошибки + DOM-assertions из `assertions.json`, диалог ноды (dblclick/ESC). Exit 1 при падениях. `--shots --out <dir>` — скриншоты. |
| `probe.js` | Точечный замер: `--page /dev/Post --sel ".fluent-data-grid th"` → computed-стили + rect (JSON в stdout). `--vars --filter mars` — CSS-переменные элемента. |
| `tokens.js` | Дамп всех CSS-переменных темы с `<html>` (inline + computed + adoptedStyleSheets). Снапшот/diff тем. |
| `diff.js` | Pixel-diff двух папок скриншотов (`--a out-v4 --b out-v5`), числовой отчёт + diff-картинки в `diff/`. |
| `sweep.js` | Grep по src/docs/devstands на мёртвые v4-параметры (список — из `ai/FluentV5Reference.md`, блок `sweep-dead-params`, иначе встроенный). Только репорт. |

Примеры:

```
node crawl.js --host http://localhost:5005
node probe.js --page /dev/Post --sel body --sel ".fluent-data-grid th"
node tokens.js --filter "colorNeutralBackground"
node sweep.js
```

## Типовой цикл правки стилей

1. `probe.js` — замер факта (какой токен/правило виноваты).
2. Правка `.less` (ТОЛЬКО less, см. `ai/CssRefactoringGuide.md`).
3. Компиляция: `npx --package less@4.1.3 lessc --source-map-map-inline style.less style.css` + BOM (рецепт-исключение для визуального цикла).
4. `probe.js` / `crawl.js` — перемер. После коммита css/js — bump `MarsAppVersion`.

Отчёты пишутся в `tools/ui/out/` (не в git).
