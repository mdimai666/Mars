using Mars.PxBlocks.Shared.Toolbox;

namespace StandPxBlocksApp.Blocks;

/// <summary>Категория демо-домена в серверный toolbox (встаёт перед «Переменные»/«Функции»).</summary>
public static class PxDemoToolbox
{
    public static PxToolboxCategory CreateCategory() => new()
    {
        Name = "Тест типов", Colour = "#607D8B", Icon = "flask", Advanced = true,
        Items =
        [
            new PxToolboxLabel { Text = "Тест типов", WebClass = "blocklyFlyoutHeading" },
            new PxToolboxBlock { Type = "demostand.demo.create_object" },
            new PxToolboxBlock { Type = "demostand.demo.number" },
            new PxToolboxBlock { Type = "demostand.demo.string" },
            new PxToolboxBlock { Type = "demostand.demo.any" },
            new PxToolboxBlock { Type = "demostand.demo.object" },
            new PxToolboxBlock { Type = "demostand.demo.take_number" },
            new PxToolboxBlock { Type = "demostand.demo.take_any" },
            new PxToolboxBlock { Type = "demostand.demo.take_object" }
        ]
    };
}
