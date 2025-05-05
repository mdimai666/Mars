dotnet pack --configuration Release
$file = $(gci .\bin\Release\*.nupkg | sort CreationTime -Descending | Select-Object -First 1).Name
echo $file
$apikey = [System.Environment]::GetEnvironmentVariable('NUGET_MARS')
dotnet nuget push .\bin\Release\$file --api-key $apikey --source https://api.nuget.org/v3/index.json
