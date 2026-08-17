using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Mars.PxBlocks.Runtime.Standard;

/// <summary>
/// Пауза в миллисекундах (аналог device_pause из PXT): Stop прерывает ожидание
/// сразу через токен запуска.
/// </summary>
internal sealed class StdLoopsPause : PxStatementImplement
{
    public StdLoopsPause() : base("core.loops.pause") { }

    public override Task ExecuteAsync(PxContext context, PxCall call)
    {
        var milliseconds = Math.Max(0, (int)call.Input("MS").ToNumber());
        return Task.Delay(milliseconds, context.CancellationToken);
    }
}
