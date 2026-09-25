# PasskeyGuide — пасскеи (WebAuthn)

Как устроен вход по пасскеям и управление ими. База — встроенная поддержка WebAuthn в
ASP.NET Core Identity 10 (`SignInManager`/`UserManager`), без сторонних библиотек.
История работ: `git show c4035291:ai/PasskeyPlan.md`.

## Как это работает

- Церемонии — стандартные API Identity: `MakePasskeyCreationOptionsAsync` / `PerformPasskeyAttestationAsync`
  (регистрация), `MakePasskeyRequestOptionsAsync` / `PerformPasskeyAssertionAsync` (вход),
  `AddOrUpdatePasskeyAsync` / `GetPasskeysAsync` / `GetPasskeyAsync` / `RemovePasskeyAsync` (хранение).
  Стор — встроенный EF: `MarsDbContext : IdentityDbContext<…, UserPasskeyEntity>` реализует `IUserPasskeyStore`.
- Состояние челленджа живёт в data-protected cookie схемы `Identity.TwoFactorUserId` (серверного хранилища нет)
  → церемонии идут **целиком в браузерном JS**: fetch same-origin с `credentials: 'include'`
  + заголовок `Authorization: bearer <token из localStorage>`.
- Регистрация: `POST /api/passkey/creation-options` ([Authorize], отдаёт сырой WebAuthn JSON) →
  `navigator.credentials.create` → `POST /api/passkey/register {credentialJson, name}` →
  attestation → **ownership-проверка** → лимит → `AddOrUpdatePasskeyAsync`.
- Вход: `POST /api/passkey/request-options` ([AllowAnonymous], usernameless — `MakePasskeyRequestOptionsAsync(null)`,
  нужны discoverable credentials) → `navigator.credentials.get` → `POST /api/passkey/login` →
  assertion → `AddOrUpdatePasskeyAsync` (обновлённый sign-count!) → `CanSignInAsync` →
  JWT через `ITokenService` + cookie `SignInAsync` → `AuthResultResponse`; клиент вызывает
  `IAuthenticationService.LoginCallback` (localStorage `authToken` + auth-state provider).
- Флаг разрешения: `PasskeyOption.Enabled` — серверный гейт в `PasskeyService` (только register/login;
  rename/delete не гейтятся — удалить существующие можно и при выключенных пасскеях),
  клиентский гейт — кнопка LoginForm и плашка/кнопка PasskeysPage.

## Карта файлов

- Сущность: `src/Server/Mars.Data/Entities/UserPasskeyEntity.cs` — `IdentityUserPasskey<Guid>` +
  CreatedAt/ModifiedAt/Disabled; таблица `user_passkeys`, FK cascade. EF-конфигурация:
  `src/Server/Mars.Data.InMemory/Configurations/UserPasskeyEntityConfiguration.cs` (общая для контекстов).
- Опция: `PasskeyOption` (`src/Mars.Modules/Mars.Identity.Contracts/Options/PasskeyOption.cs`) —
  `Enabled` (default true) + `PasskeysMaxPerUser` (default 10); регистрация в `MainIdentity.UseMarsIdentity`
  с `appendToInitialSiteData: true` (уезжает клиенту). Форма настроек:
  `src/Admin/Mars.Admin.Framework/OptionEditForms/PasskeyOptionEditForm.razor` (Настройки → «Пасскеи»).
- RP ID: `IdentityPasskeyOptions.ServerDomain` ← хост `SiteSettings.SiteUrl` — в `MainIdentity.AddMarsIdentity`.
- Сервис: `IPasskeyService` (`Mars.Identity.Abstractions/Services`) + `PasskeyService`
  (`Mars.Identity.Host/Services/PasskeyService.cs`) — вся логика, гейты, лимиты, JWT при входе.
  DTO: `Dto/Passkeys/PasskeySummary.cs`, маппинг `Mappings/Passkeys/PasskeyMapping.cs`.
- Контроллер: `PasskeyController` (`Mars.Identity.Host/Controllers`) — `api/passkey`:
  GET список, POST `creation-options`/`register`/`rename`/`request-options`/`login`, DELETE `{credentialId}`.
  Контракты: `Mars.Identity.Contracts/Passkeys/`. Options-эндпоинты отдают сырой JSON
  (`Content(json, "application/json")`) — не оборачивать в UserActionResult.
- Клиентский JS: `src/Admin/Mars.Admin.Framework/wwwroot/PasskeyJsInterop.js` + обёртка `PasskeyJs.cs`
  (DI — `TryAddScoped` в `MainAdminFramework.AddWasmServices`).
