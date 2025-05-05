Get-ChildItem .\ -include bin,obj -Recurse | ForEach-Object ($_) { echo Remove-Item $_.FullName;  Remove-Item $_.FullName -Force -Recurse }
