namespace Mars.Forms.Abstractions.Services;

/// <summary>
/// Область видимости настроек провайдера по модели владельца: точный ключ, затем шаблоны
/// <c>prefix.*</c> от длинного к короткому (<c>sql.ds1.orders</c> → <c>sql.ds1.*</c> → <c>sql.*</c>).
/// Используется локатором провайдеров и реестром правил.
/// </summary>
static class FormOwnerScopes
{
    public static IEnumerable<string> Of(string ownerModel)
    {
        if (string.IsNullOrEmpty(ownerModel)) yield break;

        yield return ownerModel;

        for (var i = ownerModel.Length - 1; i > 0; i--)
        {
            if (ownerModel[i] == '.')
                yield return string.Concat(ownerModel.AsSpan(0, i), ".*");
        }
    }
}
