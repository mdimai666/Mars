# Авторизация Mars — гайд подсистемы

Как устроена аутентификация/авторизация после реворка 2026-09 (cookie-first).
История работ: `git show 9a53b9f7:ai/AuthReworkPlan.md`. Смежные: `ai/PasskeyGuide.md`
(WebAuthn), `ai/ApiKeyGuide.md` (X-API-Key).

## Каналы аутентификации (smart-схема)

Policy-схема в `src/Mars.WebApp/UseStartup/MarsParts/MarsStartupPartCore.cs`:
заголовок `X-API-Key` → схема `ApiKey`; `Authorization: bearer` → `JwtBearer`; иначе →
Identity cookie (`.AspNetCore.Identity.Application`).

- **Браузеры** (админка WASM, фронты, Swagger UI) → только Identity-cookie (схема A1,
  «чистые куки» без JWT внутри). Кука ставится `SignInAsync` на всех путях логина.
- **Машины** (CLI, внешние API-клиенты) → X-API-Key и Bearer JWT.
- JWT по-прежнему возвращается в теле логин-ответов (Swagger/внешние клиенты) — браузерные
  клиенты его **не хранят** (localStorage не используется для токенов).

## Сроки жизни

- `JWTSettings.expiryInMinutes` (`src/Mars.WebApp/appsettings.json`) = 4320 (3 дня) — из него
  следуют и срок JWT (`TokenService`), и cookie `ExpireTimeSpan` (`MarsStartupPartCore`).
- Cookie: `SlidingExpiration = true` — активная сессия продлевается, неактивная умирает через
  3 дня → перелогин/пасскей. **Refresh-потока нет и не должно быть** (см. «Отклонено»).

## Свежесть claims в живой сессии

Проблема: cookie хранит principal, снятый при логине; sliding-перевыпуск его не освежает.
Решение — два слоя:

- **Мгновенный (кэш)**: `ISecurityStampCache` (`Mars.Identity.Abstractions/Services`, реализация
  `SecurityStampCache` на IMemoryCache в `Mars.Identity.Host/Services`, TTL 24 ч). Все точки
  обновления stamp (`UserRepository.Update`, `UpdateUserRoles`, `RemoteUserUpsert`) пишут
  `Mark(userId, newStamp)`. Валидация cookie (`CookiePrincipalValidator`, см. ниже) на КАЖДЫЙ
  запрос сверяет stamp из куки с кэшем (без БД): не совпал → principal пересобирается из БД
  (`CreateUserPrincipalAsync`) и кука перевыпускается (`ShouldRenew`); юзера нельзя впускать
  (`CanSignInAsync` false / не найден) → `RejectPrincipal` + SignOut. Запись НЕ удаляется при
  перевыпуске — сравнивается stamp, поэтому идемпотентна для всех устройств юзера.
- **Бэкстоп (БД)**: штатный `SecurityStampValidator` со сверкой раз в `ValidationInterval`
  (`AuthProtectionOption.SecurityStampValidationIntervalMinutes`, дефолт 30, 0 = на каждый
  запрос; применяется мутацией singleton `SecurityStampValidatorOptions` в `UseMarsIdentity`
  + onChangeHook). Покрывает промахи кэша: рестарт, прямой edit БД, другой инстанс
  (IMemoryCache per-process).

Итого: смена ролей/данных применяется на следующем же запросе юзера; блокировка — тоже.
Push через SignalR (обновление без запроса) — в бэклоге.

## Логин/логаут — поток

- `POST /api/Account/Login` → `Mars.Identity.Host/Controllers/AccountController.cs` →
  `Services/AccountsService.cs`: `PasswordSignInAsync(user, pwd, isPersistent: true,
  lockoutOnFailure: true)` (lockout и `CanSignInAsync` — активны), затем JWT в теле.
  Поиск юзера: по name, фолбэк email. `ValidateUserCredentials` (OAuth password grant) —
  `CheckPasswordSignInAsync(..., lockoutOnFailure: true)`.
