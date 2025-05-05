using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Core.Extensions;

public static class ExpandoObjectHelper
{
    public static bool ContainsKey(this ExpandoObject obj, string propertyName)
    {
        return obj != null && ((IDictionary<String, object>)obj!).ContainsKey(propertyName);
    }
}
