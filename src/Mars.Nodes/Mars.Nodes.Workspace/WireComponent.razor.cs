using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mars.Nodes.Workspace
{
    public partial class WireComponent
    {
        [Parameter]
        public float y2 { get; set; }
        [Parameter]
        public float x1 { get; set; }
        [Parameter]
        public float y1 { get; set; }
        [Parameter]
        public float x2 { get; set; }


        public float X1 => x1 + 10;
        public float Y1 => y1 + 8;

        public string Path => $"M {x1} {y1} C {x1 + 75} {y1} {x2 - 75} {y2} {x2} {y2}".Replace(',', '.');
        [Parameter]
        public bool selected { get; set; }
        [Parameter]
        public bool disable { get; set; }


        [Parameter]
        public EventCallback<MouseEventArgs> OnMouseDown { get; set; }
        [Parameter]
        public EventCallback<MouseEventArgs> OnMouseUp { get; set; }


        void OnMouseDownMethod(MouseEventArgs e)
        {
            OnMouseDown.InvokeAsync(e);
        }
        void OnMouseUpMethod(MouseEventArgs e)
        {
            OnMouseUp.InvokeAsync(e);
        }
    }
}