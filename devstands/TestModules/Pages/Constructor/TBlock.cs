using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestModules.Pages.Constructor;

public class TBlock
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Color { get; set; } = default!;

    public bool isDrag;
    public double x;
    public double y;
    public bool isSelect;

}
