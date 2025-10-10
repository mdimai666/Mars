using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Validators;

public class ValidatorFabric : IValidatorFabric
{
    private readonly IServiceProvider _serviceProvider;
    private static Dictionary<Type, List<Type>> _validators = [];

    public ValidatorFabric(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private static Dictionary<Type, List<Type>> ExtractValidatorsFromServiceCollections(IServiceCollection services)
    {
        // составить словарь из generic аргумента. AbstractValidator<(этот)>
        return services.Where(x => !x.ServiceType.IsInterface && typeof(IValidator).IsAssignableFrom(x.ServiceType))
                                .Select(x => x.ServiceType)
                                .GroupBy(GetBaseTypeGenericArgument)
                                .Where(s => !s.Key.IsPrimitive && s.Key != typeof(string))
                                .ToDictionary(g => g.Key, g => g.Where(v => !v.IsAbstract).ToList());
    }

    public static void AddValidatorsFromAssembly(IServiceCollection services, Assembly assembly)
    {
        services.AddValidatorsFromAssembly(assembly);
        var foundValidators = ExtractValidatorsFromServiceCollections(services);

        foreach (var v in foundValidators)
        {
            _validators.TryAdd(v.Key, v.Value);
        }
    }

    /// <summary>
    /// Находит валидатор по Query типу и вызывает Exception
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public async Task ValidateAndThrowAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();

        foreach (var typeKey in _validators.GetValueOrDefault(queryType)
                                ?? throw new KeyNotFoundException($"Validator for '{queryType.Name}' not found"))
        {
            var validator = _serviceProvider.GetRequiredService(typeKey) as IValidator<TQuery>;
            await validator.ValidateAndThrowAsync(query, cancellationToken);
        }
    }

    /// <summary>
    /// Инстацирует валидатор по типу и вызывает Exception
    /// </summary>
    /// <typeparam name="TValidator"></typeparam>
    /// <typeparam name="TQuery"></typeparam>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public async Task ValidateAndThrowAsync<TValidator, TQuery>(TQuery query, CancellationToken cancellationToken = default)
        where TValidator : AbstractValidator<TQuery>
    {
        var validator = _serviceProvider.GetRequiredService<TValidator>();
        await validator.ValidateAndThrowAsync(query, cancellationToken);
    }

    private static Type GetBaseTypeGenericArgument(Type validatorInstanceType)
    {
        var baseType = validatorInstanceType.BaseType;
        while (baseType != null && !baseType.IsGenericType && baseType.GetGenericTypeDefinition() != (typeof(AbstractValidator<>)))
        {
            baseType = validatorInstanceType.BaseType;
        }
        return baseType.GenericTypeArguments[0];
    }
}

public interface IValidatorFabric
{
    Task ValidateAndThrowAsync<TQuery>(TQuery query, CancellationToken cancellationToken = default);
    Task ValidateAndThrowAsync<TValidator, TQuery>(TQuery query, CancellationToken cancellationToken = default)
        where TValidator : AbstractValidator<TQuery>;
}
