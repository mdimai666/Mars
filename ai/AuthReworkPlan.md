# Реворк авторизации — план

Ветка: `ai/auth-rework`. Аудит пайплайна — 2026-09-25 (два Explore-прохода: сервер + админка).

## Решения (2026-09-25, пользователь)

1. **Целевая схема — вариант A1 («чистые куки»)**: браузеры (админка WASM, фронты с
   авторизованным рендером, Swagger UI) аутентифицируются стандартной Identity application
   cookie без JWT внутри; машинные клиенты — X-API-Key (уже реализован) и Bearer JWT.
   JWT в localStorage **не храним** (убирается в этапе 3).
2. **Сроки**: access JWT и cookie — 3 дня; cookie со `SlidingExpiration` (активная сессия
   продлевается, неактивная умирает через 3 дня → перелогин/пасскей). Refresh-поток
   **не делаем вовсе** — его роль выполняет sliding expiration; для машин — API-ключи.
   Отклонено: A2 (JWT+refresh в HttpOnly-куках) — велосипед поверх готового, браузеру
   нечего читать в своих куках.
3. **Impersonation** (вход админа от имени юзера, фича позже) — A1 совместим: серверный
   `SignInAsync(targetUser)` + claim `OriginalUserId` для возврата/аудита.
4. **Протухшие claims в куке** — освежает штатный SecurityStampValidator (дефолт 30 мин):
   админские правки пользователя обязаны вызывать `UpdateSecurityStampAsync` (чек-пойнт этапа 3).
5. **Rate limiting**: параметры по умолчанию, вынесенные в опцию БД (паттерн `PasskeyOption`).
   Покрыть: `/api/Account/Login`, `/api/Passkey/login`, `POST /api/openid-connect/auth`,
   `POST /api/oauth/token`. Вторым слоем — lockout Identity (сейчас логин через
   `CheckPasswordAsync`, счётчик неудач не инкрементируется никогда).
6. **Antiforgery отложен** (вернёмся в гигиене; до него единственная защита — SameSite=Lax).
7. **Порядок — по этапам, не забегая**; гигиену обсуждать по пунктам. Коммит — по команде.

## As-is (кратко, детали в аудите коммитов ветки)

- Логин (`AccountsService.Login`): `CheckPasswordAsync` (lockout мёртв) → JWT RS256 на **30 суток**
  (`JWTSettings.expiryInMinutes: 43200`) в теле + `SignInAsync` ставит cookie. Refresh-токен
  генерируется и выбрасывается (`RefreshToken = null`), эндпоинта refresh нет.
- Клиент: JWT в `localStorage['authToken']` в двух форматах (Blazored JSON + raw); attaching
  через `DefaultRequestHeaders`; fallback на несуществующую cookie `authToken` — мёртвый код;
  refresh в `CookieOrLocalStorageAuthStateProvider` закомментирован.
- Logout чисто клиентский; серверный `/api/Account/Logout` не вызывается; `CookieRemove`
  HttpOnly-куки из JS — no-op.
- `UseAntiforgery` закомментирован (`MarsWebAppStartup.cs`), `[ValidateAntiForgeryToken]` нет.
- `AddRateLimiter`/`UseRateLimiter` нет нигде. В `AccountController` висит `//TODO: rate limit`.
- «Smart»-схема (`MarsStartupPartCore.cs`): `X-API-Key` → ApiKey; `Authorization: bearer` →
  JwtBearer; иначе Identity cookie.
- Хабы `ChatHub` (`/_ws/admin`), `AiChatHub` — без `[Authorize]`; query `access_token` сервером
  не читается (нет `OnMessageReceived`).
- OAuth-модуль (SSO): client secret открытым `==`, refresh-токены в БД не хэшируются,
  отладочный `_db.AuthCodes.ToList()`.
- Несанитизированные `MarkupString`: Markdig в `FluentMarkdownSection`, имена Docker-сущностей
  в диалогах. CORS `AllowAnyOrigin().AllowAnyHeader()` (`//not check`). ASP0001 подавлен прагмой.

## Этап 1 — Rate limiting + lockout ✅ (2026-09-25)

- [x] Опция `AuthProtectionOption` (`Mars.Identity.Contracts/Options`, паттерн `PasskeyOption`):
      `Enabled` (true), окно/лимит запросов на IP, `LockoutMaxFailedAccessAttempts` (5),
      `LockoutDefaultLockoutTimeSpanMinutes` (5). Регистрация в `UseMarsIdentity`
      (`RegisterOption`, без initial site data — серверная).
- [x] `AddRateLimiter` в `AddMarsIdentity`: политика `auth` — fixed window, партиция по IP,
      лимиты читаются из опции на запрос; `Enabled=false` → NoLimiter.
