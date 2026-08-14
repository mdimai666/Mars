namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>variables_get — значение переменной.</summary>
public sealed record PxVariableGet : PxExpression
{
    public required string VariableId { get; init; }
}

/// <summary>variables_set — присвоить переменной.</summary>
public sealed record PxVariableSet : PxStatement
{
    public required string VariableId { get; init; }

    public required PxExpression Value { get; init; }
}
