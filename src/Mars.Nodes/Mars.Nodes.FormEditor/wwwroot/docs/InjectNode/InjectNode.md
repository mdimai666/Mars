# InjectNode

Starts a flow: manually from the editor, from the CLI, at startup, or on a schedule.

## Fields

The node emits a message with a list of fields. Each field has:

- **Key** — an identifier or a dot path. `Payload` (case-insensitive) goes to `msg.Payload`;
  any other key goes to the context and is available as `msg.<key>`.
- **Type** — value type: `int`, `long`, `float`, `double`, `decimal`, `bool`, `string`, `DateTime`,
  `Guid`, arrays (`int[]`, `string[]`, …) and `timestamp`.
- **Value** — interpreted according to the value kind:
  - `const` — a literal parsed by Type. Arrays and objects are entered as JSON text.
    `timestamp` expects unix milliseconds; empty means "now".
  - `msg` — a path into the incoming message, e.g. `Payload.Count` (resolved as `msg.<path>`).
  - `expression` — a C# expression (DynamicExpresso), e.g. `msg.Payload.Count() + 1`.
    Available roots: `msg`, `GlobalContext`, `FlowContext`, `VarNode`, `env("KEY")`; LINQ is supported.
    The result is converted to Type.

Field order is visual only (drag & drop); the context is a dictionary.

A parse or evaluation error fails the node with the field name and value in the message.

A newly added Inject node has one default field: `Payload` of type `timestamp` (current time).

## Value editor

In the form the value is typed as plain text (`const`) or with an `@` prefix (`expression`),
e.g. `@21 * 2` or `@msg.Payload.Count() + 1`. A field stored with kind `msg` is shown as `@msg.<path>`
and is saved back as an equivalent expression. In expression mode autocomplete (`Ctrl+Space`),
the field tree (`Alt+Down`) and the operations popup (`Alt+Up`) are available;
for a `DateTime` constant there is a date picker (`F4`).

## Startup and schedule

- **Run at startup** + **Delay millis** — run once after the node starts.
- **IsSchedule** + **cron mask** — Quartz cron expression, e.g. `0/5 * * * * ?` for every 5 seconds.

## JSON example

```json
{
  "TypeId": "core.InjectNode",
  "Fields": [
    { "Key": "Payload", "VarType": "string", "ValueKind": "const", "Value": "Hello!" },
    { "Key": "status", "VarType": "string", "Value": "ok" },
    { "Key": "doubled", "VarType": "int", "ValueKind": "expression", "Value": "21 * 2" }
  ]
}
```

## CLI

`./mars.exe node inject <name|id>` — starts the node with an empty incoming message
(default fields produce the current timestamp).
