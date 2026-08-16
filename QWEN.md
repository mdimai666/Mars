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

## Mars CLI — thin client to a running instance

`Mars.exe` (Mars.WebApp) can control an already running instance: if a server is running
for the current directory, commands execute directly inside that live process over a unix
domain socket (no second startup); otherwise they run in-process.

- `Mars.exe status` — is the instance alive (pid, version, uptime); exit 1 if not.
- The command set is not fixed — discover it via `-h` and the sources
  (`src/Mars.Modules/Mars.CommandLine`, `src/Mars.WebApp/CommandLine`, CommandCli classes in modules).
- Commands mutate the LIVE instance — run them against a running server only with the user's confirmation.
- Flags: `--local` (run in-process even with a live server), `--no-uds` (start without the CLI socket), `--disable-logs`.
- In test mode (IsTesting / ASPNETCORE_ENVIRONMENT=Test) CLI arguments are ignored — this path is unavailable under tests.

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
