if ($args.Count -gt 1) { Write-Error "Usage: .\UpdateMigration.ps1 {MigrationName?}"; exit 1 }

$migration = if ($args[0]) { "$($args[0])" } else { $null }
dotnet ef database update $migration --startup-project ..\Mars.WebApp\Mars.WebApp.csproj
