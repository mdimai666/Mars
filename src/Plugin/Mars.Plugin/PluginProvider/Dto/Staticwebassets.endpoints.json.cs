using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Mars.Plugin.PluginProvider.Dto;

public class StaticwebassetsEndpointsManifestJson
{
    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("ManifestType")]
    public string ManifestType { get; set; } = "Publish";

    [JsonPropertyName("Endpoints")]
    public List<EndpointJsonDto> Endpoints { get; set; } = [];
}

[DebuggerDisplay("{AssetFile}={Route}")]
public class EndpointJsonDto
{
    [JsonPropertyName("Route")]
    public string Route { get; set; } = default!;

    [JsonPropertyName("AssetFile")]
    public string AssetFile { get; set; } = default!;

    [JsonPropertyName("Selectors")]
    public List<SelectorJsonDto> Selectors { get; set; } = [];

    [JsonPropertyName("ResponseHeaders")]
    public List<ResponseHeaderJsonDto> ResponseHeaders { get; set; } = [];

    [JsonPropertyName("EndpointProperties")]
    public List<EndpointPropertyJsonDto> EndpointProperties { get; set; } = [];
}

public class SelectorJsonDto
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("Value")]
    public string Value { get; set; } = default!;

    [JsonPropertyName("Quality")]
    public string Quality { get; set; } = default!;
}

public class ResponseHeaderJsonDto
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("Value")]
    public string Value { get; set; } = default!;
}

public class EndpointPropertyJsonDto
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("Value")]
    public string Value { get; set; } = default!;
}

//public class EndpointJsonDtoSimplified
//{
//    [JsonPropertyName("Route")]
//    public string Route { get; set; } = default!;

//    [JsonPropertyName("AssetFile")]
//    public string AssetFile { get; set; } = default!;
//}
