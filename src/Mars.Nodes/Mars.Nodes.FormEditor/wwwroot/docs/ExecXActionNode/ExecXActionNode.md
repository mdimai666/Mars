# ExecXActionNode

Executes an XAction on the host machine.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CommandId` | `string` | `""` | Id of the XAction to execute |
| `Args` | `Dictionary<string, string>` | `[]` | Named call arguments (`name=value`, one per line) |

The result (`XActResult`, including effects) is passed to the output payload — the flow
decides what to do with it, no automatic chaining is performed.
