# Mars Feature Integration Guide for Agent

Как добавить новую фичу-модуль в движок Mars (backend + admin-панель).
Гайд написан по реальному опыту добавления модуля `Mars.AiChat` и повторяет конвенции
существующих модулей (`Mars.SemanticKernel`, `Mars.Docker`).

## Когда модуль, а когда плагин

- **Модуль** (`src/Mars.Modules/...`) — подключается к `Mars.WebApp` проект-референсом и
  `Add*/Use*`-методами, опционально за feature-флагом. Это путь для фич, входяющих в поставку движка.
- **Плагин** (`src/Plugin/...`, грузится из `data/plugins` в рантайме) — для внешних расширений,
  см. `ai/PluginCreationGuide.md`.

## Структура модуля

```
src/Mars.Modules/
  Mars.<Feature>.Shared/        # чистые POCO: DTO, опции, константы (Sdk: Microsoft.NET.Sdk)
  Mars.<Feature>.Host.Shared/   # серверные интерфейсы/модели для внешнего переиспользования
  Mars.<Feature>.Host/          # бэкенд: Main<Feature>.cs, контроллеры, сервисы, хабы
  Mars.<Feature>.Front/         # Blazor RCL для админки (Sdk: Microsoft.NET.Sdk.Razor)
```

Не всякому модулю нужны все четыре проекта: минимум — `.Host` (бэкенд) и `.Front` (если есть UI).

### Шаблоны csproj

Общее: центральное управление версиями (`ManagePackageVersionsCentrally=true` в `Directory.Build.props`) —
**у PackageReference НЕ указывать Version**, версии пинятся в `Directory.Packages.props` (корень).
`TargetFramework` (net10.0), `Nullable`, `ImplicitUsings` — из `Directory.Build.props`, не дублировать.

`Mars.<Feature>.Host.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="..." /> <!-- без Version -->
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Mars.Host.Shared\Mars.Host.Shared.csproj" />
    <ProjectReference Include="..\Mars.<Feature>.Host.Shared\Mars.<Feature>.Host.Shared.csproj" />
  </ItemGroup>
</Project>
```

`Mars.<Feature>.Front.csproj` (RCL — wwwroot автоматически раздаётся как `_content/Mars.<Feature>.Front/...`):

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\AppFront.Main\AppFront.Main.csproj" />
    <ProjectReference Include="..\..\Mars.WebApiClient\Mars.WebApiClient.csproj" />
    <ProjectReference Include="..\Mars.<Feature>.Shared\Mars.<Feature>.Shared.csproj" />
  </ItemGroup>
</Project>
```

## Чек-лист подключения

### 1. Решение и пакеты

1. Добавить проекты в `Mars.slnx`: `<Folder Name="/Mars.Modules/<Feature>/">` + `<Project Path="...csproj" />`.
2. Новые NuGet-пакеты — пин в `Directory.Packages.props` (`<PackageVersion Include="..." Version="..." />`).

### 2. Бэкенд (Mars.WebApp)

1. `Mars.WebApp.csproj` — `<ProjectReference>` на `Mars.<Feature>.Host`.
2. Точка входа модуля — статический класс `Main<Feature>`:

```csharp
public static class MainAiChat
{
    public static IServiceCollection AddMars<Feature>(this IServiceCollection services) { ... }

    public static WebApplication UseMars<Feature>(this WebApplication app)
    {
        app.Services.GetRequiredService<IOptionService>().RegisterOption<MyOption>();
        app.MapHub<MyHub>("/_ws/my", options => options.Transports =
            HttpTransportType.WebSockets | HttpTransportType.LongPolling); // если нужен SignalR
        return app;
    }
}
```

3. Feature-флаг: константа в `src/Mars.Host.Shared/Features/FeatureFlags.cs` +
   значение в `Mars.WebApp/appsettings.json` → `FeatureManagement`.
4. `MarsWebAppStartup.cs`:

```csharp
// ConfigureBuilder
builder.AddIfFeatureEnabled(FeatureFlags.<Feature>, b => b.Services.AddMars<Feature>());
// ConfigureApp (после UseMarsNodes/..., рядом с другими UseIfFeatureEnabled)
app.UseIfFeatureEnabled(FeatureFlags.<Feature>, app => app.UseMars<Feature>());
```

Контроллеры модуля обнаруживаются автоматически (Web SDK генерирует `ApplicationPartAttribute`
для референсов WebApp) — явный `AddApplicationPart` не нужен.

### 3. Конвенции контроллеров

Образец — `AIToolController` / `AiChatController`:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]                     // или [Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]                // все из Mars.Host.Shared.ExceptionFilters
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.<Feature>)]            // Microsoft.FeatureManagement.Mvc
public class MyController { ... }
```

- Ошибки для пользователя: бросать `UserActionException` (→ HTTP 466 + текст), `NotFoundException` (→ 404).
- Текущий пользователь в HTTP-запросе: `IRequestContext` (`User.Id`, `Roles`).
  В фоновых задачах/хабах его НЕТ — передавай userId явно.
- Настройки сайта и вообще любые опции: `IOptionService` (`SysOption`, `GetOption<T>`, `SaveOption`).

### 4. Опции и их формы в админке

- Опция = обычный POCO в `.Shared`, регистрация в `Use...`: `optionService.RegisterOption<MyOption>()`.
- Форма редактирования — Razor-компонент в `.Front/OptionForms` с тремя атрибутами:

