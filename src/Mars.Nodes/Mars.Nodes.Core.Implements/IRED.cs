using Mars.Host.Shared.Models;
using Mars.Nodes.Core.Implements.Models;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Core.Implements;

public interface IRED
{
    /*void Init(int httpServer, int userSettings);
    void Start();
    void Stop();*/

    ILogger<IRED> Logger { get; }
    /*int units { get; set; }
    int events { get; set; }
    int hooks { get; set; }
    int settings { get; set; }
    int version { get; set; }
    int httpAdmin { get; set; }
    int httpNode { get; set; }
    int server { get; set; }
    int runtime { get; set; }
    int auth { get; set; }*/

    FlowNodeImpl Flow { get; }

    void Status(NodeStatus nodeStatus);
    void DebugMsg(DebugMessage msg);
    void DebugMsg(Exception ex);

    void RegisterHttpMiddleware(HttpCatchRegister mw);

    List<HttpCatchRegister> HttpRegisterdCatchers { get; }
    IEnumerable<HttpCatchRegister> GetHttpCatchRegistersForMethod(string method);


    public HttpClient GetHttpClient();

    public IServiceProvider ServiceProvider { get; }
    public VariablesContextDictionary GlobalContext { get; }
    public VariablesContextDictionary FlowContext { get; }

    public VarNodeVaribleDto? GetVarNodeVarible(string varName);
    public void SetVarNodeVarible(string varName, object? value);

    public IReadOnlyDictionary<string, VarNode> VarNodesDict { get; }

    public IReadOnlyDictionary<string, ConfigNode> ConfigNodesDict { get; }

    public InputConfig<TConfigNode> GetConfig<TConfigNode>(string id) where TConfigNode : ConfigNode;
    public InputConfig<TConfigNode> GetConfig<TConfigNode>(InputConfig<TConfigNode> config) where TConfigNode : ConfigNode;

    public static string ToJsonString(object input, bool formatted = false)
    {
        return JsonNodeImpl.ToJsonString(input, formatted);
    }

    public static T? ParseString<T>(string input)
    {
        return JsonNodeImpl.ParseString<T>(input);
    }

    public static System.Text.Json.Nodes.JsonNode? ParseString(string input)
    {
        return JsonNodeImpl.ParseString(input);
    }
}



public interface IRedUtils
{
    int log { get; set; }
    int i18n { get; set; }
    int util { get; set; }
    int hook { get; set; }
}

public interface IRedRuntime
{
    /*
    comms: CommsModule;
    flows: FlowsModule;
    library: LibraryModule;
    nodes: NodesModule;
    settings: SettingsModule;
    projects: ProjectsModule;
    context: ContextModule;
    hooks: Hooks;

    isStarted: () => Promise<boolean>;
    version: () => Promise<string>;

    storage: StorageModule;
    events: EventEmitter;
    util: Util;
    */
}
