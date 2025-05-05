using System.Diagnostics;

namespace Mars.Integration.Tests.Extensions;

public static class TaskExtensions
{
    [DebuggerStepThrough]
    public static void RunSync(this Task task)
    {
        task.ConfigureAwait(false).GetAwaiter().GetResult();
    }

    [DebuggerStepThrough]
    public static T RunSync<T>(this Task<T> task)
    {
        return task.ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
