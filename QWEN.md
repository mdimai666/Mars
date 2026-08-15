# Mars Project Context

Read @ai/ProjectDescription.md for a full overview of the Mars platform.

## Quick Summary

Mars is an open-source visual programming platform (inspired by Node-RED and WordPress) built on .NET 10 / Blazor. It combines:
- **Visual programming** — 55+ node types for flow-based workflows
- **Content management** — flexible PostTypes with 15 MetaField types
- **Multi-database** — PostgreSQL, MsSQL, MySQL
- **Plugin system** — .NET assemblies loaded at runtime
- **Docker, Scheduler, AI/Semantic Kernel, OpenTelemetry**

## Key Directories

- `src/Mars.WebApp` — main application
- `src/Mars.Nodes` — visual programming engine
- `src/AppAdmin` — admin panel (Blazor Server)
- `src/Plugin` — plugin system
- `docs/` — documentation site
- `ai/` — AI agent context files

## Codebase Knowledge Graph (codebase-memory MCP)

The repository is indexed as a knowledge graph by the `codebase-memory` MCP server
(project: `C-Users-D-Documents-VisualStudio-2025-Mars`; confirm via `list_projects`).
Use it for STRUCTURAL questions that grep can't answer:

- `search_graph` / `get_code_snippet` — find symbol definitions, read their source
- `trace_path` — call chains: who calls X / what X calls
- `detect_changes` — blast radius of a diff before refactoring
- `search_code` — large usage searches, deduplicated into functions
- `get_architecture` — high-level overview of the solution

Do NOT use it for reading known files (`read_file`) or when the exact current text
matters (`grep_search`) — the live tree is always the source of truth.

The index is a SNAPSHOT and can go stale. If graph results contradict the live code,
re-index: `index_repository(repo_path=<repo root>, mode="full")`.

## Language

Respond in Russian unless asked otherwise.
