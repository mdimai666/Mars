namespace Mars.PxBlocks.Runtime.Ast;

/// <summary>logic_null — а также заглушка парсера для пустых сокетов-выражений.</summary>
public sealed record PxNullLiteral : PxExpression;

/// <summary>Числовая константа — дефолты парсера (пустой сокет шага цикла = 1 и т.п.).</summary>
public sealed record PxNumberLiteral(double Number) : PxExpression;