- Серверный logout: `POST /api/Account/Logout` — **сознательно без `[Authorize]`** (протухший
  bearer не должен мешать снять cookie; `SignOutAsync` удаляет только cookie вызывающего).
- Клиент админки: `src/Admin/Mars.Admin.Framework/AuthProviders/AuthenticationService.cs` —
  Logout best-effort (FlurlHttpException глушится), состояние снимает
  `CookieAuthStateProvider.MarkUserAsLoggedOut`.
- **Любая смена сессии на клиенте = `NavigateTo(..., forceLoad: true)`** (логин пароль/пасскей/
  SSO, logout, 401-перехватчик в `src/Mars.Admin/App.razor.cs`): хост-страница
  `_AdminHost.cshtml` перерендеривается сервером с актуальной cookie.

## Состояние авторизации в админке (WASM)

- `src/Admin/Mars.Admin.Framework/AuthProviders/CookieAuthStateProvider.cs`: состояние =
  `InitialUserPrimaryInfo` из server-rendered VM (`_AdminHost.cshtml` → JS-глобал
  `InitialSiteDataViewModel`). Аноним → `UserPrimaryInfo == null`
  (`Mars.Admin.Host/Handlers/InitialSiteDataViewModelHandler.cs`, `RequestContext.User` null).
  Провайдер сам грузит VM через `ViewModelService.GetLocalInitialSiteDataViewModel()`
  (снимает гонку с `App.OnInitializedAsync`). Claims строятся из `UserPrimaryInfo`
  (Id/Username/Email/FirstName/LastName/Roles), authenticationType "cookie".
- Remote-fallback VM: `GET /vm/ViewModel/InitialSiteDataViewModel` — сознательно публичный
  (те же данные, что в host-странице).
- Токенов на клиенте нет: `PasskeyJsInterop.js` — fetch с `credentials: 'include'`;
  Flurl/WebApiClient — same-origin, cookie уходит браузером сама; `DefaultRequestHeaders`
  для auth не используется.

## Rate limiting + lockout

- Опция `AuthProtectionOption` (`src/Mars.Modules/Mars.Identity.Contracts/Options/`):
  Enabled, окно/лимит на IP (дефолт 60 с / 10), lockout-параметры (5 попыток / 5 минут).
  Регистрация в `UseMarsIdentity` (`Mars.Identity.Host/MainIdentity.cs`), там же
  `ApplyLockoutSettings` мутирует singleton `IdentityOptions.Lockout` + onChangeHook
  (смена опции в админке действует без рестарта).
- Политика лимитера `auth` (const `AuthProtectionOption.RateLimitPolicyName`): fixed window,
  партиция по `Connection.RemoteIpAddress`, лимиты читаются из опции на каждый запрос,
  `Enabled=false` → NoLimiter. `AddRateLimiter` — в `AddMarsIdentity`, `app.UseRateLimiter()` —
  после `UseRouting` (`MarsWebAppStartup.cs`).
- `[EnableRateLimiting]` на: `Account/Login`, `Passkey/login`, `POST /api/openid-connect/auth`
  (OAuthPageController), `POST /api/oauth/token` (OAuthHostController). В SSO-модуле — литерал
  `"auth"` (Mars.SSO.Host.OAuth не ссылается на Identity.Contracts).

## OAuth-сервер (Mars как IdP, Mars.SSO.Host.OAuth)

- Клиенты — из опции `OpenIDServerOption.OpenIDClientConfigs` (БД), маппинг в
  `Services/InMemoryClientStore.cs`. `ClientSecretHash` = SHA-256 base64 (контракт
  `ApiKeyFormat.HashSecret` из Identity.Abstractions); plaintext показывается админу ОДИН раз
  при генерации (`Admin/Mars.Admin.Framework/OptionEditForms/OpenIDServerOptionEditForm.razor`).
  Сравнение — `FixedTimeEquals` (`Models/OAuthClient.VerifySecret`).
