using FluentAssertions;
using Mars.Data.Entities;
using Mars.QueryLang.Host.Services;
using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.QueryLang.Tests;

public class EfStringQuerySyntaxTests
{
    static IQueryable<PostEntity> CreatePosts() => new List<PostEntity>
    {
        new() { Title = "111", Slug = "a", PostType = new PostTypeEntity { TypeName = "z" } },
        new() { Title = "111", Slug = "b", PostType = new PostTypeEntity { TypeName = "a" } },
        new() { Title = "000", Slug = "c", PostType = new PostTypeEntity { TypeName = "m" } },
    }.AsQueryable();

    static EfStringQuery Q() => new(CreatePosts(), new XInterpreter());

    class Item
    {
        public int Price { get; set; }
    }

    static EfStringQuery Items() => new(new List<Item>
    {
        new() { Price = 10 },
        new() { Price = 30 },
        new() { Price = 20 },
    }.AsQueryable(), new XInterpreter());

    [Fact]
    public void Where_LegacyBareLambdaSyntax_ReturnSameResult()
    {
        Q().Where("post.Title==\"111\"").ToList().Cast<PostEntity>().Count().Should().Be(2);
        Q().Where("Title==\"111\"").ToList().Cast<PostEntity>().Count().Should().Be(2);
        Q().Where("p => p.Title==\"111\"").ToList().Cast<PostEntity>().Count().Should().Be(2);
    }

    [Fact]
    public void Where_BareMethodCallOnMember_Works()
    {
        Q().Where("Title.StartsWith(\"11\")").ToList().Cast<PostEntity>().Count().Should().Be(2);
    }

    [Fact]
    public void Where_ArrowInsideStringLiteral_NotTreatedAsLambda()
    {
        var posts = new List<PostEntity>
        {
            new() { Title = "x=>y", Slug = "a" },
            new() { Title = "111", Slug = "b" },
        }.AsQueryable();

        var bare = new EfStringQuery(posts, new XInterpreter()).Where("Title == \"x=>y\"").ToList().Cast<PostEntity>().ToList();
        bare.Should().ContainSingle().Which.Slug.Should().Be("a");

        var lambda = new EfStringQuery(posts, new XInterpreter()).Where("p => p.Title == \"x=>y\"").ToList().Cast<PostEntity>().ToList();
        lambda.Should().ContainSingle().Which.Slug.Should().Be("a");
    }

    [Fact]
    public void OrderBy_DottedKeySelector_Works()
    {
        var ordered = Q().OrderBy("PostType.TypeName").ToList().Cast<PostEntity>().ToList();
        ordered.Select(s => s.Slug).Should().ContainInOrder("b", "c", "a");
    }

    [Fact]
    public void Select_ProjectionBecomesCurrentQuery()
    {
        var q = Q().Where("Title==\"111\"");
        var projection = (IQueryable)q.Select("Title");

        projection.ElementType.Should().Be(typeof(string));
        q.ToList().Cast<string>().Should().OnlyContain(t => t == "111");
    }

    [Fact]
    public void First_Last_AreTerminal_DoNotNarrowQuery()
    {
        var q = Q().Where("Title==\"111\"");
        var first = (PostEntity)q.First()!;
        first.Title.Should().Be("111");
        q.ToList().Cast<PostEntity>().Count().Should().Be(2);

        var ordered = Q().Where("Title==\"111\"").OrderBy("Slug");
        var last = (PostEntity)ordered.Last()!;
        last.Slug.Should().Be("b");
        ordered.ToList().Cast<PostEntity>().Count().Should().Be(2);
    }

    [Fact]
    public void Count_WithAndWithoutPredicate()
    {
        Q().Count().Should().Be(3);
        Q().Count("Title==\"111\"").Should().Be(2);
    }

    [Fact]
    public void Any_WithAndWithoutPredicate()
    {
        Q().Any().Should().BeTrue();
        Q().Any("Title==\"111\"").Should().BeTrue();
        Q().Any("Title==\"xxx\"").Should().BeFalse();
    }

    [Fact]
    public void All_ChecksEveryElement()
    {
        Q().All("Title.Length==3").Should().BeTrue();
        Q().All("Title==\"111\"").Should().BeFalse();
    }

