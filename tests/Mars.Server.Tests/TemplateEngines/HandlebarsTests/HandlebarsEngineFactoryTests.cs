using FluentAssertions;
using HandlebarsDotNet;
using Mars.TemplateEngine.Providers.HandlebarsProvider;

namespace Mars.Server.Tests.TemplateEngines.HandlebarsTests;

/// <summary>
/// Контракт-тесты механизма фабрики/контрибьюторов Providers.Handlebars:
/// фильтрация по scope, null-scope для всех, колбэк конфигурации.
/// </summary>
public class HandlebarsEngineFactoryTests
{
    class TestContributor : IHandlebarsBuilderContributor
    {
        readonly string _helperName;

        public TestContributor(string? scope, string helperName)
        {
            Scope = scope;
            _helperName = helperName;
        }

        public string? Scope { get; }

        public void Configure(IHandlebars handlebars)
            => handlebars.RegisterHelper(_helperName,
                (EncodedTextWriter output, Context context, Arguments arguments) => output.WriteSafeString("contributed"));
    }

    [Fact]
    public void Create_ScopeContributor_AppliedOnlyToMatchingScope()
    {
        var factory = new HandlebarsEngineFactory([new TestContributor(HandlebarsScopes.Site, "site_helper")]);

        var siteHb = factory.Create(HandlebarsScopes.Site);
        siteHb.Compile("{{site_helper}}")(new { }, null).Should().Be("contributed");

        var coreHb = factory.Create(HandlebarsScopes.Core);
        coreHb.Compile("{{site_helper}}")(new { }, null).Should().BeEmpty("site-контрибьютор не должен попадать в core scope");
    }

    [Fact]
    public void Create_NullScopeContributor_AppliedToAllScopes()
    {
        var factory = new HandlebarsEngineFactory([new TestContributor(null, "any_helper")]);

        foreach (var scope in new[] { HandlebarsScopes.Core, HandlebarsScopes.Site, "plugin.custom" })
        {
            var hb = factory.Create(scope);
            hb.Compile("{{any_helper}}")(new { }, null).Should().Be("contributed");
        }
    }

    [Fact]
    public void Create_ScopeMatch_IsCaseInsensitive()
    {
        var factory = new HandlebarsEngineFactory([new TestContributor("SITE", "site_helper")]);

        var hb = factory.Create("site");
        hb.Compile("{{site_helper}}")(new { }, null).Should().Be("contributed");
    }

    [Fact]
    public void Create_ConfigureConfiguration_AppliedBeforeEngineCreation()
    {
        var factory = new HandlebarsEngineFactory([]);

        var hb = factory.Create(HandlebarsScopes.Core, cfg => cfg.NoEscape = true);
        hb.Compile("{{x}}")(new { x = "<b>" }, null).Should().Be("<b>");

        var hbEscaped = factory.Create(HandlebarsScopes.Core);
        hbEscaped.Compile("{{x}}")(new { x = "<b>" }, null).Should().NotContain("<b>");
    }

    [Fact]
    public void TemplateEngine_UsesFactoryContributors_ForCoreScope()
    {
        var factory = new HandlebarsEngineFactory([new TestContributor(HandlebarsScopes.Core, "core_helper")]);
        var engine = new HandlebarsTemplateEngine(factory);

        engine.Render("{{core_helper}}", new { }).Should().Be("contributed");
    }
}
