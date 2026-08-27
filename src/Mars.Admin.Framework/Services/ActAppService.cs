using Mars.Admin.Framework.Interfaces;
using Mars.Contracts.XActions;
using Mars.Contracts.Interfaces;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Services;

internal class ActAppService : IActAppService
{
    protected readonly IMarsWebApiClient _client;
    protected readonly IMessageService _messageService;
    protected readonly ViewModelService _viewModelService;
    protected readonly NavigationManager _navigationManager;
    protected readonly IXActionFormPresenter _formPresenter;
    protected readonly IReadOnlyDictionary<string, IFrontActionRunner> _frontRunners;

    public ActAppService(IMarsWebApiClient client, IMessageService messageService, ViewModelService viewModelService,
        NavigationManager navigationManager, IXActionFormPresenter formPresenter,
        IEnumerable<IFrontActionRunner> frontActionRunners)
    {
        _client = client;
        _messageService = messageService;
        _viewModelService = viewModelService;
        _navigationManager = navigationManager;
        _formPresenter = formPresenter;
        _frontRunners = frontActionRunners.ToDictionary(r => r.ActionId, r => r);
    }

    public async Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string>? args = null)
    {
        var actions = Q.Site.XActions;

        if (!actions.TryGetValue(id, out var act))
        {
            Console.WriteLine($"ActService: action '{id}' not found");
            return null!;
        }

        return act.Type switch
        {
            XActionType.Link => InjectLink(act),
            XActionType.HostAction => await InjectHostActionAsync(act, args),
            XActionType.FrontAction => await InjectFrontActionAsync(act, args),
            _ => throw new NotImplementedException(),
        };
    }

    XActResult InjectLink(XActionCommand act)
    {
        _navigationManager.NavigateTo(act.LinkValue!);
        return XActResult.ToastSuccess(act.LinkValue ?? "");
    }

    async Task<XActResult> InjectHostActionAsync(XActionCommand act, IReadOnlyDictionary<string, string>? args)
    {
        var resolved = await ResolveArgsAsync(act, args);
        if (resolved is null) return null!;

        try
        {
            var res = await _client.Act.Inject(act.Id, resolved);
            return PresentResult(res);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
            return null!;
        }
    }

    async Task<XActResult> InjectFrontActionAsync(XActionCommand act, IReadOnlyDictionary<string, string>? args)
    {
        var resolved = await ResolveArgsAsync(act, args);
        if (resolved is null) return null!;

        if (!_frontRunners.TryGetValue(act.Id, out var runner))
        {
            _ = _messageService.Error($"фронтовое действие '{act.Id}' не зарегистрировано на клиенте");
            return null!;
        }

        try
        {
            var res = await runner.ExecuteAsync(resolved, CancellationToken.None);
            if (res is null) return null!;
            return PresentResult(res);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
            return null!;
        }
    }

    /// <summary>
    /// Аргументы уже переданы — использовать как есть; иначе при наличии схемы показать форму.
    /// Возвращает null, если пользователь отменил вызов в форме.
    /// </summary>
    async Task<IReadOnlyDictionary<string, string>?> ResolveArgsAsync(XActionCommand act, IReadOnlyDictionary<string, string>? args)
    {
        if (args is not null) return args;

        if (act.Arguments is { Length: > 0 })
        {
            var formValues = await _formPresenter.ShowFormAsync(act);
            if (formValues is null)
            {
                Console.WriteLine($"ActService: command '{act.Id}' cancelled (form)");
                return null;
            }
            return formValues;
        }

        return new Dictionary<string, string>();
    }

    /// <summary>
    /// «Отрисовка результата»: Message → тост; NavigateEffect → навигация;
    /// TriggerEventEffect → клиентская шина. NextAction/Custom возвращаются вызывающему.
    /// </summary>
    XActResult PresentResult(XActResult res)
    {
        if (!string.IsNullOrEmpty(res.Message))
        {
            _ = _messageService.Show(res.Message, res.MessageIntent);
        }

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
}
