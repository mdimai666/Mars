using System;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mars.Core.Features;
using Mars.Host.Data;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.ActionHistorys;

namespace Mars.Host.Services;

internal class ActionHistoryService : IActionHistoryService
{
    /*public ActionHistoryService(IConfiguration configuration, IServiceProvider serviceProvider) //: base(configuration, serviceProvider)
    {

    }

    public async Task Add(Exception exception, string title)
    {
        ActionHistory add = new ActionHistory
        {
            Title = title,
            ActionLevel = ActionHistoryLevel.Error,
            Content = exception.StackTrace,
            ActionType = EActionType.Error,
        };

        throw new NotImplementedException();
        //await Add(add);
    }

    public async Task Add<T>(T data, string title, ActionHistoryLevel level, EActionType type, string message = null)
        where T : class
    {
        throw new NotImplementedException();

        //string content = string.Empty;

        //try
        //{
        //    if (content is not null)
        //    {
        //        content = JsonConvert.SerializeObject(data, QServer.DefaultConvertSetting());
        //    }
        //}
        //catch (Exception ex)
        //{
        //    content += data?.ToString() + "\n\n" + ex.Message;
        //}

        //ActionHistory add = new ActionHistory
        //{
        //    ActionLevel = level,
        //    Title = title,
        //    Message = message ?? data?.ToString(),
        //    Content = content,
        //    ActionModel = typeof(T).Name,
        //    UserId = MarsDbContextLegacy.DefaultUserIdForCreateItem,
        //    ActionType = type,
        //};

        //await Add(add);
    }
    */
    public Task Add(Exception exception, string title)
    {
        throw new NotImplementedException();
    }

    public Task Add<T>(T data, string title, ActionHistoryLevel level, string actionType, string? message = null) where T : class
    {
        throw new NotImplementedException();
    }
}

