# План: Passkey (WebAuthn) — вход и управление

Ветка: `ai/user-passkey` (отпочкована от `ai/user-xapi-key`).

**Статус 2026-09-25: реализовано** — этапы 1–7 выполнены и закоммичены в ветке,
сборка `Mars.slnx` чистая, PasskeyServiceTests — 11 зелёные. Осталось: ручная проверка церемоний
на localhost (Windows Hello) и ревью/merge; при закрытии инициативы — свернуть план в гайд
по `ai/PlanLifecycleGuide.md`.

## Решения (согласованы с пользователем 2026-09-25)

- База — встроенная поддержка пасскеев в ASP.NET Core Identity 10 (`SignInManager`/`UserManager`,
  пакет уже 10.0.11): `MakePasskeyCreationOptionsAsync(PasskeyUserEntity)`, `PerformPasskeyAttestationAsync(credentialJson)`,
  `MakePasskeyRequestOptionsAsync(user?)`, `PerformPasskeyAssertionAsync` / `PasskeySignInAsync`,
  `AddOrUpdatePasskeyAsync`, `GetPasskeysAsync`, `RemovePasskeyAsync`. Сторонние WebAuthn-библиотеки НЕ нужны.
- БД уже готова: `UserPasskeyEntity : IdentityUserPasskey<Guid>` (+ CreatedAt/ModifiedAt/Disabled),
  `MarsDbContext : IdentityDbContext<…, UserPasskeyEntity>` — `IUserPasskeyStore` реализован встроенным
  EF-стором, таблица `user_passkeys` в миграциях есть. `UserPasskeyInfo`: CredentialId(byte[]), Name,
  CreatedAt, SignCount, Transports, IsUserVerified/IsBackupEligible/IsBackedUp, AttestationObject, ClientDataJson.
- Scope: **логин по пасскею + управление** (добавить/переименовать/удалить) в Mars.Admin.
  Passwordless-регистрация и conditional UI — не в этот раз (см. «Продолжения»).
- Поле `Disabled` не реализуем: удаление — единственная операция (встроенный стор Disabled не учитывает).
- `ServerDomain` (Relying Party ID) — из хоста `SiteSettings.SiteUrl` (уже используется для JWT issuer),
  конфигурируется в `MainIdentity.AddMarsIdentity`. Читается при старте — смена RP ID всё равно
  инвалидирует зарегистрированные пасскеи, динамика здесь вредна. SiteUrl пуст → ServerDomain не задаём
  (фолбэк на Host header).
- Лимит пасскеев: **новый класс `PasskeyOption`** в Mars.Identity.Contracts (`PasskeysMaxPerUser`, default 10),
  регистрация через `IOptionService.RegisterOption<PasskeyOption>(appendToInitialSiteData: true)` в `UseMarsIdentity`
  (паттерн MainMedia). Добр. 2026-09-25: флаг `Enabled` (default true) — глобальное разрешение пасскеев:
  серверный гейт в PasskeyService (registration/login), клиентский — кнопка LoginForm и PasskeysPage;
  форма настроек `PasskeyOptionEditForm.razor` (OptionEditForms, паттерн ApiOptionEditForm).
- Эндпоинты: **один `PasskeyController`** (`api/passkey`, Mars.Identity.Host): управление под `[Authorize]`,
  логин-пара (`request-options`, `login`) с `[AllowAnonymous]`. Контракты в Mars.Identity.Contracts/Passkeys/.
- Клиент: **отдельный JS-модуль** `PasskeyJsInterop.js` + обёртка `PasskeyJs` в Mars.Admin.Framework
  (по образцу AdminJs/AdminJsInterop.js, DI — TryAddScoped в MainAdminFramework). Церемонии
  (options → navigator.credentials → submit) идут **целиком в JS** через fetch `credentials: 'include'`
  + заголовок `Authorization: Bearer <token из localStorage>` (паттерн .NET 10 из доков MS — `headers`
  в createCredential/requestCredential); list/rename/delete — через WebApiClient (Flurl, `client.Passkey`).
- Результат passkey-логина — `AuthResultResponse` с JWT (`ITokenService.CreateAccessToken`), клиент
  переиспользует `AuthenticationService.LoginCallback/LoginStage` (паттерн SSO-колбэка).
- `options.SignIn.RequireConfirmedAccount = true` распространяется и на passkey-логин (PreSignInCheck).
- JS-сериализация credential — ручная (base64url), по workaround из доков MS: у части парольных
  менеджеров сломан `PublicKeyCredential.toJSON` («Illegal invocation»).
- Состояние церемоний (challenge) Identity хранит сам в data-protected cookie схемы
  `Identity.TwoFactorUserId` — серверного хранилища не нужно, но браузер обязан слать запросы
  same-origin с credentials. Админка раздаётся тем же WebApp — условие выполняется.

## Этапы (коммит после каждого)

1. **Конфигурация + опция**: `PasskeyOption` (Mars.Identity.Contracts/Options); в `MainIdentity`:
   `AddOptions<IdentityPasskeyOptions>()` c ServerDomain из SiteSettings.SiteUrl (хост),
   в `UseMarsIdentity` — `RegisterOption<PasskeyOption>()`.
