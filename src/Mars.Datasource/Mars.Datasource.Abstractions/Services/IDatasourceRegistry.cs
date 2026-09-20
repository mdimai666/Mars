using Mars.Contracts.Common;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;

namespace Mars.Datasource.Abstractions.Services;

/// <summary>
/// Реестр источников: сохранённые конфиги, подключённые провайдеры и проверка настроек.
/// Отдельный контракт, чтобы потребителям (форма настроек, CLI, хук опции) не был виден
/// весь набор операций запросов.
/// </summary>
public interface IDatasourceRegistry
{
    /// <summary>Основная база приложения: синтетический источник из <c>ConnectionStrings:DefaultConnection</c>.</summary>
    public DatasourceConfig DefaultConfig { get; }

    /// <summary>Профили подключённых провайдеров: форма настроек рисует по ним типы источника и их поля.</summary>
    public IReadOnlyCollection<DatasourceKindProfile> Providers();

    public IEnumerable<SelectDatasourceDto> ListSelectDatasource();

    /// <summary>Проверить несохранённые настройки: источник проверяется построением его каталога.</summary>
    public Task<UserActionResult> TestConnection(ConnectionStringTestDto dto);

    /// <summary>Настройки источников изменились: сбросить кэш конфигов и каталогов.</summary>
    public void InvalidateLocalDictCache(DatasourceOption opt);
}
