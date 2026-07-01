namespace Mars.Nodes.Host.Services;

internal static class OldNodesNameMigratorHelper
{
    public static string ConvertOldFileToNew(string flowJson)
    {
        //Удалить через версию.

        //"Type": "Mars.Nodes.Core.Nodes.FlowNode",

        return flowJson
                    .Replace("\"Type\": \"Mars.", "\"TypeId\": \"Mars.")
                    .Replace("\"Type\": \"core.", "\"TypeId\": \"core.")
                    .Replace("\"type\": \"Mars.", "\"typeId\": \"Mars.")
                    .Replace("\"type\": \"core.", "\"typeId\": \"core.")
                    .Replace("Mars.Nodes.Core.Nodes.", "core.");

        string names = """
            Mars.Nodes.Core.Nodes.InjectNode -> core.InjectNode
            Mars.Nodes.Core.Nodes.DebugNode
            Mars.Nodes.Core.Nodes.CallNode
            Mars.Nodes.Core.Nodes.CallResponseNode
            Mars.Nodes.Core.Nodes.CommentNode
            Mars.Nodes.Core.Nodes.LinkInNode
            Mars.Nodes.Core.Nodes.LinkOutNode
            Mars.Nodes.Core.Nodes.FunctionNode
            Mars.Nodes.Core.Nodes.TemplateNode
            Mars.Nodes.Core.Nodes.DelayNode
            Mars.Nodes.Core.Nodes.EvalNode
            Mars.Nodes.Core.Nodes.ExecNode
            Mars.Nodes.Core.Nodes.ExecXActionNode
            Mars.Nodes.Core.Nodes.JoinNode
            Mars.Nodes.Core.Nodes.SplitNode
            Mars.Nodes.Core.Nodes.StringNode
            Mars.Nodes.Core.Nodes.SwitchNode
            Mars.Nodes.Core.Nodes.VariableSetNode
            Mars.Nodes.Core.Nodes.ActionCommandNode
            Mars.Nodes.Core.Nodes.CheckUserNode
            Mars.Nodes.Core.Nodes.DevAdminConnectionNode
            Mars.Nodes.Core.Nodes.CatchErrorNode
            Mars.Nodes.Core.Nodes.KillTaskJobNode
            Mars.Nodes.Core.Nodes.TerminateAllJobsNode
            Mars.Nodes.Core.Nodes.CounterNode
            Mars.Nodes.Core.Nodes.DevMicroschemeNode
            Mars.Nodes.Core.Nodes.DirReadNode
            Mars.Nodes.Core.Nodes.FileReadNode
            Mars.Nodes.Core.Nodes.FileServiceReadNode
            Mars.Nodes.Core.Nodes.FileServiceWriteNode
            Mars.Nodes.Core.Nodes.FileWriteNode
            Mars.Nodes.Core.Nodes.EmailSendNode
            Mars.Nodes.Core.Nodes.EndpointNode
            Mars.Nodes.Core.Nodes.HttpInFormSaveFilesNode
            Mars.Nodes.Core.Nodes.HttpInNode
            Mars.Nodes.Core.Nodes.HttpRequestNode
            Mars.Nodes.Core.Nodes.HttpResponseNode
            Mars.Nodes.Core.Nodes.MqttInNode
            Mars.Nodes.Core.Nodes.MqttOutNode
            Mars.Nodes.Core.Nodes.EventNode
            Mars.Nodes.Core.Nodes.LoggerNode
            Mars.Nodes.Core.Nodes.ForeachNode
            Mars.Nodes.Core.Nodes.QueueNode
            Mars.Nodes.Core.Nodes.HtmlParseNode
            Mars.Nodes.Core.Nodes.JsonNode
            Mars.Nodes.Core.Nodes.InlineFunctionNode
            Mars.Nodes.Core.Nodes.MicroschemeNode
            """;
    }
}
