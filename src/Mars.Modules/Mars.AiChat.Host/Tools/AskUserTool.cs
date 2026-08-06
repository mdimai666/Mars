using System.ComponentModel;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструмент агента: уточняющий вопрос пользователю.
/// После вызова агент должен остановиться и ждать ответа следующим сообщением.
/// </summary>
public class AskUserTool
{
    public const string FunctionName = nameof(AskUser);

    public string? LastQuestion { get; private set; }

    [Description("Задать уточняющий вопрос пользователю, если для выполнения задачи не хватает информации. " +
                 "После вызова этого инструмента остановись: не вызывай другие инструменты и не продолжай работу — " +
                 "ответ пользователя придёт следующим сообщением.")]
    public string AskUser(
        [Description("Текст вопроса пользователю. Формулируй конкретно, предлагай варианты ответа, если возможно.")] string question)
    {
        LastQuestion = question;
        return "Вопрос отправлен пользователю. Остановись и дождись его ответа следующим сообщением. Больше не вызывай инструменты в этом ходу.";
    }
}
