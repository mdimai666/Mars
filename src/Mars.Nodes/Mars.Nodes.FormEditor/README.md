# Markdown File

# https://github.com/serdarciplak/BlazorMonaco

```html
<head>
    <link href="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.css" rel="stylesheet" />
</head>
<body>
    ...
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>
    <script>require.config({ paths: { 'vs': '_content/BlazorMonaco/lib/monaco-editor/min/vs' } });</script>
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>
    <script src="_content/BlazorMonaco/jsInterop.js"></script>
    ...
</body>
```