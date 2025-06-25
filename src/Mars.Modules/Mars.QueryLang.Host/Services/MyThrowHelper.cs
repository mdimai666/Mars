using System.Diagnostics;

namespace Mars.QueryLang.Host.Services;

public static class MyThrowHelper
{
    [DebuggerStepThrough]
    public static void IfArgumentCount(object[] args, int count, string error = "")
    {
        if (args.Count() != count)
            throw new ArgumentException($"Argument count must be {count} given {args.Count()}; " + error);
    }

    [DebuggerStepThrough]
    public static void IfArgumentCount(object[] args, int min, int max, string error = "")
    {
        int _count = args.Count();
        if (_count < min || _count > max)
            throw new ArgumentException($"Argument count must be between {min}-{max} given {_count}; " + error);
    }
}
