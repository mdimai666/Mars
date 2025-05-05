$__DIR__ = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\')


dotnet pack --configuration Release
$file = $(gci "$__DIR__\bin\Release\*.nupkg" | sort CreationTime -Descending | Select-Object -First 1).Name
echo $file
$apikey = [System.Environment]::GetEnvironmentVariable('NUGET_MARS')
dotnet nuget push "$__DIR__\bin\Release\$file" --api-key $apikey --source https://api.nuget.org/v3/index.json
