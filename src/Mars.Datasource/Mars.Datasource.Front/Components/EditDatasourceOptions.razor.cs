using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Front.Services;
using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Components;

/// <summary>
/// Форма списка источников: карточка на источник, выбор типа и движка. Поля настроек конкретного
/// типа источника рисует <see cref="DatasourceSettingFields"/> по дескрипторам из профиля провайдера,
/// поэтому форма не знает, какие бывают источники.
/// </summary>
public partial class EditDatasourceOptions
{
    [Inject] IDatasourceServiceClient datasourceService { get; set; } = default!;

    [CascadingParameter] public DatasourceOption opt { get; set; } = default!;

    /// <summary>Профили провайдеров приходят от сервера: тип источника можно подключить, не правя ядро модуля.</summary>
    IReadOnlyCollection<DatasourceKindProfile> drivers = [];

    IEnumerable<string> Kinds => drivers
        .Select(profile => profile.Kind)
        .Where(kind => !string.IsNullOrEmpty(kind))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase);

    IEnumerable<DatasourceKindProfile> DriversFor(string kind)
        => drivers.Where(profile => string.Equals(profile.Kind, kind, StringComparison.OrdinalIgnoreCase));

    IEnumerable<string> DriverKeys(string kind)
        => DriversFor(kind).Select(profile => profile.Driver).Where(driver => !string.IsNullOrEmpty(driver));

    /// <summary>У типа источника без вариантов (file, rest) драйвера в форме нет.</summary>
    bool HasDrivers(string kind) => DriverKeys(kind).Any();

    /// <summary>Подпись типа источника — из профиля провайдера, а не из словаря формы.</summary>
    string KindLabel(string kind) => DriversFor(kind).FirstOrDefault()?.KindLabel ?? kind;

    string? KindDescription(DatasourceConfig config)
        => Profile(config)?.Description is { Length: > 0 } description ? description : null;

    /// <summary>Профиль выбранного провайдера; для типа без вариантов — единственный профиль типа.</summary>
    DatasourceKindProfile? Profile(DatasourceConfig config)
        => DriversFor(config.Kind).FirstOrDefault(profile => string.Equals(profile.Driver, config.Driver, StringComparison.OrdinalIgnoreCase))
           ?? DriversFor(config.Kind).FirstOrDefault();

    IReadOnlyList<DatasourceSettingField> SettingsFields(DatasourceConfig config)
        => Profile(config)?.Settings ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (opt.Configs.Count == 0)
        {
            opt.Configs.Add(new DatasourceConfig());
        }

        try
        {
            drivers = await datasourceService.Providers();
            RepairUnknownKinds();
        }
        catch
        {
            // Список провайдеров — подсказка в форме: без него редактор остаётся рабочим.
        }
    }

    /// <summary>
    /// Тип источника должен быть из списка подключённых провайдеров: сохранённое значение могло
    /// устареть или записаться подписью вместо ключа — тогда форма прячет поля, а сервер не находит провайдера.
    /// </summary>
    void RepairUnknownKinds()
    {
        var known = Kinds.ToList();

        if (known.Count == 0) return;

        foreach (var config in opt.Configs)
        {
            if (known.Contains(config.Kind, StringComparer.OrdinalIgnoreCase)) continue;

            config.Kind = known.Contains(DatasourceKind.Sql, StringComparer.OrdinalIgnoreCase) ? DatasourceKind.Sql : known[0];
            AfterChangeKind(config);
        }
    }

    void AddNew() => opt.Configs.Add(new DatasourceConfig());

    void Delete(DatasourceConfig cfg) => opt.Configs.Remove(cfg);

    string? SlugError(DatasourceConfig config)
    {
        var error = DatasourceConfig.ValidateSlug(config.Slug);
        if (error is not null) return error;

        var duplicate = opt.Configs.Any(c => !ReferenceEquals(c, config)
            && string.Equals(c.Slug, config.Slug, StringComparison.OrdinalIgnoreCase));

        return duplicate ? "Slug уже занят другим источником" : null;
    }

    string? SettingsError(DatasourceConfig config)
        => Profile(config) is { DefaultConnectionString.Length: > 0 } && string.IsNullOrWhiteSpace(config.ConnectionString)
            ? "Укажите строку подключения"
            : null;

    /// <summary>Сменили тип источника — драйвер, строка подключения и настройки от прошлого типа уже не подходят.</summary>
    void AfterChangeKind(DatasourceConfig config)
    {
        var available = DriverKeys(config.Kind).ToList();

        if (!available.Contains(config.Driver, StringComparer.OrdinalIgnoreCase))
        {
            config.Driver = available.FirstOrDefault() ?? "";
        }

        config.Settings.Clear();

        AfterChangeDriver(config, config.Driver);
    }

    void AfterChangeDriver(DatasourceConfig config, string? driver)
    {
        config.Driver = driver ?? "";

        var profile = Profile(config);

        if (profile is null) return;

        if (profile.DefaultConnectionString.Length == 0)
        {
            config.ConnectionString = "";
            return;
        }

        if (string.IsNullOrEmpty(config.ConnectionString) || config.IsDefaultString())
        {
            config.ConnectionString = profile.DefaultConnectionString;
        }
    }
}
