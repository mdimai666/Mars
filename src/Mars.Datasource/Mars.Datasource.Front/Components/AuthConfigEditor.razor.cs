using Mars.HttpSmartAuthFlow;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Доступы источника на общей модели <see cref="AuthConfig"/> из <c>Mars.HttpSmartAuthFlow</c>:
/// тот же словарь режимов и те же поля, которые исполняет <c>AuthFlowHandler</c> на сервере.
/// «Без доступа» — это <c>null</c>, а не отдельный режим.
/// </summary>
public partial class AuthConfigEditor
{
    /// <summary>Режимы, которые источник умеет исполнять; пустая строка — без доступа.</summary>
    static readonly string[] Modes =
    [
        "",
        nameof(AuthMode.BasicAuth),
        nameof(AuthMode.BearerToken),
        nameof(AuthMode.ApiKey),
        nameof(AuthMode.CookieForm),
    ];

    [Parameter] public AuthConfig? Value { get; set; }

    [Parameter] public EventCallback<AuthConfig?> ValueChanged { get; set; }

    string Mode => Value?.Mode.ToString() ?? "";

    bool UsesCredentials => Value?.Mode is AuthMode.BasicAuth or AuthMode.BearerToken or AuthMode.CookieForm;

    static string ModeLabel(string mode) => mode switch
    {
        "" => "без доступа",
        nameof(AuthMode.BasicAuth) => "Basic — логин и пароль",
        nameof(AuthMode.BearerToken) => "Bearer — токен по логину",
        nameof(AuthMode.ApiKey) => "API key — заголовок с ключом",
        nameof(AuthMode.CookieForm) => "Cookie — вход через форму",
        _ => mode,
    };

    async Task OnModeChanged(string mode)
    {
        if (string.IsNullOrEmpty(mode))
        {
            await ValueChanged.InvokeAsync(null);
            return;
        }

        var config = Value ?? new AuthConfig { ApiKeyHeaderName = "X-API-Key" };
        config.Mode = Enum.Parse<AuthMode>(mode);

        await ValueChanged.InvokeAsync(config);
    }

    async Task Set(Action<AuthConfig> change)
    {
        if (Value is null) return;

        change(Value);

        await ValueChanged.InvokeAsync(Value);
    }
}
