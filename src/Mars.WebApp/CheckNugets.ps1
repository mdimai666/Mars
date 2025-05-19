$dateStamp = Get-Date -UFormat "%Y-%m-%d_%H-%M-%S"
$log_filename = "logs/nuget-deploy-$dateStamp.local.log"

Start-Transcript -path $log_filename -append

$dirs =  @(
    # shared
    "Mars.Core",
    "Mars.Shared",
    "Mars.Options/Mars.Options",
    "AppFront.Shared",
    "AppFront.Main",
    # host
    "Mars.Host.Shared",
    "Mars.Host.Data",
    "Mars.Shared",
    # nodes
    "Mars.Nodes/Mars.Nodes.Core",
    "Mars.Nodes/Mars.Nodes.Core.Implements",
    "Mars.Nodes/Mars.Nodes.EditorApi",
    "Mars.Nodes/Mars.Nodes.FormEditor",
    # modules
    "Modules/MarsEditors",
    "Modules/MarsCodeEditor2",
    "Mars.WebApiClient",
    "Modules/BlazoredHtmlRender",
    # plugin
    "Plugin/Mars.Plugin.Abstractions",
    "Plugin/Mars.Plugin.Front",
    "Plugin/Mars.Plugin.Kit.Host",
    "Plugin/Mars.Plugin.Kit.Front",
    "Plugin/Mars.Plugin.PluginHost",
    "Plugin/Mars.Plugin.Front.Abstractions",
    "Plugin/Mars.Plugin.PluginPublishScript"
)

$MarsAppVersion = (Select-String -Path ..\..\Directory.Packages.props  -Pattern "<MarsAppVersion>(.+?)</MarsAppVersion>").Matches.Groups[1].Value

Write-Host "-------------------"
Write-Host "> Publish Nugets" -ForegroundColor Green
Write-Host 
echo "MarsAppVersion = $MarsAppVersion"
Write-Host "-------------------"

foreach ($d in $dirs) {

    $pname = ($d.split('/')[-1])
    $csproj = "$pname.csproj"
    # echo $csproj
    Write-Host "$csproj => " -NoNewline

    $csproj_file = "../$d/$csproj";

    $regPackageVersion = "<PackageVersion>(.+?)</PackageVersion>" 

    $version = (Select-String -Path $csproj_file  -Pattern $regPackageVersion).Matches.Groups[1].Value.Replace('$(MarsAppVersion)', $MarsAppVersion)
    # Write-Host "sdsd" -NoNewline
    Write-Host "$version" -ForegroundColor Blue

    # echo "$csproj => $version"

}

$ans = Read-Host "Do you publish all? [y]"

if($ans -ne "y") {
    echo "Exit";
    Stop-Transcript
    exit;
}

echo "Start publish..."

$__DIR__ = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.\')

# echo $__DIR__

foreach ($d in $dirs) {
    cd $__DIR__
    cd "../$d/"
    # $ps1 = (ls "Deploy.Nuget.ps1")[0].FullName
    Write-Host 
    Write-Host "==============================" -ForegroundColor Blue
    #Write-Host $ps1 -ForegroundColor Blue

    $pname = ($d.split('/')[-1])
    $csproj = "$pname.csproj"
    $csproj_file = "$csproj";
    $regPackageVersion = "<PackageVersion>(.+?)</PackageVersion>"
    $version = (Select-String -Path $csproj_file  -Pattern $regPackageVersion).Matches.Groups[1].Value.Replace('$(MarsAppVersion)', $MarsAppVersion)
    Write-Host "$d" -NoNewline -ForegroundColor Yellow
    Write-Host " $version" -ForegroundColor Green
    Write-Host "==============================" -ForegroundColor Blue
    # Invoke-Expression $ps1 | Out-Host - call Deploy.Nuget.ps1 script

    
    dotnet pack --configuration Release -o "C:\Users\D\Documents\VisualStudio\_LocalNugets" --include-source


    cd $__DIR__
}

cd $__DIR__

echo "----------------";
echo "END";
Stop-Transcript
