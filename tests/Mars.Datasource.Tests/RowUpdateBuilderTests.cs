using FluentAssertions;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;

namespace Mars.Datasource.Tests;

public class RowUpdateBuilderTests
{
    static Func<string, string> Quote(char start, char end) => name => $"{start}{name}{end}";

    static SqlUpdatePlan? Build(
        IReadOnlyDictionary<string, string?> changes,
        IReadOnlyDictionary<string, string?>? keys = null,
        string? schema = "public",
        IReadOnlyList<string>? keyColumns = null)
        => RowUpdateBuilder.Build(
            schema,
            "todo",
            Quote('"', '"'),
            keyColumns ?? ["id"],
            keys ?? new Dictionary<string, string?> { ["id"] = "42" },
            changes);

    [Fact]
    public void Build_SingleChange_GeneratesParameterizedUpdate()
    {
        var plan = Build(new Dictionary<string, string?> { ["title"] = "new" });

        plan.Should().NotBeNull();
        plan!.Sql.Should().Be("""UPDATE "public"."todo" SET "title" = @p1 WHERE "id" = @p2""");
        plan.Parameters.Should().HaveCount(2);
        plan.Parameters[0].Name.Should().Be("p1");
        plan.Parameters[0].Value.Should().Be("new");
        plan.Parameters[1].Value.Should().Be("42");
    }

    [Fact]
    public void Build_CompositeKey_AddsAllKeyConditions()
    {
        var plan = Build(
            new Dictionary<string, string?> { ["value"] = "1" },
            new Dictionary<string, string?> { ["a"] = "1", ["b"] = "2" },
            schema: null,
            keyColumns: ["a", "b"]);

        plan.Should().NotBeNull();
        plan!.Sql.Should().Be("""UPDATE "todo" SET "value" = @p1 WHERE "a" = @p2 AND "b" = @p3""");
        plan.Parameters.Should().HaveCount(3);
    }

    [Fact]
    public void Build_NullValue_KeepsNullParameter()
    {
        var plan = Build(new Dictionary<string, string?> { ["content"] = null });

        plan.Should().NotBeNull();
        plan!.Parameters[0].Value.Should().BeNull();
    }

    [Fact]
    public void Build_NoChanges_ReturnsNull()
    {
        Build(new Dictionary<string, string?>()).Should().BeNull();
    }

    [Fact]
    public void Build_NoKeyColumns_ReturnsNull()
    {
        Build(new Dictionary<string, string?> { ["title"] = "x" }, keyColumns: []).Should().BeNull();
    }

    [Fact]
    public void Build_MissingKeyValue_ReturnsNull()
    {
        Build(
            new Dictionary<string, string?> { ["title"] = "x" },
            new Dictionary<string, string?> { ["other"] = "1" }).Should().BeNull();
    }

    [Fact]
    public void Build_MsSqlStyleQuoting_UsesBrackets()
    {
        var plan = RowUpdateBuilder.Build(
            "dbo",
            "todo",
            Quote('[', ']'),
            ["Id"],
            new Dictionary<string, string?> { ["Id"] = "1" },
            new Dictionary<string, string?> { ["Title"] = "x" });

        plan.Should().NotBeNull();
        plan!.Sql.Should().Be("UPDATE [dbo].[todo] SET [Title] = @p1 WHERE [Id] = @p2");
    }
}
