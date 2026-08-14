using System.Text.Json.Nodes;
using Mars.PxBlocks.Runtime.Ast;
using Mars.PxBlocks.Runtime.Execution;

namespace Mars.PxBlocks.Runtime.Parsing;

/// <summary>
/// Blockly JSON (PxWorkspaceState.BlocksJson) → AST. Форматы — по blockly 13.1.1:
/// поля-переменные как {"id": …}, extraState мутаторов (controls_if, procedures,
/// text_join), отключённые блоки помечены disabledReasons. Структурные блоки
/// (control flow, переменные, функции, короткое замыкание) уходят в узлы ядра;
/// остальные обязаны быть в локаторе имплементаций, иначе — ошибка с id блока.
/// </summary>
public sealed class PxParser
{
    private readonly PxBlockImplementsLocator? _implements;
    private PxProgram _program = new();

    public PxParser(PxBlockImplementsLocator? implements = null)
        => _implements = implements;

    /// <summary>Парсер со стандартным набором листьев (сборка Mars.PxBlocks.Runtime).</summary>
    public static PxParser CreateDefault() => new(PxInterpreter.CreateDefaultImplements());

    public PxProgram Parse(string blocksJson)
    {
        var root = JsonNode.Parse(blocksJson) ?? throw new PxParseException("Пустой workspace JSON");
        return Parse(root);
    }

    public PxProgram Parse(JsonNode root)
    {
        var rootObject = root as JsonObject ?? throw new PxParseException("Workspace JSON не является объектом");
        _program = new PxProgram();

        if (rootObject["variables"] is JsonArray variables)
        {
            foreach (var variable in variables)
            {
                if (variable is not JsonObject variableObject)
                    continue;
                _program.Variables.Add(new PxVariableDecl(
                    (string?)variableObject["id"] ?? "",
                    (string?)variableObject["name"] ?? ""));
            }
        }

        var blocks = rootObject["blocks"] switch
        {
            JsonObject wrapper when wrapper["blocks"] is JsonArray inner => inner,
            JsonArray direct => direct,
            _ => throw new PxParseException("В workspace JSON нет списка блоков")
        };

        foreach (var block in blocks)
        {
            if (block is not JsonObject blockObject || IsDisabled(blockObject))
                continue;

            var type = BlockType(blockObject);
            if (type is PxCoreBlocks.ProceduresDefNoReturn or PxCoreBlocks.ProceduresDefReturn)
            {
                // Определения функций исполняются не по месту — ParseProcedureDef
                // сам кладёт их в PxProgram.Procedures.
                ParseProcedureDef(blockObject, type);
                continue;
            }

            if (ParseStatement(blockObject) is { } statement)
                _program.TopLevel.Add(statement);
        }

        return _program;
    }

