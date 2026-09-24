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

## Этап 2 — Logout

- [ ] Клиент (`AuthenticationService.Logout`) вызывает серверный `POST /api/Account/Logout`
      (SignOutAsync реально снимает cookie); убрать мёртвый `CookieRemove('.AspNetCore.Identity.Application')`.

## Этап 3 — Переход на A1 (cookie-first)

- [ ] Сервер: cookie `ExpireTimeSpan` = 3 дня; `JWTSettings.expiryInMinutes` 43200 → 4320 (3 дня).
- [ ] Логин-эндпоинты: JWT в теле только для не-браузерных клиентов (решить при реализации:
      убрать из ответа или оставить — клиент его больше не хранит).
- [ ] Клиент: не писать JWT в localStorage (оба формата); `CookieOrLocalStorageAuthStateProvider` →
      cookie-провайдер (состояние из `InitialUserPrimaryInfo` / me-эндпоинта); удалить мёртвый
      fallback на cookie `authToken`, закомментированный refresh, `TryRefreshAccessTokenAsync`.
- [ ] `PasskeyJsInterop.js` / `PasskeyJs.cs`: fetch с `credentials: 'include'` без Bearer-заголовка.
- [ ] SSO callback: внешний id_token в localStorage не класть (cookie уже ставится SignInAsync).
- [ ] Хабы: `[Authorize]` на `ChatHub`/`AiChatHub`; `AccessTokenProvider` у клиента AiChat убрать
      (cookie приезжает в WS-рукопожатии сама) либо оставить с `OnMessageReceived` — решить по факту.
- [ ] Чек-пойнт: админские сервисы редактирования юзеров (роли, блокировка, данные) вызывают
      `UpdateSecurityStampAsync`; при отсутствии — добавить.
- [ ] SPA front template (`Res/front_templates/default/wwwroot/js/app.js`): тот же паттерн
      localStorage — привести к cookie.

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
