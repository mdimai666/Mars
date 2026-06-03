using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Mars.TemplateEngine.Host.InternalProviders;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using Mars.TemplateEngine.Providers.ScribanProvider;

BenchmarkRunner.Run<TemplateEnginesBenchmark>();

// [MemoryDiagnoser] включает детальный замер выделяемой памяти (аллокаций GC)
[MemoryDiagnoser]
// Сортирует результаты в итоговой таблице по скорости выполнения
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[ShortRunJob]
public class TemplateEnginesBenchmark
{
    private TextReplaceTemplateEngine _regexEngine = null!;
    private HandlebarsTemplateEngine _handlebarsEngine = null!;
    private ScribanTemplateEngine _scribanEngine = null!;
    private ScribanRazorStyleTemplateEngine _scribanRazorStyleEngine = null!;

    private object _context = null!;

    private string _regexTemplate = null!;
    private string _handlebarsTemplate = null!;
    private string _scribanTemplate = null!;
    private string _scribanRazorTemplate = null!;

    // [GlobalSetup] выполняется один раз перед стартом всех тестов для подготовки данных
    [GlobalSetup]
    public void Setup()
    {
        _regexEngine = new TextReplaceTemplateEngine();
        _handlebarsEngine = new HandlebarsTemplateEngine();
        _scribanEngine = new ScribanTemplateEngine();
        _scribanRazorStyleEngine = new ScribanRazorStyleTemplateEngine();

        // Общий контекст для рендеринга
        _context = new
        {
            User = "Александр",
            Status = "Премиум",
            IsActive = true
        };

        // Шаблоны с одинаковой логикой под каждый синтаксис
        _regexTemplate = "Привет, {User}! Твой статус: {Status}.";
        _handlebarsTemplate = "Привет, {{User}}! Твой статус: {{Status}}.";
        _scribanTemplate = "Привет, {{ User }}! Твой статус: {{ Status }}.";
        _scribanRazorTemplate = "Привет, @User! Твой статус: @Status.";

        // Принудительно прогреваем кэш для "горячих" тестов
        _regexEngine.RenderCached("warm_id", _regexTemplate, _context);
        _handlebarsEngine.RenderCached("warm_id", _handlebarsTemplate, _context);
        _scribanEngine.RenderCached("warm_id", _scribanTemplate, _context);
        _scribanRazorStyleEngine.RenderCached("warm_id", _scribanRazorTemplate, _context);
    }

    // ==========================================
    // СЕКЦИЯ 1: ХОЛОДНЫЙ ЗАПУСК (Без кэша / Первая компиляция)
    // ==========================================

    [Benchmark(Description = "Regex (Cold)")]
    public string RegexCold() => _regexEngine.Render(_regexTemplate, _context);

    [Benchmark(Description = "Handlebars (Cold)")]
    public string HandlebarsCold() => _handlebarsEngine.Render(_handlebarsTemplate, _context);

    [Benchmark(Description = "Scriban (Cold)")]
    public string ScribanCold() => _scribanEngine.Render(_scribanTemplate, _context);

    [Benchmark(Description = "RazorStyle (Cold)")]
    public string RazorStyleCold() => _scribanRazorStyleEngine.Render(_scribanRazorTemplate, _context);

    // ==========================================
    // СЕКЦИЯ 2: ГОРЯЧИЙ ЗАПУСК (Через кэш / Happy Path)
    // ==========================================

    [Benchmark(Description = "Regex (Cached)")]
    public string RegexCached() => _regexEngine.RenderCached("warm_id", _regexTemplate, _context);

    [Benchmark(Description = "Handlebars (Cached)")]
    public string HandlebarsCached() => _handlebarsEngine.RenderCached("warm_id", _handlebarsTemplate, _context);

    [Benchmark(Description = "Scriban (Cached)")]
    public string ScribanCached() => _scribanEngine.RenderCached("warm_id", _scribanTemplate, _context);

    [Benchmark(Description = "RazorStyle (Cached)")]
    public string RazorStyleCached() => _scribanRazorStyleEngine.RenderCached("warm_id", _scribanRazorTemplate, _context);
}
