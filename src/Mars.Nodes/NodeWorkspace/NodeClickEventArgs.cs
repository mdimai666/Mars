using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeWorkspace;

public class NodeClickEventArgs
{
    public MouseEventArgs MouseEvent = default!;
    public Node Node = default!;
}
