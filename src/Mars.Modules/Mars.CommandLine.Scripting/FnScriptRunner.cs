using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Mars.CommandLine.Scripting;

/// <summary>
/// Компиляция и одноразовое исполнение C#-скрипта (top-level statements) через Roslyn CSharpScript.
/// Ссылки — полный TPA процесса (одноразовый запуск, надёжность важнее скорости компиляции);
/// MetadataReference создаются из путей БЕЗ загрузки сборок в процесс.
/// </summary>
public static class FnScriptRunner
{
    // BCL-база как в FunctionNode/CodeCompletion; остальное — явными using в коде скрипта
    private static readonly string[] BclImports =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Threading.Tasks",
        "System.Threading",
    ];

    private static readonly Lazy<IReadOnlyList<MetadataReference>> PlatformReferences = new(() =>
    {
        var references = new List<MetadataReference>();
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string tpa || tpa.Length == 0)
            return references;

        foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (BadImageFormatException)
            {
                // нативная/повреждённая сборка — пропускаем
            }
        }

        return references;
    });

    public static Task<ScriptState<object>> RunAsync(string code, FnContext globals, CancellationToken ct)
    {
        var options = ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .WithImports(BclImports)
            .WithReferences(PlatformReferences.Value);

        // без catchException-предиката RunAsync перебрасывает исключения скрипта вместо
        // ScriptState.Exception; отмену НЕ перехватываем — Ctrl+C должен прерывать команду.
        // Предикат есть только у Script<T>.RunAsync (у статического CSharpScript.RunAsync — нет).
        return CSharpScript
            .Create(code, options, typeof(FnContext))
            .RunAsync(globals, static ex => ex is not OperationCanceledException, ct);
    }
}