- UI: кнопка входа — `src/Mars.Admin/Pages/Public/LoginForm.razor(.cs)` (`ExecutePasskeyLogin`);
  управление — `src/Mars.Admin/Pages/UserViews/PasskeysPage.razor(.cs)` (`@page "/Passkeys"`,
  ссылка `/dev/Passkeys` в `UserBar.razor`).
- WebApiClient: `IPasskeyServiceClient`/`PasskeyServiceClient` (`src/Mars.WebApiClient`) — `client.Passkey`
  (List/Rename/Delete). Церемонии через него НЕ ходят — только JS.
- Тесты: `tests/Mars.Integration.Tests/Services/PasskeyServiceTests.cs`.

## Тесты

- Юниты (13): NSubstitute, без БД. Запуск: собрать проект, затем из `bin\Debug\net10.0`:
  `Mars.Integration.Tests.exe -filter "/*/*/PasskeyServiceTests/*"`.
- Сабститут `UserManager` — через store: `Substitute.For<IUserStore<UserEntity>, IUserPasskeyStore<UserEntity>>()`
  и стабы методов `IUserPasskeyStore`; виртуальные методы самого UserManager стабить ненадёжно (см. Грабли).
- Церемонии WebAuthn юнитами не покрыть — ручная проверка на localhost (Windows Hello):
  добавить на `/Passkeys` → разлогиниться → войти с пасскеем → удалить.

## Грабли

- WebAuthn работает только по HTTPS или localhost.
- `PerformPasskeyAssertionAsync` НЕ сохраняет обновлённый пасскей — без `AddOrUpdatePasskeyAsync`
  после assertion ломается защита от replay (sign-count). `PasskeySignInAsync` сохраняет сам,
  но не возвращает пользователя — а нам нужен ещё и JWT, поэтому используем assertion-путь.
- Ownership-проверка при регистрации обязательна (`attestationResult.UserEntity.Id == userId`):
  handler берёт пользователя из состояния в cookie и сам это не проверяет.
- «Illegal invocation» — у части парольных менеджеров сломан `PublicKeyCredential.toJSON`;
  в `PasskeyJsInterop.js` ручная base64url-сериализация (workaround из доков MS). Не заменять на `JSON.stringify(credential)`.
- NSubstitute `ForPartsOf<UserManager>`: стабы виртуальных методов могут не срабатывать (вызов
  проваливается в base) — стабить на уровне store-интерфейса. `SignInManager.SignInAsync` обязательно
  застабить: base требует HttpContext с `IAuthenticationService`.
- CORS `AllowAnyOrigin` без credentials: если админка уедет на другой origin — cookie-состояние
  церемоний перестанет ходить, вход/регистрация сломаются.
- `UserPasskeyInfo.CredentialId` — `byte[]`; в API и роутах используется base64url-строка
  (`Base64UrlEncoder`), порядок параметров ctor `UserPasskeyInfo` не совпадает с XML-доками
  (transports/флаги идут перед attestationObject/clientDataJson).

## Инварианты

- RP ID фиксируется при старте (`IOptions<IdentityPasskeyOptions>` — singleton): смена
  `SiteSettings.SiteUrl` инвалидирует зарегистрированные пасскеи (привязаны к домену). SiteUrl пуст →
  ServerDomain не задаётся (фолбэк на Host header).
- `SignIn.RequireConfirmedAccount = true` распространяется на passkey-вход (проверка `CanSignInAsync`).
- Лимит и `Enabled` проверяются в `PasskeyService`, не в контроллере/UI.
- Клиентский гейт трактует отсутствие опции как «включено» (`?.Enabled != false`) — back-compat.

## Отклонено

- Сторонние WebAuthn-библиотеки (fido2-net-lib и т.п.) — Identity 10 покрывает сценарии аутентификации из коробки.
- Поддержка `Disabled` — решение «только удаление»; встроенный стор поле не учитывает, для него нужна
  кастомная обёртка `IPasskeyHandler`. Поле в сущности лежит мёртвым грузом.
- Валидация attestation statements — не делаем (дефолт MS, осознанно).

## Что ещё не сделано

- Passwordless-регистрация (аккаунт сразу с пасскеем) + account recovery.
- Conditional UI — пасскеи как autofill-подсказки в поле логина (`mediation: 'conditional'`).
- Пасскей как 2FA — встроенный Identity трактует его только как первичный фактор.
- `VerifyAttestationStatement` / отзыв по AAGUID (enterprise).
- Пасскеи во фронтовых приложениях (не админка).
- Rate-limit на анонимные `request-options`/`login` (общий TODO с AccountController).
