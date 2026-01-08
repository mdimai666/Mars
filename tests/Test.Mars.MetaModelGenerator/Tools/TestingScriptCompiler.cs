using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;
using Mars.Host.Shared.Interfaces;
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
                        typeof(global::Mars.Nodes.Core.Node),
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
