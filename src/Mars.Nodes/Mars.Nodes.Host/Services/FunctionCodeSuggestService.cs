using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Services;

public class FunctionCodeSuggestService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;
    private readonly IDevAdminConnectionService _devAdminConnectionService;
    int TAKE_COUNT = 10;

    public FunctionCodeSuggestService(IServiceProvider serviceProvider,
                                    IServiceCollection services,
                                    IDevAdminConnectionService devAdminConnectionService)
    {
        _serviceProvider = serviceProvider;
        _services = services;
        _devAdminConnectionService = devAdminConnectionService;
    }

    public Task<List<KeyValuePair<string, string>>> FunctionCodeSuggest(string f_action, string? search)
    {
        List<KeyValuePair<string, string>> list = [];

        if (f_action == "di:services")
        {
            var sc = _services;

            Func<ServiceDescriptor, KeyValuePair<string, string>> sget =
                (s) => new(s.ServiceType.Name, $"var {FirstCharToLowerCaseAndVarName(s.ServiceType.Name)} = RED.GetService<{s.ServiceType.Name}>();");

            list = sc.Where(s => s.ServiceType.FullName.StartsWith("Mars"))
                            .Select(sget)
                            .Where(s => string.IsNullOrEmpty(search) || s.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
                            .Take(TAKE_COUNT)
                            .ToList();
        }
        else if (f_action == $"{nameof(IEventManager)}.dict")
        {
            var eventManager = _serviceProvider.GetRequiredService<IEventManager>();

            list = eventManager.DeclaredEvents()
                            .Where(s => string.IsNullOrEmpty(search) || s.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
                            .Take(TAKE_COUNT)
                            .ToList();
        }
        else if (f_action == $"GetPagesContexts.dict")
        {
            var pages = _devAdminConnectionService.GetPageContexts();

            list = pages.Where(s => string.IsNullOrEmpty(search)
                                    || s.PageTypeName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || s.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .Take(TAKE_COUNT)
                        .Select(x => new KeyValuePair<string, string>(x.PageTypeName, x.DisplayName))
                        .ToList();
        }

        return Task.FromResult(list);
    }

    static string? FirstCharToLowerCaseAndVarName(string? str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
        {
            if (str[0] == 'I') str = str[1..];
            return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];
        }

        return str;
    }
}
