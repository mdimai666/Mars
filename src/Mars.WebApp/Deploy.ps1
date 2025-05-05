$dateStamp = Get-Date -UFormat "%Y-%m-%d_%H-%M-%S"

$host_destination = "/var/www/non/host"
$host_name = "sber1"

$Host.PrivateData.ErrorBackgroundColor = "Red"
$Host.PrivateData.ErrorForegroundColor = "White"
echo '>>Deploy'


echo "(1/5) - Build"
#dotnet publish --configuration Release -r linux-x64
dotnet publish --configuration Release

$dir0 = $pwd
cd $pwd\bin\Release
ls

#not require
#rmdir -Force -Recurse  $(Join-Path "$pwd/net8.0/publish" "wwwroot/upload")

$zip = "$pwd/publish.zip"
echo "(2/5) - Zip"
Compress-Archive -Path "$pwd/net8.0/publish" -DestinationPath "$zip" -CompressionLevel Optimal -Force
echo "(3/5) - Copy to server"
scp -C $zip sber1:$host_destination
echo "(4/5) - Unzip | copy"
# С аргументами -zlo не работает замена файлов
ssh $host_name "cd $host_destination;ls *.zip;unzip -oq publish.zip"
echo "(5/5) - Restart App"
ssh $host_name "pm2 restart non"

cd $dir0
echo '<<Finish'
# pause
