using FluentAssertions;
using Mars.Forms.Abstractions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Tests;

public class FormDataProviderLocatorTests
{
    [Fact]
    public void ResolvesExactKey()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IFormDataProvider>("form.feedback", (_, _) => new TestProvider("form.feedback"));

        var provider = Resolve(services, "form.feedback");

        provider!.OwnerModel.Should().Be("form.feedback");
    }

    [Fact]
    public void FallsBackToWildcardScope()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IFormDataProvider>("post.*", (_, _) => new TestProvider("post.*"));

        Resolve(services, "post.article")!.OwnerModel.Should().Be("post.*");
    }

    [Fact]
    public void LongestWildcardScope_Wins()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IFormDataProvider>("sql.*", (_, _) => new TestProvider("sql.*"));
        services.AddKeyedScoped<IFormDataProvider>("sql.ds1.*", (_, _) => new TestProvider("sql.ds1.*"));

        Resolve(services, "sql.ds1.orders")!.OwnerModel.Should().Be("sql.ds1.*");
    }

    [Fact]
    public void UnknownOwner_ReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IFormDataProvider>("post.*", (_, _) => new TestProvider("post.*"));

        Resolve(services, "sql.ds.orders").Should().BeNull();
        Resolve(services, "").Should().BeNull();
    }

    [Fact]
    public void OwnerModels_ListsRegisteredKeys()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IFormDataProvider>("post.*", (_, _) => new TestProvider("post.*"));
        services.AddKeyedScoped<IFormDataProvider>("form.feedback", (_, _) => new TestProvider("form.feedback"));

        new FormDataProviderLocator(services).OwnerModels.Should().BeEquivalentTo("post.*", "form.feedback");
    }

    [Fact]
    public void AddMarsForms_RegistersEngineServices()
    {
        var services = new ServiceCollection().AddMarsForms();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFormDefinitionNormalizer>().Should().NotBeNull();
        provider.GetRequiredService<IFormDataProviderLocator>().Should().NotBeNull();
        provider.GetRequiredService<IFormValidator>().Should().NotBeNull();

        // встроенные правила считает FormRuleEvaluator, в реестре — только правила провайдеров
        provider.GetRequiredService<IFormRuleRegistry>().KnownTypes("any.owner").Should().BeEmpty();
    }

    static IFormDataProvider? Resolve(IServiceCollection services, string ownerModel)
    {
        var locator = new FormDataProviderLocator(services);
        return locator.GetProvider(ownerModel, services.BuildServiceProvider());
    }

    sealed class TestProvider(string ownerModel) : IFormDataProvider
    {
        public string OwnerModel { get; } = ownerModel;

        public Task<FormDefinition> GetFormAsync(FormContext context, CancellationToken cancellationToken)
            => Task.FromResult(new FormDefinition { OwnerModel = OwnerModel });
    }
}
