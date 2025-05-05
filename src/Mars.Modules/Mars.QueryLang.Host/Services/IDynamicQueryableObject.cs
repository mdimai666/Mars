using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.QueryLang.Host.Services;

public interface IDynamicQueryableObject
{
    object? InvokeMethod(string methodName, string expr);
}
