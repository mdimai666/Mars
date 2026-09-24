# План: X-API-Key авторизация пользователей

Ветка: `ai/user-xapi-key`.

**Статус 2026-09-25: реализовано** — все этапы 1–7 выполнены и закоммичены в ветке,
сборка `Mars.slnx` чистая, новые тесты зелёные (ApiKeyFormatTests — 13, ApiKeyServiceTests — 4).
Осталось: ревью/merge, при закрытии инициативы — свернуть план в гайд по `ai/PlanLifecycleGuide.md`.

## Решения (согласованы с пользователем 2026-09-25)

- Формат ключа: `mars_{keyId}.{secret}` — keyId = Guid записи ключа, secret — случайный,
  в БД хранится только SHA-256 хэш secret. Полный ключ показывается ровно один раз при создании.
- Права: MVP «ключ = пользователь» — те же claims/роли, что при обычном логине. Скоупы — позже.
- Lifecycle: имя ключа, отзыв (удаление), опциональный срок действия (ExpiresAt),
  лимит активных ключей на пользователя (настройка, default 10). Без LastUsedAt.
- Точка аутентификации: PolicyScheme `"smart"` в `src/Mars.WebApp/UseStartup/MarsParts/MarsStartupPartCore.cs` —
  ветка «есть заголовок `X-API-Key` → схема `ApiKey`» (кастомный AuthenticationHandler).
  Работает везде, где сейчас работает `[Authorize]`, без правок контроллеров.
- UI: отдельная страница `/ApiKeys` в Mars.Admin (свои ключи), пункт в дропдауне UserBar.
- CLI: подкоманды `user apikey add|list|delete` в `UserCommandCli` (Mars.Identity.Host).
- `ApiKeyStrategy` в `src/Modules/Mars.HttpSmartAuthFlow` НЕ трогаем — это клиентская
  стратегия для исходящих запросов нод, к серверной авторизации отношения не имеет.
- Swagger уже декларирует `X-API-Key` (`src/Server/Mars.Server/Startup/MarsSwagger.cs`) — декларация станет правдой.

## Этапы (коммит после каждого)

1. **БД**: `UserApiKeyEntity` в `src/Server/Mars.Data/Entities/` (образец — `UserPasskeyEntity`):
   Id, UserId+nav, Name, KeyHash (base64 SHA-256), KeyPrefix, ExpiresAt?, CreatedAt.
   DbSet в MarsDbContext (+PluginDbContext по образцу passkey), FK cascade в UserEntityConfiguration,
   конфигурации EF в PostgreSQL и InMemory наборах, миграция `AddUserApiKeys` (AddMigration.ps1).
2. **Репозиторий + сервис**: `IUserApiKeyRepository` в Mars.Identity.Abstractions + internal-реализация
   в Mars.Data.Repositories (регистрация MainDataRepositories). `IApiKeyService`/`ApiKeyService`
   в Mars.Identity.Host: Create (возврат полного ключа один раз), List, Revoke, проверка лимита
   (опция через IOptionService, default 10).
3. **Аутентификация**: `ApiKeyAuthenticationHandler` (схема `"ApiKey"`) в Mars.Identity.Host:
   парсинг `mars_{keyId}.{secret}`, lookup по keyId, constant-time сравнение хэшей, проверка ExpiresAt,
   ClaimsPrincipal через `AppClaimsPrincipalFactory`. Регистрация схемы + расширение селектора `"smart"`.
4. **API**: `ApiKeyController` в Mars.Identity.Host/Controllers (образец — AccountController):
   `[Authorize]`, свои ключи через IRequestContext — list/create/revoke. Контракты в Mars.Identity.Contracts.
5. **CLI**: подкоманда `apikey` в UserCommandCli — add/list/delete.
6. **UI**: `src/Mars.Admin/Pages/UserViews/ApiKeysPage.razor` (`@page "/ApiKeys"`),
   пункт в UserBar, WebApiClient-паттерн, компонент ≤400–500 строк.
7. **Тесты**: юнит-тесты handler/сервиса (формат ключа, хэш-сверка, просрочка, лимит, отзыв).

## Верификация

- `dotnet build Mars.slnx`; тесты затронутых проектов (сборка + запуск exe из bin).
