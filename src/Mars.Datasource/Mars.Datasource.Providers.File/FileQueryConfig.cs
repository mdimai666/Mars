using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// Конфигурация разбора запросов к файлу. Функции <see cref="Val"/> регистрируем явно:
/// поиск типов по атрибуту <c>[DynamicLinqType]</c> сканирует уже загруженные сборки и кэширует
/// результат на первом же разборе, поэтому без явной регистрации <c>Val</c> находится через раз.
/// </summary>
public static class FileQueryConfig
{
    public static ParsingConfig ParsingConfig { get; } = Create();

    static ParsingConfig Create()
    {
        ParsingConfig config = new();
        config.CustomTypeProvider = new DefaultDynamicLinqCustomTypeProvider(config, [typeof(Val)], true);

        return config;
    }
}
