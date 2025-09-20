using System.Text.RegularExpressions;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Nodes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes;

public class FunctionNodeImpl : INodeImplement<FunctionNode>, INodeImplement
{

    public FunctionNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public FunctionNodeImpl(FunctionNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {

        try
        {
            //IPostService ps = RED.GetService<IPostService>();
            ////IFileService fs = RED.GetService<IFileService>();
            //IMarsDbContext ef = RED.GetService<IMarsDbContext>();
            //var p1 = ef.Posts.First();
            //RED.DebugMsg(new DebugMessage() { message = p1.Title });

            string script = Node.Code;
            //https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
            //var result = CSharpScript.EvaluateAsync<int>(script, ScriptOptions.Default, input).Result;

            var definedAssemblies = new[] {

                    typeof(Node).Assembly,
                    //typeof(IMarsDbContext).Assembly,
                    typeof(UserDetail).Assembly,
                    typeof(IPostService).Assembly,
                    typeof(EntityFrameworkQueryableExtensions).Assembly,
                    typeof(ServiceProviderServiceExtensions).Assembly
            };

            var sc = RED.ServiceProvider.GetRequiredService< IServiceCollection>();
            //var assemblies = sc.Select(s => s.ServiceType.Assembly).Concat(definedAssemblies).Distinct().ToArray();

            Regex re = new Regex("RED\\.GetService<(.*?)>");

            var detectAssemblied = re.Matches(script).Select(s => s.Groups[1].Value).ToList();

            var comparedAssemblies = sc
                .Where(s => detectAssemblied.Contains(s.ServiceType.Name))
                .Select(s => s.ServiceType.Assembly)
                .Concat(definedAssemblies)
                .Distinct()
                .ToArray();

            ScriptOptions scriptOptions = ScriptOptions.Default
                .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks",
                "Mars.Nodes.Core",
                typeof(Mars.Nodes.Core.Node).Namespace,
                "Microsoft.Extensions.DependencyInjection"
                //"Mars.Shared.Models",
                //"Mars.Shared.Services",
                //"AppShared.Models",
                //"Microsoft.EntityFrameworkCore",
                )
                .WithReferences(
                    comparedAssemblies
                );

            var compiled = CSharpScript.Create(script, scriptOptions, typeof(ScriptExecuteContext)).CreateDelegate();

            var ctx = new ScriptExecuteContext
            {
                msg = input,
                RED = RED,
                callback = callback
            };

            using var result = compiled.Invoke(ctx);

            if (result.IsCompleted)
            {
                input.Payload = result.Result;
                if (input.Payload != null)
                {
                    callback(input);
                }
            }

        }

        catch (CompilationErrorException ex)
        {
            //Error(ex);
            RED.Status(NodeStatus.Error("compile error"));
            RED.DebugMsg(ex);
        }
        catch (Exception ex)
        {
            //Error(ex);
            RED.Status(NodeStatus.Error("error"));
            RED.DebugMsg(ex);
        }

        return Task.CompletedTask;
    }

    public class ScriptExecuteContext
    {
        public NodeMsg msg = default!;
        public IRED RED = default!;
        public ExecuteAction callback = default!;
        public FlowNodeImpl Flow => RED.Flow;
        public VariablesContextDictionary GlobalContext => RED.GlobalContext;

        public void Send(object msgOrPayload, int output = 0)
        {
            if (msgOrPayload is NodeMsg nodeMsg)
            {
                callback(nodeMsg, output);
            }
            else
            {
                callback(new NodeMsg { Payload = msgOrPayload }, output);
            }
        }
    }
}
