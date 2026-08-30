using System.ComponentModel.DataAnnotations;
using Mars.Cms.Abstractions.Interfaces;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Data.OwnedTypes.MetaFields;
using Mars.Integration.Tests.Extensions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;

namespace Test.Mars.MetaModelGenerator.Tools;

internal static class TestingScriptCompiler
{
    public static readonly ScriptOptions scriptOptions = ScriptOptions.Default
                .WithImports(
                    ["System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text",
                    "System.Threading.Tasks",
                    "Microsoft.EntityFrameworkCore",
                    "System.Linq.Expressions",
                    ..((Type[])[
                        typeof(PostEntity),
                        typeof(DisplayAttribute),
                        typeof(IMtoMarker),
                        typeof(MetaFieldVariant),
                    ]).Select(t=>t.Namespace!).Distinct()
                    ]
                )
                .WithReferences(
                    typeof(EntityFrameworkQueryableExtensions).Assembly,
                    typeof(System.Linq.Expressions.Expression).Assembly,
                    typeof(MarsDbContext).Assembly,
                    typeof(PostEntity).Assembly,
                    typeof(DisplayAttribute).Assembly,
                    typeof(IMtoMarker).Assembly,
                    typeof(MetaFieldVariant).Assembly
                );

    public static Type Compile(string code, string className)
    {
        var script = $$"""
            {{code}}

            return typeof({{className}});
            """;

        var compiled = CSharpScript.Create<Type>(script, scriptOptions).CreateDelegate();

        var result = compiled.Invoke().RunSync();

        return result!;
    }

}