    [Fact]
    public void Distinct_DeduplicatesProjection()
    {
        var q = Q();
        q.Select("Title");
        q.Distinct().ToList().Cast<string>().Should().BeEquivalentTo(["111", "000"]);
    }

    [Fact]
    public void DistinctBy_KeepsOneElementPerKey()
    {
        var result = Q().DistinctBy("Title").ToList().Cast<PostEntity>().ToList();

        result.Should().HaveCount(2);
        result.Select(s => s.Title).Should().BeEquivalentTo(["111", "000"]);
    }

    [Fact]
    public void MaxBy_MinBy_ReturnElement_AreTerminal()
    {
        Items().MaxBy("Price").Should().BeOfType<Item>().Which.Price.Should().Be(30);
        Items().MinBy("Price").Should().BeOfType<Item>().Which.Price.Should().Be(10);

        var q = Q();
        ((PostEntity)q.MaxBy("PostType.TypeName")!).Slug.Should().Be("a");
        ((PostEntity)q.MinBy("PostType.TypeName")!).Slug.Should().Be("b");
        q.ToList().Cast<PostEntity>().Should().HaveCount(3);
    }

    [Fact]
    public void Max_Min_OnFieldAndDottedPath()
    {
        Q().Max("PostType.TypeName").Should().Be("z");
        Q().Min("PostType.TypeName").Should().Be("a");
        Items().Max("Price").Should().Be(30);
        Items().Min("Price").Should().Be(10);
    }

    [Fact]
    public void Sum_Average_OnNumericField()
    {
        Items().Sum("Price").Should().Be(60);
        Items().Average("Price").Should().Be(20d);
    }

    [Fact]
    public void Sum_OnNonNumericField_Throws()
    {
        var act = () => Q().Sum("Title");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Table_Paginates()
    {
        var table = Q().OrderBy("Slug").Table("1, 2") as TotalResponse2<PostEntity>;

        table.Should().NotBeNull();
        table!.Items.Count.Should().Be(2);
        table.TotalCount.Should().Be(3);
        table.HasMoreData.Should().BeTrue();
    }

    [Fact]
    public void Union_ViaInvokeMethodArgs_MergesQueries()
    {
        var q = Q().Where("Title==\"111\"");
        var second = Q().Where("Title==\"000\"").GetQuery();

        var union = (EfStringQuery)((IDynamicQueryableObject)q).InvokeMethodArgs(nameof(EfStringQuery.Union), [second])!;

        union.ToList().Cast<PostEntity>().Count().Should().Be(3);
    }

    [Fact]
    public void MethodsMapping_ContainsAllStringMethods()
    {
        var map = Q().MethodsMapping();

        map.Keys.Should().Contain([
            nameof(EfStringQuery.Count), nameof(EfStringQuery.Any), nameof(EfStringQuery.All),
            nameof(EfStringQuery.First), nameof(EfStringQuery.Last),
            nameof(EfStringQuery.Where), nameof(EfStringQuery.OrderBy), nameof(EfStringQuery.OrderByDescending),
            nameof(EfStringQuery.ThenBy), nameof(EfStringQuery.ThenByDescending),
            nameof(EfStringQuery.Skip), nameof(EfStringQuery.Take), nameof(EfStringQuery.ToList),
            nameof(EfStringQuery.Distinct), nameof(EfStringQuery.DistinctBy),
            nameof(EfStringQuery.Max), nameof(EfStringQuery.Min),
            nameof(EfStringQuery.MaxBy), nameof(EfStringQuery.MinBy),
            nameof(EfStringQuery.Sum), nameof(EfStringQuery.Average),
            nameof(EfStringQuery.Select), nameof(EfStringQuery.Include), nameof(EfStringQuery.Table),
            nameof(EfStringQuery.Search), nameof(EfStringQuery.Union),
        ]);
    }

    [Fact]
    public void Skip_Take_StringExpressions()
    {
        Q().OrderBy("Slug").Skip("1").Take("1").ToList().Cast<PostEntity>()
            .Should().ContainSingle().Which.Slug.Should().Be("b");
    }
}
