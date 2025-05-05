using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;
using NodeWorkspace;
using Mars.Nodes.Core;

namespace NodeWorkspace
{
    public partial class MicroschemeComponent
    {
        [Parameter] public Node node { get; set; } = default!;

        [Parameter] public float x { get; set; }
        [Parameter] public float y { get; set; }

        float X => node.X + 10;
        float Y => node.Y + 8;

        public float bodyRectHeight => node.Outputs.Count < 2 ? 30 : node.Outputs.Count * 16f;

        [Parameter] public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnMouseUp { get; set; }

        [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNew { get; set; }
        [Parameter] public EventCallback<NodeWirePointEventArgs> wireStartNewEnd { get; set; }

        [Parameter] public EventCallback<string> OnInject { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnDblClick { get; set; }



        void OnMouseDownMethod(MouseEventArgs e)
        {
            OnMouseDown.InvokeAsync(e);
        }
        void OnMouseUpMethod(MouseEventArgs e)
        {
            //OnInputWirePointUp(e);
            //OnOutputWirePointUp(e, 0);
            OnMouseUp.InvokeAsync(e);
        }

        void OnInjectClick(MouseEventArgs e)
        {
            OnInject.InvokeAsync(this.node.Id);
        }



        // Simple events ============================
        void OnClickEvent(MouseEventArgs e)
        {
            OnClick.InvokeAsync(e);
        }
        void OnDblClickEvent(MouseEventArgs e)
        {
            OnDblClick.InvokeAsync(e);
        }
        // Wires ============================

        void OnInputWirePointDown(MouseEventArgs e)
        {
            wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, node));
        }

        void OnInputWirePointUp(MouseEventArgs e)
        {
            wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, 0, true, node));
        }

        void OnOutputWirePointDown(MouseEventArgs e, int index)
        {
            wireStartNew.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
        }

        void OnOutputWirePointUp(MouseEventArgs e, int index)
        {
            wireStartNewEnd.InvokeAsync(new NodeWirePointEventArgs(e, index, false, node));
        }
    }
}