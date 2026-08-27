using System.Globalization;

namespace Test.Mars.WebSiteProcessor.Fixtures;

public class CultureFixture : IDisposable
{
    public CultureFixture()
    {
        // Устанавливаем культуру перед запуском тестов
        Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");
    }

    public void Dispose()
    {
        // Можно вернуть культуру по умолчанию, если нужно
    }
}

[CollectionDefinition("Culture collection")]
public class CultureCollection : ICollectionFixture<CultureFixture> { }
