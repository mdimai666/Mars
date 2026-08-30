using System.Dynamic;

namespace Mars.Core.Extensions;

public static class ExpandoObjectHelper
{
    public static bool ContainsKey(this ExpandoObject obj, string propertyName)
    {
        return obj != null && ((IDictionary<String, object>)obj!).ContainsKey(propertyName);
    }
}
