namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>
/// Узел AST блочной программы. Каждый узел несёт id блока Blockly — для маппинга
/// «ошибка → блок» и подсветки исполняемого блока в редакторе.
/// </summary>
public abstract record PxNode
{
    /// <summary>Тип блока (PxBlockDefinition.TypeId / стандартный тип Blockly).</summary>
    public required string TypeId { get; init; }

    /// <summary>Id блока в workspace Blockly.</summary>
    public required string BlockId { get; init; }
}

/// <summary>Блок-оператор (statement): исполняется последовательно, имеет «хвост» next.</summary>
public abstract record PxStatement : PxNode
{
    /// <summary>Следующий блок стека (коннектор next Blockly).</summary>
    public PxStatement? Next { get; set; }
}

/// <summary>Блок-выражение (output): вычисляется в PxValue.</summary>
public abstract record PxExpression : PxNode;
