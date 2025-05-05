using AppFront.Shared.Interfaces;
using Mars.Shared.Resources;

namespace AppFront.Main.Extensions;

public static class SmartSaveExtensions
{

    static IMessageService _messageService = default!;

    public static void Setup(this IMessageService messageService) => _messageService = messageService;

    /// <summary>
    /// Вызывает дейстиве и показывает toast success или error
    /// </summary>
    /// <returns></returns>
    public static async Task<bool> SmartActionExecutor(this Task task, string message)
    {
        try
        {
            await task;
            _ = _messageService.Success(message);
            return true;
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        return false;
    }

    public static Task<bool> SmartDelete(this Task task) => SmartActionExecutor(task, AppRes.DeletedSuccessfully);
    public static Task<bool> SmartSuccess(this Task task) => SmartActionExecutor(task, AppRes.CompletedSuccessfully);
    public static Task<bool> SmartSave(this Task task) => SmartActionExecutor(task, AppRes.SavedSuccessfully);

    public static async Task<UserActionResult> SmartActionResult(this Task<UserActionResult> task, string? message = null)
    {
        try
        {
            var result = await task;
            if (result.Ok)
            {
                _ = _messageService.Success(message ?? result.Message);
            }
            else
            {
                _ = _messageService.Warning(message ?? result.Message);
            }
            return result;
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
            return UserActionResult.Exception(ex);
        }
    }
}
