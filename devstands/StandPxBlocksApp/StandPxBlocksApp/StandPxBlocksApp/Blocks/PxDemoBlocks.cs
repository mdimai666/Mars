using Mars.PxBlocks.Shared.Definitions;

namespace StandPxBlocksApp.Blocks;

/// <summary>
/// Демо-домен: блоки объявляются ТОЛЬКО на сервере, в WASM их определений нет —
/// редактор получает их через api/PxBlocks/Definitions. Имена полей (NUM, TEXT, VAL)
/// сохраняются ради совместимости с уже сохранёнными workspace в localStorage.
/// </summary>
public sealed class PxDemoBlocks : PxBlockSet
{
    public PxDemoBlocks()
    {
        Add(PxMaster.Define("px_demo_number").Output("Number").Colour("#712672")
            .Message("число {NUM}", PxMaster.Number("NUM")));
        Add(PxMaster.Define("px_demo_string").Output("String").Colour("#996600")
            .Message("строка {TEXT}", PxMaster.Text("TEXT", "abc")));
        Add(PxMaster.Define("px_demo_any").Output("Any").Colour("#5C2D91")
            .Message("любое значение"));
        Add(PxMaster.Define("px_demo_object").Output("Object").Colour("#A80000")
            .Message("объект"));

        Add(PxMaster.Define("px_demo_take_number").Colour("#107C10")
            .Message("принять число {VAL}", PxMaster.Value("VAL", "Number")));
        Add(PxMaster.Define("px_demo_take_any").Colour("#107C10")
            .Message("принять любое {VAL}", PxMaster.Value("VAL")));
        Add(PxMaster.Define("px_demo_take_object").Colour("#107C10")
            .Message("принять объект {VAL}", PxMaster.Value("VAL", "Object")));

        Add(PxMaster.Define("px_create_object").Output("Object").Colour("#A80000")
            .Tooltip("Кнопка «+» добавляет пары поле→значение")
            .Message("создать объект")
            .Mutator("px_object_builder"));
    }
}
