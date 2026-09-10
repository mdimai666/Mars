# План: админка (AppAdmin) — хостинг и модернизация

> **Статус: Фаза 0 ✅ и Фаза 1 ✅ выполнены 2026-08-21, prerender-механика удалена 2026-08-21.**
> Фаза 2 (Blazor Web App) — отклонена. Остатки: инлайн-стили лоадера в `_AdminHost.cshtml`
> перенести в `.less` (компилирует пользователь).
> Задача-источник: вопросы пользователя «правильно ли админка живёт в /dev со старых
> версий .NET» и «переходить ли на App.razor из Blazor Web App» (2026-08-21).

## Принятые решения (2026-08-21)

1. **Хостинг в `/dev` остаётся как есть.** `MapWhen("/dev")` + rewrite `dev/_content/*` →
   `_content/*` + `UseBlazorFrameworkFiles("/dev")` + fallback на Razor Page `_AdminHost.cshtml` —
   по-прежнему валидный паттерн «standalone Blazor WASM, смонтированный в подпуть»
   (проверено по release notes .NET 10: standalone WASM поддерживается,
   `UseBlazorFrameworkFiles` на месте, у standalone-модели появились свои новые фичи —
   клиентский fingerprinting, preload ассетов, boot-конфиг инлайнится в dotnet.js).
   Rewrite `_content` неизбежен при base href `/dev/`: RCL-ассеты всегда лежат в корневом
   `_content`, а браузер резолвит относительные ссылки от base href.
2. **Миграция на Blazor Web App (App.razor document shell) — не переходить.**
   - Хост Mars мульти-фронтовый: пользовательские сайты рендерит терминальная middleware
     `UseFront` (Handlebars), плюс setup-визард на Razor Pages, контроллеры, хабы.
     BWA предполагает, что приложение — центр роутинга хоста.
   - BWA не умеет жить в подпути `/dev` (подпуть поддерживается только для всего
     приложения целиком через PathBase за reverse proxy).
   - Плагин-механика завязана на `AppAdmin.staticwebassets.endpoints.json`
     (`PluginManifestProvider` считает diff ассетов плагинов).
   - Эффект для чисто-WASM админки мал (prerender первого экрана, enhanced navigation),
     цена высокая: переписать бут, auth, доставку `InitialSiteDataViewModel`,
     NOADMIN-режим, DevServer, дебаг, E2E.
3. **Пререндера в админке не будет — старая задумка, удалено везде (2026-08-21):**
   `Q.IsPrerenderProcess`, `App.IsPrerenderProcess`/`App.IsPrerenderSupport`, все ветки
   `if (!Q.IsPrerenderProcess)` в AppFront.Shared / AppFront.Main / AppAdmin, присвоение
   флага и атрибут `prerender` в `_AdminHost.cshtml`. Флаг нигде не устанавливался в true —
   удаление ничего не меняет в поведении. (Упоминания режима `BlazorPrerender` в
   `AppFrontMigration` и тесте `FrontManagerTests` — миграция легаси-конфигов фронтов,
   оставлены.)
4. **`_AdminHost.cshtml` остаётся Razor Page** — это правильный инструмент для
   серверно-инжектируемого SPA-шелла (InitialSiteDataViewModel, auth-классы body,
   тёмная тема из cookie). Code-behind пока не нужен. Инлайн-стили лоадера позже
   перенести в `.less` (компилирует пользователь, см. ai/CssRefactoringGuide.md).

## Как сейчас устроен хостинг (as-is)

- `MarsWebAppStartup.ConfigureApp`: `UseRouting` → `UseAuthentication` → `UseAuthorization`
  (глобальные, до ветки) → `app.UseDevAdmin()`.
- `StartupDevAdmin.UseDevAdmin`: `MapWhen("/dev")` → rewrite `dev/_content/(.*)` →
  `_content/$1` → `UseBlazorFrameworkFiles("/dev")` (`_framework` под подпутью) →
  `UseStaticFiles` (wwwroot AppAdmin отдаётся под `/dev` благодаря
  `<StaticWebAssetBasePath>dev</StaticWebAssetBasePath>`) → `UseRouting`/`UseAuthorization`
  в ветке → `MapFallbackToPage("/_AdminHost")` (у страницы `@page ""`, поэтому напрямую
  она не routable — только как fallback).
- `_AdminHost.cshtml`: title из опций, `<base href="/dev/" />`, head/footer-скрипты через
  keyed `ISiteScriptsBuilder` (`AppAdminSpaHtmlScripts`), инлайн-функция
  `InitialSiteDataViewModel()` с zip→base64 JSON, лоадер, blazor-error-ui.
- Бут WASM: `BlazorScriptsAppend.html` (embedded resource в Mars.Host) —
  `blazor.webassembly.js autostart="false"` + `Blazor.start({ applicationCulture })`
  (с Фазы 1 — без кастомного `loadBootResource`/JS-brotli; сжатие отдаёт сервер).
- Ограничение: плагины считают свои фронтовые ассеты по
  `AppAdmin.staticwebassets.endpoints.json` (копируется в publish-папку target'ом
  `CopyAppAdminStaticWebAssets`).
