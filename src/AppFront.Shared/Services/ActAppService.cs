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

    public ActAppService(IMarsWebApiClient client, IMessageService messageService, ViewModelService viewModelService, NavigationManager navigationManager)
    {
        _client = client;
        _messageService = messageService;
        _viewModelService = viewModelService;
        _navigationManager = navigationManager;
    }

    public async void Inject(string id, string[]? args = null)
    {
        var dev = await _viewModelService.DevAdminExtraViewModel().ConfigureAwait(false);
        var actions = dev.XActions;

        if (actions.TryGetValue(id, out var act))
        {

            if (act.Type == XActionType.Link)
            {
                //_ = messageService.Info($"action '{id}' click");
                _navigationManager.NavigateTo(act.LinkValue!);
            }
            else if (act.Type == XActionType.HostAction)
            {
                var res = await _client.Act.Inject(id, args ?? []);

                if (res.NextStep == XActResult.XActionNextStep.Toast)
                {
                    _ = _messageService.Show(res.Message, res.MessageIntent);
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
        }
    }
}