- Refresh-токены: в `RefreshToken.TokenHash` хранится только SHA-256; поиск/ротация/отзыв по
  хэшу (`Services/OAuthService.cs`). Срок — `RefreshTokenLifetimeDays` из опции клиента.
  `SsoAuthDbContext` — **InMemory** (`MainOAuth.cs`): коды и refresh-токены не переживают рестарт.
- Password grant оставлен сознательно (закрыт rate limit + lockout).

## CORS

- `CorsOption.AllowedOrigins` (`src/Server/Mars.Server.Contracts/Options/`, регистрация в
  `MainServer.UseMarsServerOptions`) + динамический провайдер
  `src/Mars.WebApp/UseStartup/MarsParts/OptionCorsPolicyProvider.cs` (на каждый запрос).
- Дефолт (пустой список) = только same-origin, кросс-origin запрещены. Перечисленным origins —
  any header/method + `AllowCredentials` (куки). Same-origin запросы CORS не затрагивает.

## CSRF

Antiforgery **сознательно не включён** (`UseAntiforgery` закомментирован в
`MarsWebAppStartup.cs`). Защита — `SameSite=Lax` (дефолт Identity cookie): кросс-site
POST/fetch куку не отправляют. См. инварианты.

## SignalR

- `ChatHub` (`src/Mars.Nodes/Mars.Nodes.Abstractions/Hubs/`, маппинг `/_ws/admin` в
  `Mars.Admin.Host/MainAdmin.cs`) и `AiChatHub` (`Mars.AiChat.Host/Hubs/`) — `[Authorize]`;
  cookie приходит в negotiate и WS-рукопожатии (same-origin). Query `access_token` сервером
  не читается и не нужен.
- `AiChatHubClient` (`Mars.AiChat.Front/Services/`) — без `AccessTokenProvider`.

## Тесты

- `tests/Mars.Integration.Tests/Services/AccountsServiceTests.cs` — lockout/логин на NSubstitute
  (partial subs UserManager/SignInManager; **обязателен `DoNotCallBase`** перед стаббингом
  `PasswordSignInAsync`/`CheckPasswordSignInAsync`, иначе base-реализация портит last-call).
- `tests/Mars.Integration.Tests/Modules/SSO/OAuthProviderTests.cs`, `OAuthLoginPageTests.cs` —
  клиент регистрируется с `ClientSecretHash = ApiKeyFormat.HashSecret(plaintext)`.
- `tests/Mars.Integration.Tests/Services/SecurityStampCacheTests.cs` — mark/try-get кэша stamp.
- `tests/Mars.WebApiClient.Integration.Tests/Tests/Accounts/RoleRefreshTests.cs` — смена ролей
  применяется к живой cookie-сессии на следующем запросе (без перелогина).
- `tests/Mars.WebApiClient.Integration.Tests/Tests/Accounts/LoginAccountTests.cs` — login/logout
  на живом хосте.
- E2E: `E2EServerFixture.Seed` поднимает `RateMaxRequestsPerWindow` до 10000 через
  `SetOptionOnMemory` (браузерные тесты логинятся много раз с одного localhost).

## Грабли

- **`ConfigureApplicationCookie` пересоздаёт `options.Events` целиком** (`MarsStartupPartCore.cs`) —
  штатная привязка Identity `OnValidatePrincipal = SecurityStampValidator.ValidateAsync` при этом
  ТЕРЯЕТСЯ (до 2026-09 stamp-валидация cookie в Mars не работала вовсе). Сейчас в Events явно
  прописан `CookiePrincipalValidator.ValidateAsync`, который внутри сам вызывает
  `SecurityStampValidator.ValidateAsync<ISecurityStampValidator>`. При любых правках Events —
  не терять OnValidatePrincipal.