- Поведение `/dev` застраховано тестами: `HandlebarsAppFrontTests`
  (админка не перехватывается фронт-fallback'ом, работает в режиме обслуживания),
  E2E (`/dev/Login` и др.).

## Фаза 0 — чистка мёртвого кода ✅ (2026-08-21)

- Удалены остатки шаблона ранних версий: `Mars.WebApp/App.razor`, `Shared/`
  (SurveyPrompt, MainLayout, LoginDisplay, BlankLayout, RedirectToLogin, …),
  `Areas/Identity/` (79 файлов, уже был выключен из компиляции), `_Imports.razor`.
- Удалены `Pages/code.cshtml` (роут `/dev/monaco`, iframe-хост Monaco — единственный
  потребитель `MarsCodeEditor.razor` сам выключен из компиляции), `Pages/_LayoutBlank.cshtml`,
  `Pages/DisplayFF1.razor`/`EditFF1.razor`, `wwwroot/js/code.js`; из dev-`index.html`
  убрана ссылка на `Mars.styles.css` (бандл больше не генерируется — в Mars.WebApp
  не осталось .razor).
- `StartupDevAdmin.cs`: убран закомментированный код, пустой `AddDevAdmin()` и лишний
  `MapControllers()` в ветке (ни один контроллер не имеет маршрутов `/dev/*`).
- `_AdminHost.cshtml`: убраны мёртвый try/catch с rethrow, закомментированные
  `<component>`, атрибут `prerender`, `//window._dev`.
- Починены ссылки в csproj (исключения `Areas\Identity\**`, `Content Remove` удалённых
  razor-файлов, `<Folder Include="Areas\" />`, `None Include` удалённого code.js).

## Фаза 1 — ассеты: нативная прекомпрессия и кеширование ✅ (2026-08-21)

### Что сделано

1. **`MapStaticAssets()` добавлен в ветку `/dev`** (`StartupDevAdmin`): эндпоинты
   статических ассетов внутри ветки (`UseRouting` → `UseAuthorization` →
   `UseEndpoints(MapStaticAssets + MapFallbackToPage)`), после них fallback-середины
   `UseBlazorFrameworkFiles("/dev")` + `UseStaticFiles` для дев-ассетов, которых
   нет в эндпоинтах.
2. **JS-brotli удалён полностью**: `BlazorScriptsAppend.html` переписан на обычный
   `blazor.webassembly.js autostart=false` + `Blazor.start({ applicationCulture })`
   с минимальным экраном ошибки старта; удалены `loadBootResource`, brotli-полифил
   `wwwroot/mars/js/brotli.decode.min.js` и неиспользуемое свойство
   `BlazorSpaWasmHtmlScripts.Brotli`.
3. **Cache-Control для бут-ассетов**: middleware в `UseHostFiles` ставит
   `Cache-Control: no-cache` на ответы `/dev/_framework*` (см. грабли ниже).
4. Bump `MarsAppVersion` → 0.7.8-alpha.42.

### Грабли (важно для будущих правок пайплайна)

**Физические ассеты `/dev/*` отдаёт глобальный `UseHostFiles` ДО ветки `/dev`.**
После publish файлы `wwwroot/dev/_framework/*` и `wwwroot/dev/*` существуют физически,
и общий `app.UseStaticFiles()` (`UseHostFiles`) обслуживает их раньше, чем запрос
доходит до `MapWhen("/dev")`. Поэтому:
- `MapStaticAssets` в ветке реально подхватывает только переписанные `_content`-ассеты
  (`/dev/_content/*` → rewrite → `/_content/*`, физически их в `wwwroot/dev/` нет);
- сжатие `_framework` в publish делает `UseResponseCompression` (динамически,
  BrotliCompressionProvider уже зарегистрирован в `MarsStartupPartCore`),
  а не прекомпрессированные файлы;
- middleware заголовков для `/dev/_framework` должна стоять ПЕРЕД `UseHostFiles`,
  а не в ветке (эксперимент подтвердил: из ветки заголовок не появлялся).
В Development `_framework` отдаёт манифест статических ассетов через тот же
глобальный `UseStaticFiles`.

### Проверено

- **Publish (Release)**: `Content-Encoding: br` по `Accept-Encoding` на
  `/dev/_framework/dotnet.js` (+ вариант gzip), на fingerprinted
  `AppAdmin.<hash>.wasm` (.NET 10 фингерпринтит WASM при публикации),
  на `/dev/_content/*` (через rewrite) и `/dev/css/*`; `Cache-Control: no-cache`
  на `_framework`; `Vary: Accept-Encoding`; ETag; HTML `/dev/` без brotli,
  SPA-fallback `/dev/settings` → 200; плагины не сломаны
  (манифест `Mars.staticwebassets.endpoints.json` на месте).
- **Development**: все ассеты `/dev` → 200, `_framework` с `Cache-Control: no-cache`.
- **Регрессия**: сборка + `HandlebarsAppFrontTests` 17/17.

### Не вошло (возможные продолжения)

- Preload фреймворк-ассетов .NET 10 (`OverrideHtmlAssetPlaceholders`) — требует
  плейсхолдеров в HTML-шелле, у нас шелл `_AdminHost.cshtml` нестандартный.
- Перевод `?v=`-версионирования (`ScriptFileInfo`) на fingerprinting — пока
  `?v=`+`MarsAppVersion` работает, MapStaticAssets query-варианты поддерживает.

## Фаза 2 — Blazor Web App: отклонено

Вернуться к обсуждению, только если появятся новые основания:
- админка станет гибридной (SSR-страницы логина/setup-визарда, prerender частей админки
  ради первого экрана) — тогда BWA-модель начнёт окупаться;
- standalone WASM станетdeprecated (в .NET 10 признаков этого нет);
- появится возможность вынести админку на отдельный домен/подпуть целиком
  (reverse proxy + PathBase) — тогда BWA станет штатным сценарием.

## Связанные документы

- `ai/CssRefactoringGuide.md` — CSS админки (токены, .less-workflow).
- `ai/ProjectDescription.md` — общее описание платформы.
