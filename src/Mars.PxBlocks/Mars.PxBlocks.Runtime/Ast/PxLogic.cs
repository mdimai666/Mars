namespace Mars.PxBlocks.Runtime.Ast;

public enum PxLogicOp
{
    And,
    Or
}

/// <summary>logic_operation — в ядре: короткое замыкание нельзя отдать листьям.</summary>
public sealed record PxLogicOperation : PxExpression
{
    public PxLogicOp Op { get; init; } = PxLogicOp.And;

    public required PxExpression Left { get; init; }

    public required PxExpression Right { get; init; }
}

/// <summary>logic_ternary — в ядре: невыбранная ветка не вычисляется.</summary>
public sealed record PxLogicTernary : PxExpression
{
    public required PxExpression Condition { get; init; }

    public required PxExpression Then { get; init; }

    public required PxExpression Else { get; init; }
}
