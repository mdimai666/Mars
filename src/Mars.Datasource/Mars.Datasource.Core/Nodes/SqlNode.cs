using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Nodes.Core;

namespace Mars.Datasource.Core.Nodes;

public class SqlNode : Node
{
    public string SqlQuery { get; set; } = "SELECT * \nFROM posts\nLIMIT 10;";

    public string DatasourceSlug { get; set; } = "default";

    public ESqlNodeInputSource Source { get; set; }
    public ESqlNodeOutputType OutputType { get; set; }

    public SqlNode()
    {
        haveInput = true;
        Color = "#faeaae";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/db-48.png";
    }

    public enum ESqlNodeInputSource
    {
        Static,
        Payload
    }

    public enum ESqlNodeOutputType
    {
        ArrayArrayString,
        //Object
    }
}
