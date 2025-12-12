using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpResponseNode/HttpResponseNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpResponseNode : Node
{
    public override string Label => base.Label + $":{ResponseStatusCode}";

    public int ResponseStatusCode { get; set; } = 200;

    public HttpResponseNode()
    {
        isInjectable = false;
        Color = "#e7e6af";
        Inputs = [new()];
        //Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/web-48.png";
    }

    public static IReadOnlyDictionary<int, string> StatusCodes = new Dictionary<int, string>()
    {
        [200] = "200 OK",
        [201] = "201 Created",
        [202] = "202 Accepted",
        [204] = "204 NoContent",
        [301] = "301 MovedPermanently",
        [302] = "302 Found",
        [304] = "304 NotModified",
        [307] = "307 TemporaryRedirect",
        [308] = "308 PermanentRedirect",
        [400] = "400 BadRequest",
        [401] = "401 Unauthorized",
        [402] = "402 PaymentRequired",
        [403] = "403 Forbidden",
        [404] = "404 NotFound",
        [405] = "405 MethodNotAllowed",
        [406] = "406 NotAcceptable",
        [409] = "409 Conflict",
        [413] = "413 RequestEntityTooLarge", // RFC 2616, renamed
        [413] = "413 PayloadTooLarge", // RFC 7231
        [418] = "418 ImATeapot",
        [500] = "500 InternalServerError",
        [501] = "501 NotImplemented",
        [502] = "502 BadGateway",
        [503] = "503 ServiceUnavailable",
    };
}
