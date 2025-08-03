using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

public interface IPostContentProcessorsLocator
{
    IReadOnlyCollection<string> ListKeys(string[]? tags = null);

    /// <summary>
    /// GetProvider
    /// </summary>
    /// <param name="key">
    /// <see cref="PostTypeDetail.PostContentSettings"/>
    /// <see cref="PostContentSettingsDto.PostContentType"/>
    /// </param>
    /// <returns></returns>
    IPostContentProcessor? GetProvider(string postContentType);
}
