using Mars.Shared.Contracts.XActions;

namespace Mars.XActions;

#if DEBUG
/// <summary>
/// Отладочная команда для проверки генерик-формы XActions:
/// строка, число, bool и выбор из списка; по выполнению показывает тостом введённые значения.
/// </summary>
public class FormTestAct : IAct
{
    public const string CommandId = "mars.debug.testForm";

    public const string TextArg = "text";
    public const string NumberArg = "number";
    public const string BoolArg = "flag";
    public const string ChoiceArg = "choice";

    public Task<XActResult> Execute(IActContext context, CancellationToken cancellationToken)
    {
        var message =
            $"строка: '{context.Get(TextArg)}', " +
            $"число: '{context.Get(NumberArg)}', " +
            $"bool: '{context.Get(BoolArg)}', " +
            $"выбор: '{context.Get(ChoiceArg)}'";

        return Task.FromResult(XActResult.ToastSuccess(message));
    }
}
#endif
