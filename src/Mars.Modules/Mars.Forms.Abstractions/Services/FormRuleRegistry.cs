using System.Text.Json.Nodes;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;

namespace Mars.Forms.Abstractions.Services;

internal class FormRuleRegistry : IFormRuleRegistry
{
    static readonly Task<IEnumerable<string>> EmptyTask = Task.FromResult(Enumerable.Empty<string>());

    readonly Dictionary<string, FormRuleHandler> _global = new(StringComparer.Ordinal);
    readonly Dictionary<string, Dictionary<string, FormRuleHandler>> _scoped = new(StringComparer.Ordinal);
    readonly object _lock = new();

    public void Register(string type, FormRuleHandler handler)
    {
        lock (_lock) _global[type] = handler;
    }

    public void Register(string ownerModel, string type, FormRuleHandler handler)
    {
        lock (_lock)
        {
            if (!_scoped.TryGetValue(ownerModel, out var handlers))
                _scoped[ownerModel] = handlers = new Dictionary<string, FormRuleHandler>(StringComparer.Ordinal);

            handlers[type] = handler;
        }
    }

    public bool IsKnown(string ownerModel, string type) => Resolve(ownerModel, type) is not null;

    public IReadOnlyCollection<string> KnownTypes(string ownerModel)
    {
        lock (_lock)
        {
            var types = new HashSet<string>(_global.Keys, StringComparer.Ordinal);
            foreach (var scope in FormOwnerScopes.Of(ownerModel))
            {
                if (!_scoped.TryGetValue(scope, out var handlers)) continue;
                foreach (var type in handlers.Keys) types.Add(type);
            }

            return types.ToList();
        }
    }

    public ValueTask<IEnumerable<string>> ValidateAsync(FormRuleDefinition rule, object? value,
                                                        FormFieldValidationContext context,
                                                        CancellationToken cancellationToken)
        => Resolve(context.OwnerModel, rule.Type) is { } handler
            ? handler(value, rule.Params, context, cancellationToken)
            : new ValueTask<IEnumerable<string>>(EmptyTask);

    FormRuleHandler? Resolve(string ownerModel, string type)
    {
        lock (_lock)
        {
            foreach (var scope in FormOwnerScopes.Of(ownerModel))
            {
                if (_scoped.TryGetValue(scope, out var handlers) && handlers.TryGetValue(type, out var handler))
                    return handler;
            }

            return _global.TryGetValue(type, out var global) ? global : null;
        }
    }
}
