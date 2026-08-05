# E2E Testing Guide for Agent

## Overview

E2E тесты в Mars используют Playwright + xUnit + Testcontainers. Тесты запускают реальный ASP.NET Core сервер и браузер.

## Structure

```
tests/Mars.E2E.Tests/
├── BaseE2ETests.cs          # Базовый класс для всех тестов
├── Fixtures/
│   └── E2EServerFixture.cs  # Фикстура сервера и БД
├── Helpers/
│   └── BrowserErrorTracker.cs
└── Tests/
    └── EditUserPageTests.cs # Пример рабочего теста
```

## Base Test Class

```csharp
[Collection("E2ETestApp")]
public class BaseE2ETests : IAsyncLifetime
{
    public const string? SkipE2ETests = null; // null = тесты включены
    
    protected readonly E2EServerFixture AppFixture;
    protected IPage Page { get; private set; } = null!;
    protected string BaseUrl => AppFixture.BaseUrl;
    
    // Автоматическая авторизация перед каждым тестом
    public override bool AuthorizedStart => true;
}
```

## Test Attribute

```csharp
[IntegrationFact(Skip = SkipE2ETests)]
public async Task MyTest_Scenario_ExpectedResult()
{
    // Arrange
    // Act
    // Assert
}
```

## Fluent UI Web Components

Fluent UI Blazor ренерит web components с shadow DOM. Обычные Playwright методы не работают.

### Правильный паттерн для заполнения полей

```csharp
// 1. Добавить Name атрибут в Razor
<FluentTextField Name="@nameof(model.FieldName)" @bind-Value=@model.FieldName />

// 2. В тесте использовать Click + Ctrl+A + PressSequentially
await FillTextField(Page, "FieldName", "value");

// Helper метод
private static async Task FillTextField(IPage page, string fieldName, string value)
{
    await page.Locator($"[name='{fieldName}']").ClickAsync();
    await page.Keyboard.PressAsync("Control+a");
    await page.Locator($"[name='{fieldName}']").PressSequentiallyAsync(value, new() { Delay = 10 });
}
```

### Почему не работает FillAsync

`FluentTextField` ренерит `<fluent-text-field>` с input внутри shadow DOM. Playwright не может найти input через обычные CSS selectors.

## Timeouts

**Максимальный таймаут: 3 секунды**

```csharp
// Ожидание элементов
await Page.WaitForSelectorAsync("[name='FieldName']", new() { Timeout = 3000 });

// Ожидание ответа API
var response = await Page.RunAndWaitForResponseAsync(
    async () => await Page.Locator("button[type='submit']").ClickAsync(),
    response => response.Url.Contains("/api/..."),
    new() { Timeout = 3000 });

// Ожидание сетевых запросов
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

## API Verification

Всегда проверяй ответ сервера:

```csharp
// Ожидание и проверка ответа
var saveResponse = await Page.RunAndWaitForResponseAsync(
    async () => await Page.Locator("button[type='submit']").ClickAsync(),
    response => response.Url.Contains("/api/User") && response.Request.Method == "PUT",
    new() { Timeout = 3000 });

saveResponse.Should().NotBeNull("API call should be made");
saveResponse!.Status.Should().Be(200, $"Save should succeed, but got: {await saveResponse.TextAsync()}");

// Проверка данных через API
var client = AppFixture.GetClient(isAnonymous: false);
var updatedUser = await client.Request($"api/User/{userId}").GetJsonAsync<UserDetailResponse?>();
updatedUser.Should().NotBeNull();
updatedUser!.FirstName.Should().Be("ExpectedValue");
```

## Common Patterns

### Navigation

```csharp
await Page.GotoAsync($"{BaseUrl}/dev/EditUser/{userId}");
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

### Button Click

```csharp
await Page.Locator("button[type='submit']").ClickAsync();
```

### Error Tracking

```csharp
var tracker = new BrowserErrorTracker(Page);
// ... тест ...
tracker.AssertNoErrors();
```

## Server-Side Validation

Валидация происходит на сервере через FluentValidation. Примеры:

- **Телефон**: `UserPhoneValidator` использует `PhoneUtil.TryNormalizePhone` (libphonenumber)
  - Требуется валидный международный формат: `+79161234567`
  - Невалидные: `+1234567890`, `89161234567`

- **Email**: `EmailAddressThatAllowsBlanks` — разрешает пустую строку или валидный email

## Blazor Server Specifics

- **Нет редиректов** после сохранения — страница остаётся на том же URL
- **SignalR** — изменения в UI происходят через WebSocket
- **Используй `WaitForLoadStateAsync(LoadState.NetworkIdle)`** вместо `WaitForURLAsync`

## Complete Example

```csharp
[IntegrationFact(Skip = SkipE2ETests)]
public async Task EditUserPage_UpdateFields_ShouldPersist()
{
    // Arrange
    var tracker = new BrowserErrorTracker(Page);
    var userId = UserConstants.TestUserId;
    var newFirstName = "UpdatedFirstName";
    var newPhoneNumber = "+79161234567";

    await Page.GotoAsync($"{BaseUrl}/dev/EditUser/{userId}");
    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    await Page.WaitForSelectorAsync("[name='FirstName']", new() { Timeout = 3000 });

    // Act — fill fields
    await FillTextField(Page, "FirstName", newFirstName);
    await Task.Delay(500);
    await FillTextField(Page, "PhoneNumber", newPhoneNumber);

    // Save and verify API response
    var saveResponse = await Page.RunAndWaitForResponseAsync(
        async () => await Page.Locator("button[type='submit']").ClickAsync(),
        response => response.Url.Contains("/api/User") && response.Request.Method == "PUT",
        new() { Timeout = 3000 });

    saveResponse!.Status.Should().Be(200);
    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

    // Assert — verify via API
    var client = AppFixture.GetClient(isAnonymous: false);
    var updatedUser = await client.Request($"api/User/{userId}").GetJsonAsync<UserDetailResponse?>();
    
    updatedUser!.FirstName.Should().Be(newFirstName);
    updatedUser.PhoneNumber.Should().Be(newPhoneNumber);
    
    tracker.AssertNoErrors();
}
```

## Reference

- Рабочий пример: `tests/Mars.E2E.Tests/Tests/EditUserPageTests.cs`
- Базовый класс: `tests/Mars.E2E.Tests/BaseE2ETests.cs`
- Фикстура: `tests/Mars.E2E.Tests/Fixtures/E2EServerFixture.cs`
