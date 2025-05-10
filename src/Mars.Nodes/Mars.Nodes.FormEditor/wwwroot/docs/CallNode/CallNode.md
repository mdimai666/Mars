# CallNode

Creates a public node that can be called in functions.

At the end, you need to send a response via CallResponseNode

## Call in templates
```hbs
{{#context}}
node = Node("nodeName")
{{/context}}
```
