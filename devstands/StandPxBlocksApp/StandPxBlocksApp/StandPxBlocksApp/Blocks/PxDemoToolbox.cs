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
            new PxToolboxBlock { Type = "px_create_object" },
            new PxToolboxBlock { Type = "px_demo_number" },
            new PxToolboxBlock { Type = "px_demo_string" },
            new PxToolboxBlock { Type = "px_demo_any" },
            new PxToolboxBlock { Type = "px_demo_object" },
            new PxToolboxBlock { Type = "px_demo_take_number" },
            new PxToolboxBlock { Type = "px_demo_take_any" },
            new PxToolboxBlock { Type = "px_demo_take_object" }
        ]
    };
}
