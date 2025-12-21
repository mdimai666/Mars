using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
//using MyShared.Models;

namespace MonacoRoslynCompletionProvider.Providers;

public class CompletionWorkspace
{
    public static MetadataReference[] DefaultMetadataReferences = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(int).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(DescriptionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DataSet).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(XmlDocument).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location),

            //MetadataReference.CreateFromFile(typeof(Node).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(IBlastDbContext).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(User).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(IPostService).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(EntityFrameworkQueryableExtensions).Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.Linq.JObject).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Text.Json.Nodes.JsonNode).Assembly.Location),
        };

    private Project _project;
    private AdhocWorkspace _workspace;
    private List<MetadataReference> _metadataReferences;

    public static CompletionWorkspace Create(params string[] assemblies) 
    {
        Console.WriteLine(">>Create");
        Assembly[] lst = new[] {
            Assembly.Load("Microsoft.CodeAnalysis.Workspaces"),
            Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces"),
            Assembly.Load("Microsoft.CodeAnalysis.Features"),
            Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")
        };

        var host = MefHostServices.Create(lst);
        var workspace = new AdhocWorkspace(host);

        var references = DefaultMetadataReferences.ToList();

        if (assemblies != null && assemblies.Length > 0)
        {
            for (int i = 0; i < assemblies.Length; i++)
            {
                references.Add(MetadataReference.CreateFromFile(assemblies[i]));
            }
        }

        var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), 
            VersionStamp.Create(), "TempProject", "TempProject", 
            LanguageNames.CSharp/*, hostObjectType: typeof(DimaShared)*/, hostObjectType: typeof(ScriptExecuteContext))
            .WithMetadataReferences(references);

        var project = workspace.AddProject(projectInfo);

        return new CompletionWorkspace() { _workspace = workspace, _project = project, _metadataReferences = references };
    }

    public async Task<CompletionDocument> CreateDocument(string code, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        outputKind = OutputKind.ConsoleApplication;

        var usings = new[] {
                "System",
                "System.Collections.Generic",
                "System.IO",
                "System.Linq",
                "System.Net.Http",
                "System.Threading",
                "System.Threading.Tasks",
            };

        //var _usings = string.Join("\n", usings.Select(s => $"using {s};"));
        //var _usings = string.Join("\n", usings.Select(s => $"global using global::{s};"));

        //var glDoc = _workspace.AddDocument(_project.Id, "GlobalUsings.cs", SourceText.From(_usings));
        //var glSt = await glDoc.GetSyntaxTreeAsync();
        //code = _usings + '\n' + code;

        var document = _workspace.AddDocument(_project.Id, "MyFile2.cs", SourceText.From(code))
            .WithSourceCodeKind(SourceCodeKind.Script)
            ;

        var st = await document.GetSyntaxTreeAsync();

        var options = new CSharpCompilationOptions(outputKind, usings: usings, scriptClassName: "Dima" )
            //.WithUsings(usings)
            .WithOutputKind(outputKind)
            .WithScriptClassName("Dima")
            ;//not work

        //var compilation =
        //    CSharpCompilation
        //        .Create("Temp",
        //            new[] { st },
        //            options: options,
        //            references: _metadataReferences
        //        );

        //var compilation = CSharpCompilation.CreateScriptCompilation("Temp", globalsType: typeof(Dima))
        var compilation = CSharpCompilation.Create("TempProject")
            //----------//.WithOptions(options)
            .AddSyntaxTrees(st)
            .WithReferences(_metadataReferences)
            ;

        using (var temp = new MemoryStream())
        {
            Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(temp);
            SemanticModel semanticModel = compilation.GetSemanticModel(st, true);
            
            return new CompletionDocument(document, semanticModel, result); 
        }            
    }
}

public class ScriptExecuteContext
{
    public int G { get; set; } = 123;
}
