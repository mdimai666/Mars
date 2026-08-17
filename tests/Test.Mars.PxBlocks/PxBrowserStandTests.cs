using Mars.PxBlocks.Host.Services;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Parsing;
using Mars.PxBlocks.Shared.Toolbox;
using StandPxBlocksApp.Blocks.Browser;

namespace Test.Mars.PxBlocks;

/// <summary>
/// Браузерный домен стенда (контекст «browser»): состав контекста, разбор
/// примера сценария реальными имплементациями стенда, фильтр событийных
/// блоков контекста и владение состоянием запуска у PxRunManager.
/// </summary>
public class PxBrowserStandTests
{
    [Fact]
    public void BrowserContext_Definitions_StartWithoutLoop()
    {
        var context = PxBrowserContext.Create();

        var typeIds = context.EffectiveDefinitions.Select(d => d.TypeId).ToList();

        Assert.Equal("browser", context.Name);
        Assert.Equal([PxEvents.Start], context.EventNames);
        Assert.Contains("core.events.start", typeIds);
        Assert.DoesNotContain("core.events.loop", typeIds);
        Assert.Contains("demostand.playwright.goto", typeIds);
        Assert.Contains("demostand.playwright.print_texts", typeIds);
    }

    [Fact]
    public void BrowserContext_Toolbox_HasBrowserCategoryWithoutLoop()
    {
        var context = PxBrowserContext.Create();

        var toolboxBlockTypes = context.EffectiveToolbox.Contents
            .OfType<PxToolboxCategory>()
            .SelectMany(category => category.Items)
            .OfType<PxToolboxBlock>()
            .Select(block => block.Type)
            .ToList();

        Assert.Contains("core.events.start", toolboxBlockTypes);
        Assert.DoesNotContain("core.events.loop", toolboxBlockTypes);
        Assert.Contains("demostand.playwright.goto", toolboxBlockTypes);

        var browserCategory = context.EffectiveToolbox.Contents
            .OfType<PxToolboxCategory>()
            .Single(category => category.Name == "Browser");
        Assert.Equal("globe", browserCategory.Icon);
    }

    [Fact]
    public void BrowserSample_Parses_WithStandImplements()
    {
        // Как PxBlockCatalog: стандартные листья (text и т.д.) + сборки хоста.
        var locator = PxInterpreter.CreateDefaultImplements();
        locator.RegisterAssembly(typeof(PxBrowserContext).Assembly);

        var program = new PxParser(locator).Parse(PxBrowserSample.WikipediaSearchJson);

        var start = Assert.Single(program.TopLevel.OfType<PxEventBlock>());
        Assert.Equal(PxEvents.Start, start.EventName);
        Assert.NotNull(start.Body);
    }

    [Fact]
    public void EventBlocks_Filter_SelectsEventDefinitions()
    {
        var startOnly = PxEditorContext.Define("a").EventBlocks(PxEvents.Start).Build();
        Assert.Contains("core.events.start", startOnly.EffectiveDefinitions.Select(d => d.TypeId));
        Assert.DoesNotContain("core.events.loop", startOnly.EffectiveDefinitions.Select(d => d.TypeId));

        var none = PxEditorContext.Define("b").WithoutEventBlocks().Build();
        Assert.DoesNotContain("core.events.start", none.EffectiveDefinitions.Select(d => d.TypeId));
        Assert.DoesNotContain("core.events.loop", none.EffectiveDefinitions.Select(d => d.TypeId));

        var both = PxEditorContext.Define("c").Build();
        Assert.Contains("core.events.start", both.EffectiveDefinitions.Select(d => d.TypeId));
        Assert.Contains("core.events.loop", both.EffectiveDefinitions.Select(d => d.TypeId));
    }

    [Fact]
    public void Context_AlwaysIncludesStandardBlocks()
    {
        // Сервер — единый источник определений: стандартные категории языка
        // (core.*) приходят в каждый контекст вместе с событийными и доменными.
        var typeIds = PxEditorContext.Define("std").Build().EffectiveDefinitions.Select(d => d.TypeId).ToList();

        Assert.Contains("core.logic.if", typeIds);
        Assert.Contains("core.loops.repeat", typeIds);
        Assert.Contains("core.math.number", typeIds);
        Assert.Contains("core.text.text", typeIds);
        Assert.Contains("core.variables.get", typeIds);
        Assert.Contains("core.variables.set", typeIds);
    }

    [Fact]
    public void RunManager_DisposesState_WhenContextUnknown()
    {
        var catalog = new PxBlockCatalog();
        var manager = new PxRunManager(catalog, new FakeBroadcaster(), new PxEditorContextRegistry());
        var state = new DisposeTrackingState();

        var response = manager.Start(
            new PxRunRequest { BlocksJson = "{}", ContextName = "нет-такого" }, state);

        Assert.False(response.Started);
        Assert.True(state.Disposed);
    }

    private sealed class DisposeTrackingState : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
