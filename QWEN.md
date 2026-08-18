# Mars Project Context

Full overview: @ai/ProjectDescription.md

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
- `src/AppAdmin` — admin panel (Blazor WASM)
- `src/Plugin` — plugin system
- `docs/` — documentation site
- `ai/` — AI agent context files

## Build & Test

```
dotnet build Mars.slnx                                        # full solution build
dotnet test tests/Test.Mars.Host --verbosity minimal          # fast unit tests
dotnet test tests/Mars.Integration.Tests --verbosity minimal  # integration tests
```

## Conventions

- **Solution**: `Mars.slnx` — everything builds from here
- **Modules**: `src/Mars.Modules/<ModuleName>/` — each module is a self-contained project
- **Tests**: `tests/Test.Mars.*` (unit) and `tests/Mars.*.Integration.Tests` (integration)

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

## Language

Respond in Russian unless asked otherwise.
