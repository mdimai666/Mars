namespace Mars.Integration.Tests.Attributes;

/// <summary>
/// E2E-тест (Playwright + живой сервер + Testcontainers): по умолчанию выключен, включает
/// переменная окружения <c>MARS_E2E_TESTS=1</c> (как <c>MARS_DOCKER_TESTS</c> у контейнерных
/// тестов). Свой атрибут вместо константы скипа в <c>BaseE2ETests</c>: прогон включается
/// окружением, без правки кода и пересборки сьюта.
/// </summary>
public class E2EFactAttribute : FactAttribute
{
    /// <summary>Переменная окружения, включающая E2E-прогон</summary>
    public const string EnvironmentVariable = "MARS_E2E_TESTS";

    /// <summary>E2E-тесты разрешены (<c>MARS_E2E_TESTS=1</c>)</summary>
    public static bool Enabled => Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim() == "1";

    public E2EFactAttribute()
    {
        if (!Enabled)
        {
            Skip = $"E2E-тесты выключены; для запуска задайте {EnvironmentVariable}=1";
        }
    }
}