    private PxStatement? ParseStatement(JsonObject block)
    {
        if (IsDisabled(block))
            return NextStatement(block);

        var type = BlockType(block);
        var blockId = BlockId(block);

        PxStatement node = type switch
        {
            PxCoreBlocks.If or PxCoreBlocks.IfElse => ParseIf(block, type, blockId),

            PxCoreBlocks.RepeatExt => new PxRepeatStatement
            {
                TypeId = type,
                BlockId = blockId,
                Times = ExpressionInput(block, "TIMES") ?? NullLiteral(blockId),
                Body = StatementInput(block, "DO")
            },

            PxCoreBlocks.WhileUntil => new PxWhileUntilStatement
            {
                TypeId = type,
                BlockId = blockId,
                Mode = FieldText(block, "MODE") == "UNTIL" ? PxWhileMode.Until : PxWhileMode.While,
                Condition = ExpressionInput(block, "BOOL") ?? NullLiteral(blockId),
                Body = StatementInput(block, "DO")
            },

            PxCoreBlocks.For => new PxForStatement
            {
                TypeId = type,
                BlockId = blockId,
                VariableId = FieldVariableId(block, "VAR", blockId),
                From = ExpressionInput(block, "FROM") ?? NullLiteral(blockId),
                To = ExpressionInput(block, "TO") ?? NullLiteral(blockId),
                By = ExpressionInput(block, "BY") ?? new PxNumberLiteral(1) { TypeId = PxCoreBlocks.LogicNull, BlockId = blockId },
                Body = StatementInput(block, "DO")
            },

            PxCoreBlocks.ForEach => new PxForEachStatement
            {
                TypeId = type,
                BlockId = blockId,
                VariableId = FieldVariableId(block, "VAR", blockId),
                List = ExpressionInput(block, "LIST") ?? NullLiteral(blockId),
                Body = StatementInput(block, "DO")
            },

            PxCoreBlocks.FlowStatements => new PxFlowStatement
            {
                TypeId = type,
                BlockId = blockId,
                Kind = FieldText(block, "FLOW") == "CONTINUE" ? PxFlowKind.Continue : PxFlowKind.Break
            },

            PxCoreBlocks.StartEvent or PxCoreBlocks.LoopEvent => new PxEventBlock
            {
                TypeId = type,
                BlockId = blockId,
                EventName = type == PxCoreBlocks.StartEvent ? PxEvents.Start : PxEvents.Loop,
                Body = StatementInput(block, "DO")
            },

            PxCoreBlocks.VariablesSet => new PxVariableSet
            {
                TypeId = type,
                BlockId = blockId,
                VariableId = FieldVariableId(block, "VAR", blockId),
                Value = ExpressionInput(block, "VALUE") ?? NullLiteral(blockId)
            },

            PxCoreBlocks.IfReturn => new PxIfReturnStatement
            {
                TypeId = type,
                BlockId = blockId,
                Condition = ExpressionInput(block, "CONDITION") ?? NullLiteral(blockId),
                Value = ExpressionInput(block, "VALUE")
            },

            PxCoreBlocks.ProceduresDefNoReturn or PxCoreBlocks.ProceduresDefReturn => ParseProcedureDef(block, type),

            PxCoreBlocks.ProceduresCallNoReturn => ParseProcedureCallStatement(block, blockId),

            _ => LeafStatement(block, type, blockId)
        };

        node.Next = NextStatement(block);
        return node;
    }

    private PxExpression ParseExpression(JsonObject block)
    {
        if (IsDisabled(block))
            return NullLiteral(BlockId(block));

        var type = BlockType(block);
        var blockId = BlockId(block);

        return type switch
        {
            PxCoreBlocks.VariablesGet => new PxVariableGet
            {
                TypeId = type,
                BlockId = blockId,
                VariableId = FieldVariableId(block, "VAR", blockId)
            },

            PxCoreBlocks.LogicNull => NullLiteral(blockId),

            PxCoreBlocks.LogicOperation => new PxLogicOperation
            {
                TypeId = type,
                BlockId = blockId,
                Op = FieldText(block, "OP") == "OR" ? PxLogicOp.Or : PxLogicOp.And,
                Left = ExpressionInput(block, "A") ?? NullLiteral(blockId),
                Right = ExpressionInput(block, "B") ?? NullLiteral(blockId)
            },

            PxCoreBlocks.LogicTernary => new PxLogicTernary
            {
                TypeId = type,
                BlockId = blockId,
                Condition = ExpressionInput(block, "IF") ?? NullLiteral(blockId),
                Then = ExpressionInput(block, "THEN") ?? NullLiteral(blockId),
                Else = ExpressionInput(block, "ELSE") ?? NullLiteral(blockId)
            },

            PxCoreBlocks.ProceduresCallReturn => ParseProcedureCallExpression(block, blockId),

            _ => LeafExpression(block, type, blockId)
        };
    }

