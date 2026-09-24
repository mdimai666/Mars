# ApiKeyGuide — авторизация по X-API-Key

Как устроена авторизация по персональным API-ключам пользователей и как агенту её использовать
для тестов API. Публичная пользовательская документация — `docs/dev_docs/ApiKeys.md`.

## Как это работает

- Заголовок `X-API-Key` → PolicyScheme `"smart"` (`src/Mars.WebApp/UseStartup/MarsParts/MarsStartupPartCore.cs`)
  форвардит на схему `"ApiKey"` → `ApiKeyAuthenticationHandler`
  (`src/Mars.Modules/Mars.Identity.Host/Authentication/ApiKeyAuthenticationHandler.cs`).
- Handler: парсинг ключа → lookup записи по `keyId` → constant-time сверка SHA-256(secret) с
  `KeyHash` → проверка `ExpiresAt` → `ClaimsPrincipal` через `AppClaimsPrincipalFactory`
  (тот же набор claims/роли, что при обычном логине — «ключ = пользователь»).
- Работает на всех эндпоинтах с `[Authorize]` без указания схем (дефолтная схема — `"smart"`).
- Формат ключа: `mars_{keyId}.{secret}`; вся логика (генерация, парсинг, хэш, сверка) —
  `ApiKeyFormat` в `src/Mars.Modules/Mars.Identity.Abstractions/Utils/ApiKeyFormat.cs`.

## Карта файлов

- Сущность: `src/Server/Mars.Data/Entities/UserApiKeyEntity.cs` → таблица `user_api_keys`
  (FK cascade на users). Конфигурации EF: `Mars.Data.PostgreSQL/Configurations/UserApiKeyEntityConfiguration.cs`
  и зеркальная в `Mars.Data.InMemory`. Nav-коллекция `UserEntity.UserApiKeys`.
- Репозиторий: `IUserApiKeyRepository` (`Mars.Identity.Abstractions/Repositories`) +
  internal `UserApiKeyRepository` (`src/Server/Mars.Data.Repositories/`, регистрация в `MainDataRepositories`).
- Сервис: `IApiKeyService`/`ApiKeyService` (`Mars.Identity.Host/Services/ApiKeyService.cs`) —
  Create (полный ключ возвращается один раз), ListByUser, Revoke, проверки лимита и срока.
- Лимит ключей: `ApiOption.ApiKeysMaxPerUser` (`src/Server/Mars.Server.Contracts/Options/ApiOption.cs`,
  default 10), форма настройки — `src/Admin/Mars.Admin.Framework/OptionEditForms/ApiOptionEditForm.razor`.
- Контроллер: `ApiKeyController` (`Mars.Identity.Host/Controllers`) — `api/ApiKey`: GET (список),
  POST (создание), DELETE `{id}` (отзыв); «свои» ключи через `IRequestContext.User.Id`.
  Контракты — `Mars.Identity.Contracts/ApiKeys/`, маппинг — `Mars.Identity.Abstractions/Mappings/ApiKeys/`.
- Web-клиент: `IApiKeyServiceClient`/`ApiKeyServiceClient` (`src/Mars.WebApiClient`), свойство `ApiKey`
  у `IMarsWebApiClient`.
- UI: страница `/dev/ApiKeys` — `src/Mars.Admin/Pages/UserViews/ApiKeysPage.razor(.cs)`,
  пункт меню в `src/Mars.Admin/Shared/UserBar.razor`.
- CLI: подкоманды `user apikey add|list|delete` — `Mars.Identity.Host/CommandLine/UserCommandCli.cs`.
- Тесты: `tests/Mars.Server.Tests/Utils/ApiKeyFormatTests.cs`,
  `tests/Mars.Integration.Tests/Services/ApiKeyServiceTests.cs` (NSubstitute, без БД).

## Рецепт: протестировать API ключом

1. Создать ключ (живой инстанс): `Mars.exe user apikey add <username> --name test [--expires <дата>]`
   — полный ключ печатается один раз. **Команды мутируют живой инстанс — запускать против
   работающего сервера только с подтверждения пользователя** (см. QWEN.md).
2. Дёрнуть любой `[Authorize]`-эндпоинт:
   `curl -H "X-API-Key: <ключ>" http://localhost:<порт>/api/...`
3. Отозвать: `Mars.exe user apikey delete <username> test`.
4. В интеграционных тестах (без CLI): создать ключ через `IApiKeyService` (или записать
   `UserApiKeyEntity` напрямую: `Id`, `KeyHash = ApiKeyFormat.HashSecret(secret)`,
   `KeyPrefix = ApiKeyFormat.DisplayPrefix(id)`), затем добавлять заголовок `X-API-Key`
   с `ApiKeyFormat.Build(id, secret)` к запросам тестового хоста (`Mars.Test.AppHost`).

## Грабли

- Восстановить secret из БД нельзя — хранится только хэш; потерянный ключ = отозвать и создать новый.
- Наличие заголовка `X-API-Key` переключает аутентификацию на схему `"ApiKey"` целиком:
  невалидный ключ даст 401 даже при живой cookie (на cookie/JWT фолбэка нет).
- `ApiKeyStrategy` в `src/Modules/Mars.HttpSmartAuthFlow` — клиентская стратегия для ИСХОДЯЩИХ
  запросов нод (Http Request, REST-источники). К серверной авторизации отношения не имеет, не трогать.
- FluentDataGrid: страница ключей использует `GridItemsProvider` + `RefreshDataAsync()`;
  привязка `ItemsSource` к переassign-иваемому `List` строки не обновляет (проверено багом 2026-09-25).
- Swagger уже декларирует `X-API-Key` (`src/Server/Mars.Server/Startup/MarsSwagger.cs`, схема "ApiKey") —
  отдельные правки не нужны.
- CLI в тестовом режиме (IsTesting/ASPNETCORE_ENVIRONMENT=Test) игнорирует аргументы — CLI-путь
  для тестов недоступен, использовать сервисы напрямую.

## Инварианты

- Секрет ключа нигде не хранится и не логируется в открытом виде — только SHA-256 (base64) в `KeyHash`.
- Ключ = пользователь: claims собираются `AppClaimsPrincipalFactory`, отдельных скоупов/прав у ключа нет.
- FK только fluent-конфигурацией, без атрибутов `[ForeignKey]` (конвенция проекта).
- Лимит на пользователя проверяется в `ApiKeyService.Create`, а не в контроллере/UI.

## Отклонено

- Формат `{userId}.{secret}` — раскрывал бы владельца при утече ключа; вместо него `{keyId}.{secret}`.
- Скоупы/ограничение прав ключа — отложены (MVP «ключ = пользователь»), модель под это не закладывалась.
- `LastUsedAt` — не делали осознанно, чтобы не долбить БД апдейтом на каждый запрос.
