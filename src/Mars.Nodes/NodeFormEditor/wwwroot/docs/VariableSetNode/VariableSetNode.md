# VariableSetNode

Set value for property

## Setters
- ValuePath - куда присвоить значение
- Expression - выражение
- Operation - операция

| ValuePath     | Expression    | Result    |
| ---------     | ----------    | -------   |
| msg.Payload   | 1+1           | 2         |
| msg.Payload.subprop   | 2           | 2         |
| GlobalContext.x | "222"       | "222"     |
| FlowContext.z | GlobalContext.val + 2 | 4 |