namespace Mars.Plugin.Front.Abstractions;

public class MarsFrontPluginManifest
{
    public const string DefaultManifestFileName = "_front_plugins.json";
    public string Version { get; set; } = "0.2";
    public bool IsDebug { get; set; }
    public Dictionary<string, FrontPluginInfo> Plugins { get; set; } = new();
}

public class EndpointJsonDtoSimplified
{
    public string Route { get; set; } = default!;
    public string AssetFile { get; set; } = default!;
}

public class FrontPluginStaticWebassets
{
    public EndpointJsonDtoSimplified[] Endpoints { get; set; } = default!;
}

public class FrontPluginInfo
{
    public string ProjectName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public FrontPluginStaticWebassets StaticWebassets { get; set; } = default!;
}
