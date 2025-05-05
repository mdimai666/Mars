using System.Text;
using AppShared.Models;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;

namespace Mars.GenSourceCode;

public class RuntimeTypeCompiler : IRuntimeTypeCompiler
{
    public Dictionary<string, Type> Compile(List<PostTypeEntity> postTypeList, List<MetaFieldEntity> userMetaFields, IMetaModelTypesLocator mlocator, string setNamespace = "AppFront.Host.Data")
    {
        string code = GenClassMto.GenPostTypesAsMtoString(postTypeList, userMetaFields, mlocator, addNamespace: false);

        StringBuilder sb = new StringBuilder(code);

        //will like as
        string script = @"
        public class MyClass {
            public string Name {get;set;} = ""Aa"";
        }

        return new Dictionary<string,Type> { [""MyClass""] = typeof(MyClass) };
        ";

        sb.AppendLine();
        sb.AppendLine("return new Dictionary<string,Type> {");

        foreach (var postType in postTypeList)
        {
            MtoClassInfoPost mto = new MtoClassInfoPost(postType, mlocator);
            DtoClassInfoPost dto = new DtoClassInfoPost(postType, mlocator);

            sb.AppendLine($"\t[\"{postType.TypeName}\"] = typeof({mto.postTypeClassName}),");
            sb.AppendLine($"\t[\"{postType.TypeName}_dto\"] = typeof({dto.postTypeClassName}),");
        }

        sb.AppendLine("};");

        script = sb.ToString();

        ScriptOptions scriptOptions = ScriptOptions.Default
                .WithImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text",
                    "System.Threading.Tasks",
                    "Mars.Nodes.Core",
                    "Mars.Shared.Models",
                    //"Mars.Shared.Services",
                    "AppShared.Models",
                    "Microsoft.EntityFrameworkCore",
                    "System.Linq.Expressions",
                    "Mars.GenSourceCode.Interfaces",
                    "Mars.Shared.Templators"
                //"Newtonsoft.Json",
                //"Newtonsoft.Json.Linq"
                )
                .WithReferences(
                    //typeof(Node).Assembly,
                    typeof(MarsDbContext).Assembly,
                    //typeof(User).Assembly,
                    //typeof(IPostService).Assembly,
                    typeof(EntityFrameworkQueryableExtensions).Assembly,
                    typeof(System.Linq.Expressions.Expression).Assembly,
                    typeof(IMtoMarker).Assembly,
                    typeof(MetaRelationObjectDict).Assembly
                //typeof(JObject).Assembly
                );


        var compiled = CSharpScript.Create(script, scriptOptions, typeof(CompileContext)).CreateDelegate();

        var ctx = new CompileContext
        {
            //msg = input,
            //RED = RED,
        };

        var result = compiled.Invoke(ctx).Result;

        return (result as Dictionary<string, Type>)!;

    }

    public class CompileContext
    {

    }
}
