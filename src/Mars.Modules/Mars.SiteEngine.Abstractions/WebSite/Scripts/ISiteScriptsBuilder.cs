namespace Mars.Host.Shared.WebSite.Scripts;

public interface ISiteScriptsBuilder
{
    void RegisterProvider(string key, ISiteAssetPrivider provider, float order = 10f, bool placeInHead = false);
    string HeadScriptsRender();
    string FooterScriptsRender();
    void ClearCache();
}

public interface ISiteAssetPrivider
{
    string HtmlContent();
}