2. **Сервис**: `IPasskeyService` (Mars.Identity.Abstractions/Services) + DTO (`PasskeySummary` и пр.
   в Dto/Passkeys) + `PasskeyService` (Mars.Identity.Host/Services), регистрация в `AddMarsIdentity`:
   - `List(userId)` — `GetPasskeysAsync` → summary (Name, CreatedAt, backup-флаги);
   - `BeginRegister(userId)` — `MakePasskeyCreationOptionsAsync(new PasskeyUserEntity { Id, Name, DisplayName })` → JSON;
   - `CompleteRegister(userId, credentialJson, name)` — `PerformPasskeyAttestationAsync` →
     **проверка ownership** (`attestationResult.UserEntity.Id == userId` — handler её не делает) →
     проверка лимита (PasskeyOption) → `AddOrUpdatePasskeyAsync` → присвоить Name;
   - `Rename(userId, credentialIdBase64, name)` — `GetPasskeyAsync` → Name → `AddOrUpdatePasskeyAsync`;
   - `Delete(userId, credentialIdBase64)` — `RemovePasskeyAsync`;
   - `BeginLogin()` — `MakePasskeyRequestOptionsAsync(null)` (usernameless/discoverable) → JSON;
   - `Login(credentialJson)` — `PerformPasskeyAssertionAsync` → **обязательно** `AddOrUpdatePasskeyAsync`
     (обновлённый sign-count иначе теряется) → JWT через `ITokenService` + cookie `SignInAsync` → `AuthResultDto`.
3. **API**: контракты в `Mars.Identity.Contracts/Passkeys/`; `PasskeyController`
   (образец — `ApiKeyController`): `GET` список, `POST creation-options`, `POST register`,
   `POST rename`, `DELETE` (credentialId — base64url строкой); `[AllowAnonymous] POST request-options`,
   `[AllowAnonymous] POST login` → `AuthResultResponse`. Options-эндпоинты отдают **сырой WebAuthn JSON**
   (`Content(json, "application/json")`, без обёрток UserActionResult).
4. **Клиентский JS**: `PasskeyJsInterop.js` + `PasskeyJs` в Mars.Admin.Framework:
   `registerPasskey(optionsUrl, submitUrl, headers, name)`, `loginWithPasskey(optionsUrl, loginUrl, headers)`,
   `isAvailable()` — fetch `credentials: 'include'`, `parseCreationOptionsFromJSON`/`parseRequestOptionsFromJSON`,
   `navigator.credentials.create/get`, ручная base64url-сериализация (convertToBase64 из доков MS), AbortController.
5. **UI логина**: кнопка «Войти с пасскеем» в `LoginForm.razor` (рендерим всегда, при недоступности
   WebAuthn — disabled); успех → `_authenticationService.LoginCallback(authResult)` → navigate.
6. **UI управления**: `src/Mars.Admin/Pages/UserViews/PasskeysPage.razor` (`@page "/Passkeys"`,
   образец — `ApiKeysPage.razor`): список, добавить (церемония через PasskeyJs), переименовать, удалить;
   `IPasskeyServiceClient`/`PasskeyServiceClient` в Mars.WebApiClient (List/Rename/Delete, регистрация
   в MarsWebApiClient); пункт «Пасскеи» в дропдауне `UserBar`. Компонент ≤400–500 строк.
7. **Тесты**: `PasskeyServiceTests` (NSubstitute над UserManager/SignInManager — по образцу
   `ApiKeyServiceTests` в tests/Mars.Integration.Tests/Services): лимит, ownership-fail, rename/delete.
   Церемонии WebAuthn юнит-тестами не покрыть — ручная проверка на localhost (Windows Hello).

## Грабли / риски

- WebAuthn работает только по HTTPS или на localhost (dev — ок, прод — за reverse proxy с TLS).
- CORS `AllowAnyOrigin` без credentials: если админка когда-нибудь уедет на другой origin —
  cookie-состояние церемоний перестанет работать. Сейчас same-origin — ок.
- `PerformPasskeyAssertionAsync` НЕ сохраняет обновлённый пасскей (в отличие от `PasskeySignInAsync`) —
  без `AddOrUpdatePasskeyAsync` ломается защита от replay (sign-count).
- Attestation statements по умолчанию не валидируются (осознанно, по докам MS).
- `IOptions<IdentityPasskeyOptions>` — singleton: SiteUrl читается один раз при старте (см. решения).
- Ошибка «Illegal invocation» у парольных менеджеров — закрыта ручной сериализацией (этап 4).
- `PasskeySignInAsync` не годится для логина напрямую: нам нужен ещё и JWT — поэтому
  `PerformPasskeyAssertionAsync` + свой токен + `SignInAsync`.

## Верификация

- `dotnet build Mars.slnx`; тесты затронутых проектов (сборка + запуск exe из bin): PasskeyServiceTests.
- Ручной сценарий на localhost: добавить пасскей в `/Passkeys` → разлогиниться → войти с пасскеем → удалить.

## Продолжения (не в этот scope)

- Passwordless-регистрация (создание аккаунта сразу с пасскеем) + account recovery (recovery-коды/email).
- Conditional UI: пасскеи как autofill-подсказки в поле логина (`mediation: 'conditional'`).
- Пасскей как второй фактор (2FA) — встроенный Identity трактует пасскей только как первичный фактор.
- `VerifyAttestationStatement` / отзыв по AAGUID (enterprise-сценарии).
- Поддержка `Disabled` через кастомный `IPasskeyHandler`-обёртку (фильтрация в allowCredentials и при assertion).
- Пасскеи во фронтовых приложениях (не админка).

## Ссылки

- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0
- https://andrewlock.net/exploring-dotnet-10-preview-features-6-passkey-support-for-aspnetcore-identity/
- https://duendesoftware.com/blog/20251007-passkeys-in-dotnet-10-blazor-apps-with-aspnet-identity
- Blazor-шаблон: `Components/Account/Pages/Manage/Passkeys.razor`, `PasskeySubmit.razor.js`
  (dotnet/aspnetcore, ProjectTemplates/Web.ProjectTemplates)
