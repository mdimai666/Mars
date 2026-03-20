using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AutoFixture;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.JsonConverters;

var summary = BenchmarkRunner.Run<JsonSerializationBenchmarks>();

[MemoryDiagnoser]
[HtmlExporter]
[RPlotExporter]
public class JsonSerializationBenchmarks
{
    private PostDetail _postDetail = null!;
    private string _jsonString = null!;

    // Опции с стандартным резолвером (Default)
    private JsonSerializerOptions _optionsDefault = null!;

    // Опции с упорядоченным резолвером (Ordered)
    private JsonSerializerOptions _optionsOrdered = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixture = new Fixture();
        fixture.Create<PostDetail>();

        // Сериализуем один раз, чтобы было что десериализовать
        _jsonString = JsonSerializer.Serialize(_postDetail);

        _optionsDefault = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = false
        };

        _optionsOrdered = new JsonSerializerOptions
        {
            TypeInfoResolver = new OrderedPropertiesJsonTypeInfoResolver(),
            WriteIndented = false
        };

        // Прогрев (чтобы кэширование JsonTypeInfo произошло до замеров)
        JsonSerializer.Serialize(_postDetail, _optionsDefault);
        JsonSerializer.Serialize(_postDetail, _optionsOrdered);
        JsonSerializer.Deserialize<PostDetail>(_jsonString, _optionsDefault);
        JsonSerializer.Deserialize<PostDetail>(_jsonString, _optionsOrdered);
    }

    // --- Сериализация ---

    [Benchmark(Description = "Serialize (Default Resolver)")]
    public string Serialize_Default()
    {
        return JsonSerializer.Serialize(_postDetail, _optionsDefault);
    }

    [Benchmark(Description = "Serialize (Ordered Resolver)")]
    public string Serialize_Ordered()
    {
        return JsonSerializer.Serialize(_postDetail, _optionsOrdered);
    }

    // --- Десериализация ---

    [Benchmark(Description = "Deserialize (Default Resolver)")]
    public PostDetail Deserialize_Default()
    {
        return JsonSerializer.Deserialize<PostDetail>(_jsonString, _optionsDefault)!;
    }

    [Benchmark(Description = "Deserialize (Ordered Resolver)")]
    public PostDetail Deserialize_Ordered()
    {
        return JsonSerializer.Deserialize<PostDetail>(_jsonString, _optionsOrdered)!;
    }
}
