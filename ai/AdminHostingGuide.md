# AdminHostingGuide — хостинг админки в `/dev`

Гайд для агента: как standalone Blazor WASM-админка смонтирована в подпуть `/dev`
хоста Mars, как отдаются ассеты. История инициативы (план схлопнут):
`git show 3b13b36a:ai/AppAdminPlan.md`. Смежные: `ai/CloudyGuide.md` (устройство UI),
`ai/CssRefactoringGuide.md`, `ai/ProjectStructureGuide.md`.

## Как устроено

- **Модель — standalone Blazor WASM в подпути**, НЕ Blazor Web App. Хост мульти-фронтовый:
  пользовательские сайты рендерит терминальная middleware `UseFront` (Handlebars),
  плюс setup-визард на Razor Pages, контроллеры, хабы.
- Ветка `/dev` — `MainAdmin.UseMarsAdmin` (`src/Admin/Mars.Admin.Host/MainAdmin.cs`):
  `MapWhen("/dev")` → rewrite `^dev/_content/(.*)` → `_content/$1` → `UseRouting`/
  `UseAuthorization` → `UseEndpoints(MapStaticAssets + MapFallbackToPage("/_AdminHost"))`
  → fallback `UseBlazorFrameworkFiles("/dev")` + `UseStaticFiles` для ассетов, которых
  нет в эндпоинтах (например `_framework` в Development).
- Rewrite `_content` неизбежен при base href `/dev/`: RCL-ассеты всегда лежат в корневом
  `_content`, а браузер резолвит относительные ссылки от base href.
- `<StaticWebAssetBasePath>dev</StaticWebAssetBasePath>` в `Mars.Admin.csproj` — wwwroot
  админки отдаётся под `/dev`.
- **Шелл — Razor Page `_AdminHost.cshtml`** (`Mars.Admin.Host/Pages/`, `@page ""` —
  routable только как fallback): title из опций, `<base href="/dev/">`, head/footer-скрипты
  через keyed `ISiteScriptsBuilder`, инлайн `InitialSiteDataViewModel()` (zip→base64 JSON),
  лоадер, blazor-error-ui. Razor-страницы модуля обнаруживаются только как
  `CompiledRazorAssemblyPart` (обычный `AddApplicationPart` маршруты страниц не отдаёт).
- Бут WASM: `blazor.webassembly.js autostart="false"` + `Blazor.start({ applicationCulture })`
  (embedded `BlazorScriptsAppend.html`); кастомного `loadBootResource`/JS-brotli НЕТ —
  сжатие отдаёт сервер.
- **Плагины считают свои фронтовые ассеты по `Mars.Admin.staticwebassets.endpoints.json`**
  (`PluginManifestProvider`); манифест копируется в publish target'ом — не ломать.
- Пререндера в админке НЕТ (механика удалена полностью; упоминания `BlazorPrerender`
  в `AppFrontMigration`/`FrontManagerTests` — миграция легаси-конфигов фронтов, оставлены).
- NOADMIN-режим: `#if NOADMIN` в `MainAdmin` — админка не подключается.

## Ассеты и кеширование

- `MapStaticAssets` в ветке после publish раздаёт прекомпрессированные `.br/.gz` по
  `Accept-Encoding` + fingerprinting (.NET 10 фингерпринтит WASM при публикации).
- **Грабля порядка middleware**: физические ассеты `/dev/*` отдаёт ГЛОБАЛЬНЫЙ
  `UseHostFiles` (`UseStaticFiles`) ДО ветки `/dev` — после publish файлы
  `wwwroot/dev/_framework/*` существуют физически. Поэтому:
  - `MapStaticAssets` в ветке реально подхватывает только переписанные `_content`-ассеты;
  - сжатие `_framework` в publish делает `UseResponseCompression` (динамически),
    а не прекомпрессированные файлы;
  - middleware `Cache-Control: no-cache` для `/dev/_framework` стоит ДО раздачи статики
    (в `UseMarsAdmin`), не в ветке — из ветки заголовок не появлялся (проверено).
  - `no-cache` + ETag = всегда ревалидация (дёшево, 304): без него браузер кеширует
    бут-ассеты эвристически и держит старую версию после деплоя.
- Версионирование скриптов — `?v=` + `MarsAppVersion` (`ScriptFileInfo`); после коммита
  css/js — bump версии в `Directory.Build.props`.

## Инварианты

- Поведение `/dev` застраховано тестами: `HandlebarsAppFrontTests` (админка не перехватывается
  фронт-fallback'ом, работает в режиме обслуживания) + E2E (`/dev/Login` и др.).
- `_AdminHost.cshtml` остаётся Razor Page — правильный инструмент для серверно-инжектируемого
  SPA-шелла (InitialSiteDataViewModel, auth-классы body, тёмная тема из cookie).
- Инлайн-стили лоадера в `_AdminHost.cshtml` — кандидат на перенос в `.less`
  (компиляция через `tools/ui/build-css.ps1`).

## Отклонено

- **Миграция на Blazor Web App (App.razor document shell)** — отклонена 2026-08-21:
  BWA предполагает, что приложение — центр роутинга хоста (у нас мульти-фронтовый хост);
  BWA не умеет жить в подпути `/dev` (PathBase только для всего приложения через reverse
  proxy); плагин-механика завязана на `staticwebassets.endpoints.json`; эффект для
  чисто-WASM админки мал, цена высока (бут, auth, InitialSiteDataViewModel, NOADMIN,
  DevServer, дебаг, E2E). Вернуться к обсуждению только если: админка станет гибридной
  (SSR/prerender частей), standalone WASM станет deprecated, или админку вынесут
  на отдельный домен целиком.
- Пререндер первого экрана — старая задумка, удалена везде (флаг никогда не ставился в true).
- JS-brotli полифил и кастомный `loadBootResource` — удалены (сжатие отдаёт сервер).

## Бэклог

- Preload фреймворк-ассетов .NET 10 (`OverrideHtmlAssetPlaceholders`) — требует
  плейсхолдеров в HTML-шелле, у нас шелл нестандартный.
- Перевод `?v=`-версионирования на fingerprinting — пока `?v=` работает,
  MapStaticAssets query-варианты поддерживает.
- Инлайн-стили лоадера → `.less`.