    private PxIfStatement ParseIf(JsonObject block, string type, string blockId)
    {
        var elseIfCount = 0;
        var hasElse = type == PxCoreBlocks.IfElse;
        if (block["extraState"] is JsonObject extra)
        {
            if (extra["elseIfCount"] is JsonValue countValue && countValue.TryGetValue<int>(out var count))
                elseIfCount = count;
            if (extra["hasElse"] is JsonValue elseValue && elseValue.TryGetValue<bool>(out var has))
                hasElse = has;
        }

        var branches = new List<PxIfBranch>();
        for (var i = 0; i <= elseIfCount; i++)
        {
            branches.Add(new PxIfBranch(
                ExpressionInput(block, $"IF{i}") ?? NullLiteral(blockId),
                StatementInput(block, $"DO{i}")));
        }

        return new PxIfStatement
        {
            TypeId = type,
            BlockId = blockId,
            Branches = branches,
            ElseBody = hasElse ? StatementInput(block, "ELSE") : null
        };
    }

    private PxProcedureDef ParseProcedureDef(JsonObject block, string type)
    {
        var blockId = BlockId(block);
        var parameters = new List<PxParam>();
        if (block["extraState"] is JsonObject extra && extra["params"] is JsonArray paramsArray)
        {
            foreach (var parameter in paramsArray)
            {
                if (parameter is JsonObject parameterObject)
                {
                    parameters.Add(new PxParam(
                        (string?)parameterObject["id"] ?? "",
                        (string?)parameterObject["name"] ?? ""));
                }
            }
        }

        var definition = new PxProcedureDef
        {
            TypeId = type,
            BlockId = blockId,
            Name = FieldText(block, "NAME"),
            Params = parameters,
            Body = StatementInput(block, "STACK"),
            Return = type == PxCoreBlocks.ProceduresDefReturn ? ExpressionInput(block, "RETURN") : null
        };

        // Определение может встретиться и внутри стека — регистрируем сразу.
        if (_program.Procedures.TrueForAll(p => p.Name != definition.Name))
            _program.Procedures.Add(definition);
        return definition;
    }

    private PxProcedureCallStatement ParseProcedureCallStatement(JsonObject block, string blockId)
    {
        var (name, args) = ParseProcedureCall(block, blockId);
        return new PxProcedureCallStatement
        {
            TypeId = PxCoreBlocks.ProceduresCallNoReturn,
            BlockId = blockId,
            Name = name,
            Args = args
        };
    }

    private PxProcedureCallExpression ParseProcedureCallExpression(JsonObject block, string blockId)
    {
        var (name, args) = ParseProcedureCall(block, blockId);
        return new PxProcedureCallExpression
        {
            TypeId = PxCoreBlocks.ProceduresCallReturn,
            BlockId = blockId,
            Name = name,
            Args = args
        };
    }

    /// <summary>Вызов функции: имя и число аргументов — в extraState; входы ARG0…ARGn.</summary>
    private (string Name, List<PxExpression> Args) ParseProcedureCall(JsonObject block, string blockId)
    {
        if (block["extraState"] is not JsonObject extra)
            throw new PxParseException("Вызов функции без имени (extraState)", blockId);

        var name = (string?)extra["name"] ?? "";
        var argCount = extra["params"] is JsonArray parameters ? parameters.Count : 0;

        var args = new List<PxExpression>(argCount);
        for (var i = 0; i < argCount; i++)
            args.Add(ExpressionInput(block, $"ARG{i}") ?? NullLiteral(blockId));

        return (name, args);
    }

    private PxLeafStatement LeafStatement(JsonObject block, string type, string blockId)
    {
        EnsureImplemented(type, blockId);
        return new PxLeafStatement
        {
            TypeId = type,
            BlockId = blockId,
            Inputs = ParseInputs(block),
            Fields = ParseFields(block),
            ExtraState = block["extraState"]
        };
    }

    private PxLeafExpression LeafExpression(JsonObject block, string type, string blockId)
    {
        EnsureImplemented(type, blockId);
        return new PxLeafExpression
        {
            TypeId = type,
            BlockId = blockId,
            Inputs = ParseInputs(block),
            Fields = ParseFields(block),
            ExtraState = block["extraState"]
        };
    }

