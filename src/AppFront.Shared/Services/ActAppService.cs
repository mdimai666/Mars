using AppFront.Shared.Interfaces;
using Mars.Shared.Contracts.XActions;
using Mars.Shared.Interfaces;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Services;

internal class ActAppService : IActAppService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly IMessageService _messageService;
    protected readonly ViewModelService _viewModelService;
    protected readonly NavigationManager _navigationManager;
    protected readonly IXActionFormPresenter _formPresenter;

    public ActAppService(IMarsWebApiClient client, IMessageService messageService, ViewModelService viewModelService,
        NavigationManager navigationManager, IXActionFormPresenter formPresenter)
    {
        _client = client;
        _messageService = messageService;
        _viewModelService = viewModelService;
        _navigationManager = navigationManager;
        _formPresenter = formPresenter;
    }

    public async Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string>? args = null)
    {
        var actions = Q.Site.XActions;

        if (actions.TryGetValue(id, out var act))
        {

            if (act.Type == XActionType.Link)
            {
                //_ = messageService.Info($"action '{id}' click");
                _navigationManager.NavigateTo(act.LinkValue!);
                return XActResult.ToastSuccess(act.LinkValue ?? "");
            }
            else if (act.Type == XActionType.HostAction)
            {
                // аргументы не переданы, но у команды есть схема — показать форму
                // (кастомную из реестра или генерик по схеме); вызов с аргументами идёт сразу
                if (args == null && act.Arguments is { Length: > 0 })
                {
                    var formValues = await _formPresenter.ShowFormAsync(act);
                    if (formValues == null)
                    {
                        Console.WriteLine($"ActService: command '{id}' cancelled (form)");
                        return null!;
                    }
                    args = formValues;
                }

                try
                {
                    var res = await _client.Act.Inject(id, args);

                    if (!string.IsNullOrEmpty(res.Message))
                    {
                        _ = _messageService.Show(res.Message, res.MessageIntent);
                    }

                    // эффекты — «отрисовка результата»: navigate и события применяем,
                    // NextAction/Custom возвращаем вызывающему и автоматически не выполняем
                    foreach (var effect in res.Effects)
                    {
                        switch (effect)
                        {
                            case NavigateEffect navigate:
                                _navigationManager.NavigateTo(navigate.Url);
                                break;
                            case TriggerEventEffect triggerEvent:
                                Q.Root.Emit(triggerEvent.Name, triggerEvent.Payload);
                                break;
                        }
                    }

                    return res;
                }
                catch (Exception ex)
                {
                    _ = _messageService.Error(ex.Message);
                    return null!;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            Console.WriteLine($"ActService: action '{id}' not found");
            return null!;
        }
    }
}
