namespace Mars.Middlewares;

public class DebugObjectsLifetimeMiddleware
{
    private readonly RequestDelegate _next;

    public DebugObjectsLifetimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Отличный метод для отладки утечки. Надо чтобы HttpContext не жил дольше запроса.
        var weakRef = new WeakReference(httpContext);

        await _next(httpContext);

        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (weakRef.IsAlive)
            {
                Console.WriteLine("🔥 HttpContext STILL ALIVE!");
            }
            else
            {
                Console.WriteLine("☠️ HttpContext is dead!");
            }
        });
    }
}
