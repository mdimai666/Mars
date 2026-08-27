namespace Mars.Host.Shared.WebSite.Models;

public class RootPageBodyTagSplitter
{
    public const string BodyPlaceTag = "@Body";
    public string PreBody { get; }
    public string AfterBody { get; }

    public RootPageBodyTagSplitter(string rootLayoutContentWithBodyPlaceTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootLayoutContentWithBodyPlaceTag, nameof(rootLayoutContentWithBodyPlaceTag));
        var sp = rootLayoutContentWithBodyPlaceTag.Split(BodyPlaceTag, 2);
        if (sp.Length != 2)
        {
            throw new ArgumentException($"Body tag : {BodyPlaceTag} - not found");
        }

        PreBody = sp[0];
        AfterBody = sp[1];
    }

}