- [x] `app.UseRateLimiter()` в `MarsWebAppStartup.ConfigureApp` (после `UseRouting`).
- [x] `[EnableRateLimiting("auth")]`: `AccountController.Login`, `PasskeyController` login,
      `OAuthPageController` POST auth, `OAuthHostController` POST token. Убрать `//TODO: rate limit`.
      (В SSO-контроллерах — литерал `"auth"`: Mars.SSO.Host.OAuth не ссылается на Identity.Contracts.)
- [x] Lockout: `AccountsService.Login` → `PasswordSignInAsync(user, pwd, true, lockoutOnFailure: true)`
      (заодно заменяет отдельный `SignInAsync`); `ValidateUserCredentials` (OAuth password grant) →
      `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`. Ответ при `IsLockedOut` — отдельное
      сообщение. Заодно удалена мёртвая генерация `GenerateRefreshToken()` в Login.
- [x] Lockout-настройки Identity применяются из опции в `UseMarsIdentity` (`ApplyLockoutSettings`
      мутирует singleton `IdentityOptions.Lockout` + `onChangeHook` на сохранение опции —
      обновление без рестарта).
- [x] Тесты: `tests/Mars.Integration.Tests/Services/AccountsServiceTests.cs` (7 шт. — lockout,
      неверный пароль, неизвестный юзер, успех, ValidateUserCredentials).
      Проверка: `Mars.Integration.Tests` 423/423 (4 skipped — Docker-гейт), build slnx зелёный.

Грабли этапа 1:
- `PasswordSignInAsync` учитывает `SignIn.RequireConfirmedAccount = true` (стоит в
  `MainDataInfrastructure`): все пути создания юзеров ставят `EmailConfirmed = true`
  (`SeedUsers.cs`, `UserMapping`, `RegisterUser`) — залочить существующих не должно,
  но при новых путях создания держать в голове.
- `ValidateUserCredentials` ищет только по username (в отличие от Login — name||email); не менять.

## Этап 2 — Logout ✅ (2026-09-25)

- [x] Клиент (`AuthenticationService.Logout`) вызывает серверный `POST /api/Account/Logout`
      (best-effort: `FlurlHttpException` глушится — сервер может быть недоступен, локальная
      сессия снимается в любом случае); мёртвый `CookieRemove('.AspNetCore.Identity.Application')`
      удалён вместе с `AdminJs.CookieRemove` и `d_cookie_remove` в `scripts.js`.
- [x] Сервер: с `AccountController.Logout` снят `[Authorize]` — иначе при протухшем Bearer
      (основной сценарий 401-перехватчика) smart-схема отдаёт 401 и cookie не снимается;
      `SignOutAsync` безопасен (удаляет только cookie вызывающего).
- [x] `IAccountServiceClient.Logout()` + реализация в `AccountServiceClient` (`AllowAnyHttpStatus`).
- [x] Тест: `LoginAccountTests.Logout_WithoutAuthorization_ReturnsOk` (WebApiClient.Integration.Tests,
      3/3 зелёные вместе с Login valid/invalid — логин теперь через `PasswordSignInAsync`).
- [x] Bump `MarsAppVersion` 0.8.3-alpha.30 (правка `scripts.js`).
- [x] E2E-лимиты: `E2EServerFixture.Seed` поднимает `RateMaxRequestsPerWindow` до 10000 через
      `SetOptionOnMemory` (браузерные тесты логинятся много раз с одного localhost; боевой
      дефолт 10/мин не меняется).

Грабли этапа 2:
- `AuthenticationService` больше не зависит от `AdminJs` (конструктор без него) — DI-регистрация
  `TryAddScoped<IAuthenticationService, AuthenticationService>` не затронута.
- SPA front template (`Res/front_templates/default/wwwroot/js/app.js`) всё ещё logout'ится
  удалением cookie через `document.cookie` — правится в этапе 3.

## Этап 3 — Переход на A1 (cookie-first) ✅ (2026-09-25, ждёт коммита)

- [x] Сервер: `JWTSettings.expiryInMinutes` 43200 → 4320 (3 дня) в `appsettings.json` —
      cookie `ExpireTimeSpan` следует той же настройке (`MarsStartupPartCore`), sliding уже включён.
- [x] Логин-эндпоинты продолжают возвращать JWT в теле (Swagger UI, WebApiClient, внешние
      клиенты) — браузерные клиенты его больше не сохраняют.
- [x] Клиент: `CookieOrLocalStorageAuthStateProvider` удалён → `CookieAuthStateProvider`:
      состояние из `InitialUserPrimaryInfo` server-rendered хост-страницы (для анонимуса сервер
      отдаёт null — `RequestContext.User`); сам грузит VM через `ViewModelService`
      (снимает гонку с `App.OnInitializedAsync`); `_loggedOut`-флаг до force-reload;
      Q.User обновляется через `UpdateUserByInitialVM`. Claims: NameIdentifier/Name/Email/
      GivenName/Surname/Role из `UserPrimaryInfo`, authenticationType "cookie".
