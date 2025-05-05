using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components.Web;

namespace NodeWorkspace
{
    public class NodeWirePointEventArgs
    {
        public MouseEventArgs e;
        public int pinIndex;
        public bool isInput;
        public Node node;

        public NodeWirePointEventArgs(MouseEventArgs e, int pinIndex, bool isInput, Node node)
        {
            this.e = e;
            this.pinIndex = pinIndex;
            this.isInput = isInput;
            this.node = node;
        }
    }
}