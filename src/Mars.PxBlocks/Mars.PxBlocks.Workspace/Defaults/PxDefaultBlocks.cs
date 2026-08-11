using Mars.PxBlocks.Shared.Definitions;

namespace Mars.PxBlocks.Workspace.Defaults;

/// <summary>
/// Демо-блоки по умолчанию: пример определений классами через наследование
/// (образец будущего декларативного слоя: определение отдельно, исполнение — в другой сборке).
/// </summary>
public static class PxDefaultBlocks
{
    public static IReadOnlyList<PxBlockDefinition> Create() =>
    [
        new PxDemoNumber(), new PxDemoString(), new PxDemoAny(), new PxDemoObjectValue(),
        new PxTakeNumber(), new PxTakeAny(), new PxTakeObject(),
        new PxCreateObject()
    ];

    private abstract class DemoValueBlock : PxBlockDefinition
    {
        protected DemoValueBlock(string typeId, string message, string outputType, string colour, PxArg? arg = null)
        {
            TypeId = typeId;
            Colour = colour;
            OutputType = outputType;
            Messages = [new PxMessageRow { Message = message, Args = arg != null ? [arg] : [] }];
        }
    }

    private sealed class PxDemoNumber : DemoValueBlock
    {
        public PxDemoNumber() : base("px_demo_number", "число %1", "Number", "#712672", new PxFieldNumber { Name = "NUM" }) { }
    }

    private sealed class PxDemoString : DemoValueBlock
    {
        public PxDemoString() : base("px_demo_string", "строка %1", "String", "#996600", new PxFieldText { Name = "TEXT", Text = "abc" }) { }
    }

    private sealed class PxDemoAny : DemoValueBlock
    {
        public PxDemoAny() : base("px_demo_any", "любое значение", "Any", "#5C2D91") { }
    }

    private sealed class PxDemoObjectValue : DemoValueBlock
    {
        public PxDemoObjectValue() : base("px_demo_object", "объект", "Object", "#A80000") { }
    }

    private abstract class DemoTakeBlock : PxBlockDefinition
    {
        protected DemoTakeBlock(string typeId, string message, params string[] check)
        {
            TypeId = typeId;
            Colour = "#107C10";
            Messages =
            [
                new PxMessageRow
                {
                    Message = message,
                    Args = [new PxValueInput { Name = "VAL", Check = [.. check] }]
                }
            ];
        }
    }

    private sealed class PxTakeNumber : DemoTakeBlock
    {
        public PxTakeNumber() : base("px_demo_take_number", "принять число %1", "Number") { }
    }

    private sealed class PxTakeAny : DemoTakeBlock
    {
        public PxTakeAny() : base("px_demo_take_any", "принять любое %1") { }
    }

    private sealed class PxTakeObject : DemoTakeBlock
    {
        public PxTakeObject() : base("px_demo_take_object", "принять объект %1", "Object") { }
    }

    private sealed class PxCreateObject : PxBlockDefinition
    {
        public PxCreateObject()
        {
            TypeId = "px_create_object";
            Colour = "#A80000";
            OutputType = "Object";
            Tooltip = "Кнопка «+» добавляет пары поле→значение";
            Messages = [new PxMessageRow { Message = "создать объект" }];
            Mutator = "px_object_builder";
        }
    }
}
