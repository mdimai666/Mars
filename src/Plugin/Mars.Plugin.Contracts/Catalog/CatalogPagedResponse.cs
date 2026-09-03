namespace Mars.Plugin.Contracts.Catalog;

/// <summary>Страничный ответ каталога (зеркало его <c>PagedResponse&lt;T&gt;</c>).</summary>
public sealed record CatalogPagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int Take);
