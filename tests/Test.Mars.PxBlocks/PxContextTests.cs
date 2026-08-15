using Mars.PxBlocks.Host.Services;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Shared.Definitions;
using Mars.PxBlocks.Shared.Toolbox;

namespace Test.Mars.PxBlocks;

/// <summary>Контексты редактора: fluent-построение, определения, toolbox, реестр.</summary>
public class PxContextTests
{
    [Fact]
    public void Define_FluentBuilds_ContextWithPolicy()
    {
        PxEditorContext context = PxEditorContext.Define("playwright")
            .Title("Playwright")
            .Description("Сценарии браузера")
            .Events("start", "loop")
            .StepLimit(1000)
            .OutputLimit(500);

        Assert.Equal("playwright", context.Name);
        Assert.Equal("Playwright", context.Title);
        Assert.Equal("Сценарии браузера", context.Description);
        Assert.Equal(["start", "loop"], context.EventNames);
        Assert.Equal(1000, context.StepLimit);
        Assert.Equal(500, context.OutputLimit);
    }

    [Fact]
    public void EffectiveDefinitions_ByDefault_IncludesEventBlocksAndSets()
    {
        PxEditorContext context = PxEditorContext.Define("demo").Set<ContextProbeSet>();

        var typeIds = context.EffectiveDefinitions.Select(d => d.TypeId).ToList();

        Assert.Contains("px_start", typeIds);
        Assert.Contains("px_loop", typeIds);
        Assert.Contains("test_context_probe", typeIds);
    }

    [Fact]
    public void WithoutEventBlocks_ExcludesEventBlocks()
    {
        PxEditorContext context = PxEditorContext.Define("filter")
            .WithoutEventBlocks()
            .Set<ContextProbeSet>();

        var typeIds = context.EffectiveDefinitions.Select(d => d.TypeId).ToList();

        Assert.DoesNotContain("px_start", typeIds);
        Assert.DoesNotContain("px_loop", typeIds);
        Assert.Contains("test_context_probe", typeIds);
    }

    [Fact]
    public void EffectiveToolbox_DomainCategoryBeforeSeparator()
    {
        PxEditorContext context = PxEditorContext.Define("demo")
            .Category(new PxToolboxCategory { Name = "Домен" });

        var contents = context.EffectiveToolbox.Contents;
        var domainIndex = contents.FindIndex(i => i is PxToolboxCategory { Name: "Домен" });
        var separatorIndex = contents.FindIndex(i => i is PxToolboxSeparator);

        Assert.True(domainIndex >= 0);
        Assert.True(domainIndex < separatorIndex);
    }

    [Fact]
    public void EffectiveToolbox_CustomReplacesDefault()
    {
        var custom = new PxToolbox
        {
            Contents = [new PxToolboxCategory { Name = "Выражения" }]
        };
        PxEditorContext context = PxEditorContext.Define("filter").Toolbox(custom);

        Assert.Same(custom, context.EffectiveToolbox);
    }

    [Fact]
    public void Registry_Get_IsCaseInsensitive()
    {
        var registry = new PxEditorContextRegistry();
        registry.Register(PxEditorContext.Define("Demo"));

        Assert.NotNull(registry.Get("demo"));
        Assert.NotNull(registry.Get("DEMO"));
        Assert.Null(registry.Get("нет-такого"));
    }

    [Fact]
    public void Registry_DuplicateName_Throws()
    {
        var registry = new PxEditorContextRegistry();
        registry.Register(PxEditorContext.Define("demo"));

        Assert.Throws<InvalidOperationException>(() => registry.Register(PxEditorContext.Define("demo")));
    }
}

/// <summary>Зонд-набор для контекстов (один блок-значение).</summary>
internal sealed class ContextProbeSet : PxBlockSet
{
    public ContextProbeSet()
    {
        Add(PxMaster.Define("test_context_probe").Output("Number").Message("зонд контекста"));
    }
}
