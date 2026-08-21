# План: админка (AppAdmin) — хостинг и модернизация

> **Статус: Фаза 0 выполнена 2026-08-21, prerender-механика удалена 2026-08-21.**
> Фаза 1 (ассеты) — ждёт решения о старте. Фаза 2 (Blazor Web App) — отклонена.
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
  `blazor.webassembly.js autostart="false"` + `Blazor.start({ loadBootResource })`,
  в продакшене вручную фетчит `.br` и разжимает JS-brotli.
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

## Фаза 1 — ассеты: нативная прекомпрессия и кеширование (ждёт старта)

### Проблема

- После `dotnet publish` прекомпрессия `.br`/`.gz` не раздаётся автоматически:
  `UseStaticFiles`/`UseBlazorFrameworkFiles` не делают Content-Encoding negotiation
  вне Development. Поэтому в `BlazorScriptsAppend.html` живёт кастомный
  `loadBootResource`: фетч `<файл>.br` + ручная JS-декомпрессия brotli-полифилом
  (`/mars/js/brotli.decode.min.js`, `fetch(cache:'no-cache')`).
  Минусы: медленнее нативного HTTP, нет браузерного кеширования бут-ассетов,
  лишний код и полифил.
- Статика отдаётся без `Cache-Control` → браузерный эвристический кеш (источник
  прошлых багов рассинхрона JS↔.NET; сейчас лечится bump MarsAppVersion + `?v=`).
- В .NET 10 удалён старый механизм кеширования бут-ресурсов `BlazorCacheBootResources` —
  замена ему fingerprinting.

### Вариант A (основной) — `MapStaticAssets` (.NET 9+)

Эндпоинт-раздача статических веб-ассетов с:
- negotiation `.br`/`.gz` по `Accept-Encoding` (нативная раздача, без JS),
- `Cache-Control: max-age=31536000, immutable` для fingerprinted-файлов,
- `no-cache` для остальных; поддержка вариантов по query-строке.

Открытые вопросы spike'а (проверить прототипом в publish-режиме):
1. Подхватывает ли `MapStaticAssets` ассеты `_framework` и `_content` в нашем сценарии
   (WASM-проект подключён референсом, не шаблонный hosted-кейс).
2. Работает ли внутри `MapWhen("/dev")`-ветки с base href `/dev/`
   (ассеты — endpoint'ы; ветка уже имеет свои `UseRouting`/`UseEndpoints`).
3. Не ломает ли rewrite `dev/_content/*` (rewrite middleware до эндпоинтов).
4. Совместимость с плагин-механикой `AppAdmin.staticwebassets.endpoints.json`
   (MapStaticAssets читает тот же манифест — проверить diff-логику PluginManifestProvider).
5. Как ведёт себя `UseWebAssemblyDebugging()`/hot reload в Development.

### Вариант B (запасной)

Если `MapStaticAssets` не срастается с подпутью/веткой — маленькая самописная
middleware Content-Encoding negotiation поверх `_framework`-каталога
(~50 строк, известный паттерн), либо статус-кво с JS-brotli.

### Шаг за шагом

1. Ветка-прототип: `app.MapStaticAssets()` (+ при необходимости в ветке `/dev`),
   `dotnet publish` Release, локальный запуск publish-сборки, проверка заголовков
   (`Content-Encoding: br`, `Cache-Control`) на `_framework/*` и `_content/*`.
2. Если ок: `BlazorScriptsAppend.html` упрощается до обычного подключения
   `blazor.webassembly.js` (обработчик ошибок загрузки можно оставить);
   brotli-полифил и `loadBootResource` удаляются.
3. Решить по `?v=`-версионированию (`ScriptFileInfo`): оставить как есть
   (MapStaticAssets умеет query-варианты) или перевести часть ассетов на fingerprinting.
4. Опционально, фичи standalone WASM из .NET 10: preload фреймворк-ассетов
   (`OverrideHtmlAssetPlaceholders` + `<link rel="preload">`),
   `WasmApplicationEnvironmentName` вместо заголовка `Blazor-Environment`.

### Риски

Подпуть + ветка MapWhen, плагин-ассеты, NOADMIN-сборка, дебаг/hot reload,
поведение в Development (там прекомпрессия и так раздавалась манифестом).

### Верификация

Сборка + `HandlebarsAppFrontTests`; для самого spike'а — publish-сборка + локальная
проверка заголовков и загрузки админки в браузере.

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
