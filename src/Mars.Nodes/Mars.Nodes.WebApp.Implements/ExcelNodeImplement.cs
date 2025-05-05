using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.JsonConverters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.Nodes.WebApp.Implements;

public class ExcelNodeImplement : INodeImplement<ExcelNode>, INodeImplement
{
    public ExcelNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public ExcelNodeImplement(ExcelNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        if (input.Payload is null)
        {
            //RED.DebugMsg(new DebugMessage { Level = Mars.Core.Models.MessageIntent.Warning, message = "payload is null" });
            throw new ArgumentException("Payload is null");
        }
        else if (input.Payload.GetType().IsPrimitive || input.Payload is string)
        {
            throw new ArgumentException("Payload must be object");
        }

        ArgumentException.ThrowIfNullOrEmpty(Node.TemplateFile, nameof(Node.TemplateFile));

        var excelService = RED.ServiceProvider.GetRequiredService<IExcelService>();
        //var storageService = RED.ServiceProvider.GetRequiredService<IStorageService>();
        var hostingInfo = RED.ServiceProvider.GetRequiredService<IOptions<FileHostingInfo>>().Value;

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

    readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        Converters = { new ObjectAsPrimitiveConverter(
                                    floatFormat: FloatFormat.Double,
                                    unknownNumberFormat: UnknownNumberFormat.Error,
                                    objectFormat: ObjectFormat.Dictionary) },
        WriteIndented = true,
    };
}
