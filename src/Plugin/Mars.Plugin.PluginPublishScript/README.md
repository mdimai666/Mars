# Mars.Plugin.PublicFilesScript

## How use?
Just add below rows in .csproj file


```xml
<Target Name="RunPostPublishScript" AfterTargets="Publish" Condition="'$(Configuration)' == 'Release'">
    <Exec Command="dotnet $(OutDir)Mars.Plugin.PluginPublishScript.dll --run-postpublish --ProjectName=$(ProjectName) --out=$(PublishDir) --ProjectDir=$(ProjectDir)" />
</Target>

<Target Name="RunPostCompileDebugScript" AfterTargets="CoreBuild" Condition="'$(Configuration)' == 'Debug'">
    <Exec Command="dotnet $(OutDir)Mars.Plugin.PluginPublishScript.dll --run-postdebugcompile --ProjectName=$(ProjectName) --out=$(OutDir) --ProjectDir=$(ProjectDir)" />
</Target>
```
