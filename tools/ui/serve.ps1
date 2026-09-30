param(
    [int]$Port = 5005,
    [string]$Project = "src\Mars.WebApp"
)
# Start Mars server for tools/ui harness. Run with pwsh (PowerShell 7).
# Port must go through env Urls: ConfigureKestrel in CliSocketServer reads Urls from
# configuration and overrides ASPNETCORE_URLS / --urls.
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Urls = "http://localhost:$Port"
Write-Host "Starting $Project on http://localhost:$Port (Development)"
dotnet run --project (Join-Path $repo $Project) --no-launch-profile