    private void EnsureImplemented(string type, string blockId)
    {
        if (_implements?.Find(type) != null)
            return;
        throw new PxParseException($"Неизвестный блок '{type}' — реализация исполнения не зарегистрирована", blockId);
    }

    private List<(string Name, PxExpression Expr)> ParseInputs(JsonObject block)
    {
        var inputs = new List<(string, PxExpression)>();
        if (block["inputs"] is not JsonObject inputsObject)
            return inputs;

        foreach (var (name, input) in inputsObject)
        {
            if (input is not JsonObject inputObject)
                continue;

            var inner = inputObject["block"] as JsonObject ?? inputObject["shadow"] as JsonObject;
            if (inner != null)
                inputs.Add((name, ParseExpression(inner)));
        }

        return inputs;
    }

    private static Dictionary<string, PxFieldData> ParseFields(JsonObject block)
    {
        var fields = new Dictionary<string, PxFieldData>(StringComparer.Ordinal);
        if (block["fields"] is not JsonObject fieldsObject)
            return fields;

        foreach (var (name, value) in fieldsObject)
        {
            fields[name] = value switch
            {
                // Поле-переменная: объект с id (blockly 13) либо строка-id (старые форматы).
                JsonObject obj when obj["id"] != null => PxFieldData.OfVariable(obj["id"]!.ToString()),
                JsonValue v when v.TryGetValue(out double number) => PxFieldData.OfNumber(number),
                JsonValue v when v.TryGetValue(out string? text) => PxFieldData.OfText(text ?? ""),
                JsonValue v => PxFieldData.OfText(v.ToJsonString()),
                _ => new PxFieldData()
            };
        }

        return fields;
    }

    private PxExpression? ExpressionInput(JsonObject block, string inputName)
        => InputBlock(block, inputName) is { } inner ? ParseExpression(inner) : null;

    private PxStatement? StatementInput(JsonObject block, string inputName)
        => InputBlock(block, inputName) is { } inner ? ParseStatement(inner) : null;

    private PxStatement? NextStatement(JsonObject block)
        => block["next"] is JsonObject next && next["block"] is JsonObject inner
            ? ParseStatement(inner)
            : null;

    private static JsonObject? InputBlock(JsonObject block, string inputName)
    {
        if (block["inputs"] is not JsonObject inputs || inputs[inputName] is not JsonObject input)
            return null;
        return input["block"] as JsonObject ?? input["shadow"] as JsonObject;
    }

    private static string FieldText(JsonObject block, string fieldName)
    {
        if (block["fields"] is not JsonObject fields || fields[fieldName] is not JsonValue value)
            return "";
        return value.TryGetValue(out string? text) ? text ?? "" : value.ToJsonString();
    }

    private static string FieldVariableId(JsonObject block, string fieldName, string blockId)
    {
        var node = block["fields"] is JsonObject fields ? fields[fieldName] : null;
        var variableId = node switch
        {
            JsonObject obj => (string?)obj["id"],
            JsonValue value when value.TryGetValue(out string? id) => id,
            _ => null
        };

        return variableId ?? throw new PxParseException($"Поле '{fieldName}' не ссылается на переменную", blockId);
    }

    /// <summary>blockly 13 помечает отключённые блоки disabledReasons; старый формат — enabled: false.</summary>
    private static bool IsDisabled(JsonObject block)
    {
        if (block["disabledReasons"] is JsonArray { Count: > 0 })
            return true;
        return block["enabled"] is JsonValue enabled && enabled.TryGetValue<bool>(out var isEnabled) && !isEnabled;
    }

    private static string BlockType(JsonObject block)
        => (string?)block["type"] ?? throw new PxParseException("Встречен блок без типа", BlockId(block));

    private static string BlockId(JsonObject block) => (string?)block["id"] ?? "";

    private static PxNullLiteral NullLiteral(string blockId)
        => new() { TypeId = PxCoreBlocks.LogicNull, BlockId = blockId };
}
