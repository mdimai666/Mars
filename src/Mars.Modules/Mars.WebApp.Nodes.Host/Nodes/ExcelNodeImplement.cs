using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.JsonConverters;
using Mars.Nodes.Host.Shared;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class ExcelNodeImplement : INodeImplement<ExcelNode>
{
    public ExcelNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public ExcelNodeImplement(ExcelNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (input.Payload is null)
        {
            //RNS.DebugMsg(new DebugMessage { Level = Mars.Core.Models.MessageIntent.Warning, message = "payload is null" });
            throw new ArgumentException("Payload is null");
        }
        else if (input.Payload.GetType().IsPrimitive || input.Payload is string)
        {
            throw new ArgumentException("Payload must be object");
        }

        ArgumentException.ThrowIfNullOrEmpty(Node.TemplateFile, nameof(Node.TemplateFile));

        var excelService = RNS.ServiceProvider.GetRequiredService<IExcelService>();
        //var storageService = RNS.ServiceProvider.GetRequiredService<IStorageService>();
        var hostingInfo = RNS.ServiceProvider.GetRequiredService<IOptions<FileHostingInfo>>().Value;

        var templateFullPath = Node.TemplateFile;

        if (templateFullPath.StartsWith('/'))
        {
        }
        else if (Directory.GetLogicalDrives().Any(s => templateFullPath.StartsWith(s)))
        {
        }
        else
        {
            templateFullPath = hostingInfo.FileAbsolutePath(templateFullPath);
        }

        if (!File.Exists(templateFullPath)) throw new ArgumentException($"TemplateFile not exist: '{templateFullPath}'");

        using var ms = new MemoryStream();

        object payload = input.Payload;

        if (payload is JsonNode jsonNode)
        {
            dynamic obj = JsonSerializer.Deserialize<dynamic>(jsonNode, _jsonSerializerOptions)!;
            payload = obj;
        }
        else if (payload is ExpandoObject expandoObject)
        {
            var expandoJsonNode = JsonSerializer.SerializeToNode(expandoObject);
            dynamic obj = JsonSerializer.Deserialize<dynamic>(expandoJsonNode, _jsonSerializerOptions)!;
            payload = obj;
        }

        excelService.BuildExcelReport(templateFullPath, payload as dynamic, ms);

        var content = ms.ToArray();

        input.Payload = content;

        callback(input);

        return Task.CompletedTask;
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new ObjectAsPrimitiveConverter(
                                    floatFormat: FloatFormat.Double,
                                    unknownNumberFormat: UnknownNumberFormat.Error,
                                    objectFormat: ObjectFormat.Dictionary) },
        WriteIndented = true,
    };
}
