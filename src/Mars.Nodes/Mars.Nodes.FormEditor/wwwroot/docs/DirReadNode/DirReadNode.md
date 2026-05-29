# DirReadNode

A node for reading files from a directory.

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `DirPath` | `string` | Path to the directory |
| `Pattern` | `string` | Filter pattern: `*` — all files; `.txt,.json` — only specified extensions |
| `MaxDepth` | `int` | Traversal depth: `1` — root only; `0` — all nested files; `2` — including one level of nesting |
| `ReturnRelativePath` | `bool` | Return relative paths (default: `true`) |
| `UseRootGitIgnore` | `bool` | Use found `.gitignore` file |
