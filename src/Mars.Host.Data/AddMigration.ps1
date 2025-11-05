if ($args.Count -lt 1) { Write-Error "Usage: .\AddMigration.ps1 {MigrationName}"; exit 1 }

$newName = $args[0]

dotnet ef migrations add "$newName" -o "Migrations" --startup-project ..\Mars.WebApp\Mars.WebApp.csproj
