using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TestModules;
using TestModules.Shared;
using Mars.Core.Extensions;

namespace TestModules.Pages.Constructor
{
    public partial class ConstructorEditor1
    {
        List<TBlock> Blocks = new();
        bool doDrag;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            AddTestBlock();
            AddTestBlock();
            AddTestBlock();
            AddTestBlock();
            AddTestBlock();
        }

        string[] colors = new string[] { "var(--bs-primary)", "var(--bs-success)", "var(--bs-danger)", "var(--bs-warning)", "var(--bs-secondary)", };
        void AddTestBlock()
        {
            TBlock newblock = new TBlock();
            int order = Blocks.Any() ? Blocks.Max(s => s.Order) : 0;
            newblock.Order = order + 1;
            newblock.Color = colors.TakeRandom();
            Blocks.Add(newblock);
        }

        void WorkspaceMouseDown(MouseEventArgs e)
        {
        }

        void WorkspaceMouseUp(MouseEventArgs e)
        {
            DeselectAll();
        }

        void WorkspaceMouseMove(MouseEventArgs e)
        {
            if (doDrag)
            {
                foreach (var b in dragElements)
                {
                    //b.block.x = (float)(e.ClientX + b.blockX - b.clickX);
                    //b.block.y = (float)(e.ClientY + b.blockY - b.clickY);

                    b.block.x = (float)(e.ClientX) - 50;
                    b.block.y = (float)(e.ClientY) - 25;

                    //if (!d.node.changed) d.node.changed = true;
                }
            }
        }

        void DeselectAll()
        {
            doDrag = false;
            foreach (var b in Blocks)
            {
                b.isDrag = false;
            }
        }

        void BlockClick(MouseEventArgs e, TBlock block)
        {
            //private Random random = new Random();
            //Color = String.Format("#{0:X6}", random.Next(0x1000000));
        }

        List<DragElement> dragElements = new();

        public void BlockMouseDown(MouseEventArgs e, TBlock block)
        {
            doDrag = true;
            block.isDrag = true;

            var drag = new DragElement
            {
                block = block,
                blockX = 0,
                blockY = 0,
                clickX = e.ClientX,
                clickY = e.ClientY,
            };

            dragElements.Add(drag);
        }

        public void BlockMouseUp(MouseEventArgs e, TBlock block)
        {
            //StopDrag();
            if (doDrag)
            {
                DeselectAll();
            }
        }

        class DragElement
        {
            public TBlock block = default!;
            public double clickX;
            public double clickY;
            public float blockX;
            public float blockY;
        }
    }
}