- [x] `AuthenticationService`: убраны `LoginStage`/`LoginCallback`/`MarkUserAsAuthenticated`
      (интерфейс `IAuthenticationService` ужат до Login/Logout/RegisterUser), localStorage не
      трогается; Logout — серверный вызов + `MarkUserAsLoggedOut`.
- [x] Все навигации после смены сессии — `NavigateTo(..., forceLoad: true)`: LoginForm
      (пароль/пасскей/SSO-callback), LogoutPage, 401-перехватчик в App.razor.cs. Хост-страница
      перерендеривается с актуальной cookie.
- [x] `PasskeyJsInterop.js`: убран `authHeaders()`/Bearer из localStorage — только
      `credentials: 'include'` (cookie).
- [x] Хабы: `[Authorize]` на `ChatHub` и `AiChatHub`; у `AiChatHubClient` убран
      `AccessTokenProvider` (и зависимость IJSRuntime) — cookie приезжает в negotiate и
      WS-рукопожатии сама. Анонимный старт хаба со страницы логина теперь получает 401 и
      замолкает (без ретраев до успеха); после логина force-reload создаёт новое соединение.
- [x] SSO callback: внешний id_token в localStorage не кладётся ( force-reload после обмена;
      cookie ставит серверный `ExperimentalSignInService`).
- [x] Чек-пойнт SecurityStamp: УЖЕ закрыт — `UserRepository.Update` (:134), `SetRoles` (:410)
      и `RemoteUserUpsert` (:498) вызывают `UpdateSecurityStampAsync`. Правки не потребовались.
- [x] Мёртвый код: `GenerateRefreshToken` удалён из `ITokenService`/`TokenService`
      (вызывающих не осталось); fallback на cookie `authToken` и закомментированный refresh
      ушли вместе со старым провайдером.
- [x] SPA front template `app.js`: Login не пишет токен в localStorage; Logout — серверный
      `POST /api/Account/Logout` (fetch, credentials include) вместо document.cookie-хака.
- [x] Bump `MarsAppVersion` 0.8.3-alpha.31 (PasskeyJsInterop.js, app.js).
- [x] Проверка: build slnx зелёный; Mars.Integration.Tests — все зелёные (exit 0);
      `LoginAccountTests` 3/3; `HandlebarsAppFrontTests` (Docker) зелёные.
      UI-потоки (логин/логаут/пасскей/SSO в админке) — живая проверка пользователем;
      E2E-сьют по умолчанию выключен.

Грабли этапа 3:
- **Dev-стенд `Dev/DevAdmin.DevServer` (WASM на 5185 → backend 5003)**: cross-origin —
  Identity-cookie в запросы не попадёт (fetch не шлёт куки кросс-доменно без CORS
  `AllowCredentials` + `credentials: include`). Прод-путь (`/dev` с того же origin) работает.
  Если стенд ещё используется — чинить отдельно (CORS-политика с явным origin).
- `[Authorize]` на хабах: админка стартует `/_ws/admin` сразу (в т.ч. на странице логина) —
  анонимное соединение теперь падает с 401 (fire-and-forget, один раз, без ретраев);
  лечится force-reload после логина.
- После деплоя: существующие сессии на 30-дневных cookie/JWT живут до своего `exp`
  (cookie sliding перевыпускается со старым сроком, пока не перелогинятся); JWT на 30 суток
  остаются валидными до истечения — при желании разово перегенерировать `jwt_private.pem`.

## Этап 4 — Гигиена (по пунктам, каждый обсуждается отдельно)

- [ ] Antiforgery (отложен решением 2; включить `UseAntiforgery` + токены для cookie-мутаций).
- [ ] Санитизация `MarkupString` (Markdig `FluentMarkdownSection`, Docker-имена в диалогах).
- [ ] CORS: `AllowAnyOrigin` → явные origins + `AllowCredentials` для кросс-доменных фронтов.
- [ ] OAuth-модуль: хэш client secret, хэш refresh-токенов, убрать отладочный код,
      `RefreshTokenLifetimeDays` не используется — починить или удалить.
- [ ] ASP0001: разобраться с порядком middleware вместо прагмы.
- [ ] `GET /vm/ViewModel/InitialSiteDataViewModel` без `[Authorize]` — решить, норма ли.

## Верификация (точечная)

Сборка `dotnet build Mars.slnx` + тесты затронутых областей (Identity: `tests/Mars.Server.Tests` —
сервисы аккаунтов/lockout; на этапе 3 — затронутые клиенты и E2E логина при наличии).
Полный сьют не гонять.
