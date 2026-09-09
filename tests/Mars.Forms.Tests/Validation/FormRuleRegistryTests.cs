using FluentAssertions;
using Mars.Forms.Abstractions.Services;
using Mars.Forms.Abstractions.Validation;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests.Validation;

public class FormRuleRegistryTests
{
    readonly FormRuleRegistry _registry = new();

    [Fact]
    public void GlobalRule_IsKnownForAnyOwner()
    {
        _registry.Register("myRule", Handler("global"));

        _registry.IsKnown("post.article", "myRule").Should().BeTrue();
        _registry.IsKnown("sql.ds.orders", "myRule").Should().BeTrue();
    }

    [Fact]
    public void ScopedRule_IsKnownOnlyInsideScope()
    {
        _registry.Register("post.*", "myRule", Handler("posts"));

        _registry.IsKnown("post.article", "myRule").Should().BeTrue();
        _registry.IsKnown("sql.ds.orders", "myRule").Should().BeFalse();
    }

    [Fact]
    public async Task ExactScope_WinsOverWildcardAndGlobal()
    {
        _registry.Register("myRule", Handler("global"));
        _registry.Register("post.*", "myRule", Handler("wildcard"));
        _registry.Register("post.article", "myRule", Handler("exact"));

        var errors = await _registry.ValidateAsync(Rule("myRule"), null, Context("post.article"), CancellationToken.None);

        errors.Should().Equal("exact");
    }

    [Fact]
    public async Task LongestWildcardScope_Wins()
    {
        _registry.Register("sql.*", "myRule", Handler("short"));
        _registry.Register("sql.ds1.*", "myRule", Handler("long"));

        var errors = await _registry.ValidateAsync(Rule("myRule"), null, Context("sql.ds1.orders"), CancellationToken.None);

        errors.Should().Equal("long");
    }

    [Fact]
    public async Task UnknownRule_IsSoftlySkipped()
    {
        var errors = await _registry.ValidateAsync(Rule("nope"), null, Context("post.article"), CancellationToken.None);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void KnownTypes_MergesGlobalAndScopes()
    {
        BuiltInFormRules.RegisterAll(_registry);
        _registry.Register("sql.*", "notNull", Handler("sql"));

        _registry.KnownTypes("sql.ds.orders").Should().Contain("notNull").And.Contain(FormRuleCatalog.Required);
        _registry.KnownTypes("post.article").Should().NotContain("notNull");
    }

    [Fact]
    public void BuiltInRules_CoverCatalogWithoutUnique()
    {
        BuiltInFormRules.RegisterAll(_registry);

        _registry.KnownTypes("any.owner").Should().BeEquivalentTo(FormRuleCatalog.BuiltIn);
        FormRuleCatalog.BuiltIn.Should().NotContain(FormRuleCatalog.Unique);
    }

    static FormFieldValidationContext Context(string ownerModel) => new() { OwnerModel = ownerModel };

    static FormRuleDefinition Rule(string type) => new() { Type = type };

    static FormRuleHandler Handler(string marker)
        => (_, _, _, _) => ValueTask.FromResult<IEnumerable<string>>([marker]);
}
