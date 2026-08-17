namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>Программа, собранная парсером из Blockly JSON (PxWorkspaceState.BlocksJson).</summary>
public sealed class PxProgram
{
    /// <summary>Переменные workspace (Blockly serialization → variables).</summary>
    public List<PxVariableDecl> Variables { get; set; } = [];

    /// <summary>Определения функций; исполняются не по месту, а при вызове.</summary>
    public List<PxProcedureDef> Procedures { get; set; } = [];

    /// <summary>Определения функций MakeCode (function_definition); туда же при вызове.</summary>
    public List<PxFunctionDef> Functions { get; set; } = [];

    /// <summary>Верхнеуровневые стеки операторов (без определений функций).</summary>
    public List<PxStatement> TopLevel { get; set; } = [];
}

public sealed record PxVariableDecl(string Id, string Name);
