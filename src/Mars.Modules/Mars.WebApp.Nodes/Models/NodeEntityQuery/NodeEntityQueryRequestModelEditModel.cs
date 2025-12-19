using Mars.Core.Extensions;

namespace Mars.WebApp.Nodes.Models.NodeEntityQuery;

public class NodeEntityQueryRequestModel
{
    public string EntityName { get; set; } = "";
    public NEQRM_MethodCall[] CallChains { get; set; } = [];

    public string BuildString() => EntityName + "." + CallChains.Where(s => !s.Disabled).Select(x => x.ToMethodIntelliView()).JoinStr(".");
}

public class NEQRM_MethodCall
{
    public string MethodName { get; set; } = "";
    public List<string> Parameters { get; set; } = [];
    public bool Disabled { get; set; }

    public string ToMethodIntelliView() => $"{MethodName}({Parameters.JoinStr(", ")})";
}