- **`UserRepository.UpdateUserRoles`**: нельзя мешать ручную tracked-загрузку `Include(s => s.Roles)`
  с Identity role-API (`AddToRolesAsync`/`RemoveFromRolesAsync`) — конфликт трекинга
  `UserRoleEntity`/`UserEntity` и duplicate key. Используется внутренний `UpdateRoles`
  (прямая работа с `entity.Roles` + нормализация имён), как в `Update`.
- Имена ролей в claims — как в БД (`RoleEntity.Name`), сравнения в тестах/логике — case-insensitive.
- В TestServer `Connection.RemoteIpAddress` = null → все запросы в одной партиции лимитера
  («unknown»): серии login-тестов могут упереться в лимит.
- `PasswordSignInAsync` уважает `CanSignInAsync` (`SignIn.RequireConfirmedAccount = true` в
  `MainData.Infrastructure/MainDataInfrastructure.cs`) — все пути создания юзеров обязаны
  ставить `EmailConfirmed = true` (SeedUsers, CLI UserMapping, RegisterUser — уже так).
- Dev-стенд `Dev/DevAdmin.DevServer` (WASM 5185 → backend 5003): cross-origin, cookie не
  отправляется — авторизация в этом режиме не работает; прод-путь `/dev` same-origin.
- Анонимный старт `/_ws/admin` со страницы логина — разовый 401 без ретраев, это норма.
- Legacy: JWT, выданные до реворка (30 суток), живут до своего `exp`; обнулить — удалить
  `data/jwt_private.pem` (сгенерируется новый ключ). Plaintext-секреты OAuth-клиентов в опции
  не работают — перегенерировать в админке.
- `Mars.xml` (WebApp) — генерируемый артефакт, коммитить без разбора.

## Инварианты

- Мутации только POST/DELETE (никогда GET) + `SameSite` cookie не ослаблять — на этом
  держится CSRF-защита без antiforgery.
- Никаких токенов в localStorage/JS-читаемых куках; смена сессии = full reload (forceLoad).
- Логин-пути обязаны ставить cookie через SignInManager и не возвращать refresh-токенов.
- Правки пользователей (роли, данные, блокировка) обязаны обновлять security stamp И писать
  `ISecurityStampCache.Mark` (иначе мгновенного обновления прав не будет, только 30-мин бэкстоп).
- Секреты (client secret, refresh-токены, API-ключи) хранятся только как SHA-256
  (`ApiKeyFormat.HashSecret`), сравнение — `FixedTimeEquals`.

## Отклонено (не пересматривать без причины)

- **A2 (JWT + refresh в HttpOnly-куках)** — велосипед поверх Identity-cookie; браузеру нечего
  читать в своих куках.
- **Refresh-поток для браузеров** — его роль выполняет sliding expiration; для машин — API-ключи.
- **Полный antiforgery** — отложен; Lax закрывает кросс-site мутации. Вернуться, если появятся
  cross-origin фронты на куках (в связке с CorsOption).
- **SameSite=Strict** — ломает SSO (после редиректа от IdP первая навигация без куки).
- **AngleSharp/HtmlSanitizer в админке** — вместо него `.DisableHtml()` в Markdig
  (`FluentMarkdownSection`) + `HtmlEncode` интерполяций в диалогах (Docker, QueryWorkspace).
- **Миграция plaintext-секретов OAuth в хэши** — ломаем без миграции (решение пользователя).

## Что ещё не сделано

- **Impersonation** (вход админа от имени юзера — заказан, дизайн согласован): эндпоинт
  `[Authorize(Roles=admin)]` → `SignInAsync(targetUser)` + claim `OriginalUserId`/
  `IsImpersonating` для возврата и аудита.
- Push-обновление сессии через SignalR (применять права без ожидания следующего запроса).
- Antiforgery — см. «Отклонено» (вернуться при cross-origin фронтах).
