# Mars.Plugin.PublicFilesScript

## How use?
Just add below rows in .csproj file

```xml
    <Target Name="RunPostPublishScript" AfterTargets="Publish" Condition="'$(Configuration)' == 'Release'">
        <Exec Command="dotnet $(NuGetPackageRoot)mdimai666.Mars.Plugin.PluginPublishScript\0.6.2-alpha.25\lib\net10.0\Mars.Plugin.PluginPublishScript.dll --run-postpublish --ProjectName=$(ProjectName) --out=$(PublishDir) --ProjectDir=$(ProjectDir)" />
    </Target>

    <Target Name="RunPostCompileDebugScript" AfterTargets="CoreBuild" Condition="'$(Configuration)' == 'Debug'">
        <Exec Command="dotnet $(NuGetPackageRoot)mdimai666.Mars.Plugin.PluginPublishScript\0.6.2-alpha.25\lib\net10.0\Mars.Plugin.PluginPublishScript.dll --run-postdebugcompile --ProjectName=$(ProjectName) --out=$(OutDir) --ProjectDir=$(ProjectDir)" />
    </Target>
```

## Developing
add the project directly and add

```xml
<Target Name="RunPostPublishScript" AfterTargets="Publish" Condition="'$(Configuration)' == 'Release'">
    <Exec Command="dotnet $(OutDir)Mars.Plugin.PluginPublishScript.dll --run-postpublish --ProjectName=$(ProjectName) --out=$(PublishDir) --ProjectDir=$(ProjectDir)" />
</Target>

<Target Name="RunPostCompileDebugScript" AfterTargets="CoreBuild" Condition="'$(Configuration)' == 'Debug'">
    <Exec Command="dotnet $(OutDir)Mars.Plugin.PluginPublishScript.dll --run-postdebugcompile --ProjectName=$(ProjectName) --out=$(OutDir) --ProjectDir=$(ProjectDir)" />
</Target>
```

## Костыль
Пока работает, но организовано не правильно. Потом переделать.