```razor
@attribute [OptionEditFormForOptionAttribute(typeof(MyOption))]  @* Mars.Shared.Options.Attributes *@
@attribute [Display(Name = "Моя фича")]
@attribute [AutoShowFormOnSettingsPage]                          @* Mars.Options.Attributes *@

<EditOptionForm @ref=_form TModel="MyOption" FormClass="col-12 col-lg-8 mx-lg-5 compact">
    ... @context — модель; CRUD делает сам EditOptionForm (api/Option/Option/{className})
</EditOptionForm>
```

После `IOptionsFormsLocator.RegisterAssembly(...)` форма сама появляется в Настройках.

### 5. Фронтенд (AppAdmin — Blazor WebAssembly)

1. `AppAdmin.csproj` — `<ProjectReference>` на `Mars.<Feature>.Front`.
2. `AppAdmin/Program.cs`:

```csharp
builder.Services.AddNodeWorkspace()...AddSemanticKernelFront().Add<Feature>Front();   // сервисы
app.Services.UseAppFrontMain()...UseSemanticKernelFront().Use<Feature>Front();        // локаторы/регистрации
```

3. **Глобальный UI** (модалки, плавающие кнопки): компонент-контейнер монтируется прямо в `src/AppAdmin/App.razor`
   рядом с `<AppFront.Shared.AppFrontSharedContainer />` — прецедент `AIToolsContainer`, `AiChatContainer`.
   Доступ из любых страниц — статический сервис-холдер (`AiChatAppService.Setup(...)`) + интерфейс.
4. Статические ассеты RCL: класть в `wwwroot/`, ссылаться `_content/Mars.<Feature>.Front/js|x|css/...`.
   JS-модули удобнее грузить динамически из компонента:
   `await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Mars.<Feature>.Front/js/my.js")` —
   тогда не нужно трогать `index.html` / `_AdminHost.cshtml`.
5. API из UI — **только через `IMarsWebApiClient`** (Flurl), не сырым HttpClient:
   - интерфейс `IMyServiceClient` + реализация `MyServiceClient : BasicServiceClient` (`_controllerName`,
     `_client.Request($"{_basePath}{_controllerName}", ...)`, обработчики `OnStatus404...`);
     см. `Implements/AiChatServiceClient.cs`;
   - подключить в `IMarsWebApiClient` + `MarsWebApiClient`;
   - если клиент использует DTO модуля — добавить ProjectReference на `.Shared` в `Mars.WebApiClient.csproj`.
6. Токен авторизации уже подставляется в общий HttpClient (`CookieOrLocalStorageAuthStateProvider`,
   ключ localStorage `authToken`) — ничего делать не нужно.
7. UI-кит: FluentUI (`Microsoft.FluentUI.AspNetCore.Components`) + bootstrap-классы;
   ошибки показывать через `IMessageService.Error(...)`.

### 6. SignalR (если нужен real-time)

- Свой хаб в `Use...` модуля: `app.MapHub<MyHub>("/_ws/my", ...)` (транспорты WebSockets | LongPolling).
- **Авторизация хаба**: «smart»-схема Mars форвардит на JWT только при заголовке `Authorization: Bearer`;
  WebSocket несёт токен в query (`access_token`), а он НЕ обрабатывается. Поэтому хабы либо без
  `[Authorize]` (конвенция `ChatHub`/`AiChatHub` — защищай данные на уровне REST и payload),
  либо нужен отдельный механизм. Не повторяй грабли: `[Authorize]` на хабе молча ломает WS-подключение.
- Клиент: `HubConnectionBuilder().WithUrl($"{Q.BackendUrl}/_ws/my", ...)`;
  на сервере `AddJsonProtocol` настроен с `PropertyNamingPolicy = null` — **продублируй на клиенте**.
- Сервер → клиент: `IHubContext<MyHub>.Clients.Group(...).SendCoreAsync(event, args)`;
  группы по сущности (`JoinX`/`LeaveX` — методы хаба).

### 7. Кэширование

`HybridCache` уже зарегистрирован в WebApp (L2 — Postgres distributed cache). Пример использования
с ключами/тегами/TTL: `Mars.AiChat.Host/Services/AiChatSessionStore.cs`.

### 8. Проверка

```
dotnet build Mars.slnx                                   # собирается всё, включая тесты
dotnet test tests/Test.Mars.Core --verbosity minimal     # быстрые юнит-тесты
```

E2E — `tests/Mars.E2E.Tests` (Playwright + Testcontainers), см. `ai/E2ETestingGuide.md`.

## Типичные грабли

- **Версии пакетов в csproj** → ошибка CPM; только `Directory.Packages.props`.
- **`[Authorize]` на SignalR-хабе** → WS не подключается (см. выше).
- **`IRequestContext` в фоне** → `HttpContext == null`; userId только через параметры.
- **JSON в SignalR** → рассинхрон casing, если на клиенте не задан `PropertyNamingPolicy = null`.
- **Форма опций без `RegisterAssembly`** → форма не появляется в Настройках.
- **`Use*`-метод на `WebApplication`** (для `MapHub`) → вызывать отдельной строкой через
  `UseIfFeatureEnabled`, не цепочкой с `IApplicationBuilder`-методами.
- **Статика в `App.razor` рендерится до инициализации `Q.Site`** → глобальные контейнеры должны
  переживать состояние «данные ещё не загружены» (см. паттерн `AiChatTerminal`/`AIToolsContainer`).
