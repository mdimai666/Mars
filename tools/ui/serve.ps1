param(
    [int]$Port = 5005,
    [string]$Project = "src\Mars.WebApp",
    [switch]$Watch,
    [switch]$HotReload
)
# Start Mars server for tools/ui harness. Run with pwsh (PowerShell 7).
# Port must go through env Urls: ConfigureKestrel in CliSocketServer reads Urls from
# configuration and overrides ASPNETCORE_URLS / --urls.
# -Watch: dotnet watch — auto rebuild+restart on any C#/razor/css change (no manual
# stop-rebuild-start cycle; watch stops the app before building, so no locked assemblies).
# Default is --no-hot-reload: проверено 2026-11-12 на WASM-админке — hot reload deltas
# доходят только до живого браузера, свежая загрузка страницы (crawl/probe/responsive)
# берёт СТАРЫЕ сборки; scoped .razor.css referenced-проекта watch не применяет вовсе
# ('Scoped CSS bundle file Mars.Admin.bundle.scp.css not found'). Auto-rebuild даёт
# свежие ассеты всегда; браузер пользователя обновляется browser-refresh middleware.
# -HotReload: opt-in deltas-режим (быстрее для живого браузера, но харнесс видит старое).
# --non-interactive: never prompt on restart (agent-friendly).
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Urls = "http://localhost:$Port"
$proj = Join-Path $repo $Project
if ($Watch) {
    $watchArgs = @('--project', $proj, '--non-interactive')
    if (-not $HotReload) { $watchArgs += '--no-hot-reload' }
    $mode = if ($HotReload) { 'watch + hot reload' } else { 'watch + auto-rebuild' }
    Write-Host "Starting $Project on http://localhost:$Port (Development, $mode)"
    dotnet watch @watchArgs run --no-launch-profile
} else {
    Write-Host "Starting $Project on http://localhost:$Port (Development)"
    dotnet run --project $proj --no-launch-profile
}
