using System.Text.RegularExpressions;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes.Parsers;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements.Nodes.Functions;

public class FunctionNodeImpl : INodeImplement<FunctionNode>
{

    public FunctionNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public FunctionNodeImpl(FunctionNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        try
        {
            string script = Node.Code;
            //https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md
            //var result = CSharpScript.EvaluateAsync<int>(script, ScriptOptions.Default, input).Result;

            var definedAssemblies = new[] {
                    typeof(DynamicNodeMsgWrapper).Assembly,
                    typeof(UserDetail).Assembly,
                    typeof(IPostService).Assembly,
                    typeof(EntityFrameworkQueryableExtensions).Assembly,
                    typeof(ServiceProviderServiceExtensions).Assembly
            };

            var sc = RNS.ServiceProvider.GetRequiredService<IServiceCollection>();
            //var assemblies = sc.Select(s => s.ServiceType.Assembly).Concat(definedAssemblies).Distinct().ToArray();

            var re = new Regex("RNS\\.GetService<(.*?)>");

            var detectAssemblied = re.Matches(script).Select(s => s.Groups[1].Value).ToList();

            var comparedAssemblies = sc
                .Where(s => detectAssemblied.Contains(s.ServiceType.Name))
                .Select(s => s.ServiceType.Assembly)
                .Concat(definedAssemblies)
                .Distinct()
                .ToArray();

            ScriptOptions scriptOptions = ScriptOptions.Default
                .WithLanguageVersion(LanguageVersion.Latest)
                .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Threading.Tasks",
                "System.Threading",
                "Mars.Nodes.Core",
                typeof(Node).Namespace!,
                "Microsoft.Extensions.DependencyInjection"
                )
                .WithReferences(
                    comparedAssemblies
                );

            var compiled = CSharpScript.Create(script, scriptOptions, typeof(ScriptExecuteContext)).CreateDelegate();

            var ctx = new ScriptExecuteContext
            {
                NodeId = Node.Id,
                msg = new DynamicNodeMsgWrapper(input),
                RNS = RNS,
                callback = callback
            };

            var result = await compiled.Invoke(ctx, parameters.CancellationToken);

            if (result is DynamicNodeMsgWrapper dyn)
                callback(dyn.ToNodeMsg());
            else if (result is NodeMsg msg)
                callback(msg);
            else
                callback(input.SetPayload(result));

        }

        catch (CompilationErrorException)
        {
            RNS.Status(NodeStatus.Error("compile error"));
            //RNS.DebugMsg(ex);
            throw;
        }
        catch (Exception)
        {
            RNS.Status(NodeStatus.Error("error"));
            //RNS.DebugMsg(ex);
            throw;
        }
    }

    public class ScriptExecuteContext
    {
        public string NodeId = "";
        public dynamic msg = default!;
        public IRuntimeNodeScope RNS = default!;
        public ExecuteAction callback = default!;
        public IFlowNodeImpl Flow => RNS.Flow!;
        public VariablesContextDictionary GlobalContext => RNS.GlobalContext;

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

        public void Debug(object? obj)
            => RNS.DebugMsg(new DebugMessage
            {
                NodeId = NodeId,
                Message = $"Function Debug: ({obj?.GetType().Name})",
                Json = JsonNodeImpl.ToJsonString(obj!, formatted: true)
            });
    }
}
