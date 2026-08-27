namespace Mars.Shared.Common;

public interface IBasicListQuery
{
    public int Skip { get; }
    public int Take { get; }

    /// <summary>
    /// <example>
    /// <list type="bullet">
    /// <item>CreatedAt: Ascending</item>
    /// <item>-CreatedAt: Descending</item>
    /// </list>
    /// </example>
    /// </summary>
    public string? Sort { get;  }
    public string? Search { get; }

    bool IncludeTotalCount { get; }

    public int ConvertSkipTakeToPage();

}
