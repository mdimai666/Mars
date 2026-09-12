using FluentAssertions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests;

public class FormLayoutTreeTests
{
    [Fact]
    public void Build_NestsChildrenByParent_InListOrder()
    {
        var items = new List<FormItem>
        {
            Node("tab-1", zone: "main", kind: FormItemKind.Container),
            Node("row-1", parent: "tab-1", kind: FormItemKind.Row),
            Node("col-1", parent: "row-1", kind: FormItemKind.Column, width: FormItemWidths.Half),
            Node("col-2", parent: "row-1", kind: FormItemKind.Column, width: FormItemWidths.Half),
            Node("title", parent: "col-1", zone: "main"),
        };

        var roots = FormLayoutTree.Build(items, "main");

        roots.Should().ContainSingle();
        roots[0].Item.Key.Should().Be("tab-1");

        var row = roots[0].Children.Single();
        row.Item.Key.Should().Be("row-1");
        row.Children.Select(c => c.Item.Key).Should().Equal("col-1", "col-2");
        row.Children[0].Children.Single().Item.Key.Should().Be("title");
        row.Children[1].Children.Should().BeEmpty("пустая колонка занимает место, но детей не имеет");
    }

    [Fact]
    public void Build_FiltersByZone_ButKeepsSubtrees()
    {
        var items = new List<FormItem>
        {
            Node("tab-main", zone: "main", kind: FormItemKind.Container),
            Node("row-main", parent: "tab-main", kind: FormItemKind.Row),
            Node("title", parent: "row-main", zone: "main"),
            Node("tab-side", zone: "side", kind: FormItemKind.Container),
        };

        FormLayoutTree.Build(items, "side").Select(r => r.Item.Key).Should().Equal("tab-side");

        var main = FormLayoutTree.Build(items, "main").Single();
        main.Item.Key.Should().Be("tab-main");
        main.Children.Select(c => c.Item.Key).Should().Equal("row-main");
        main.Children[0].Children.Single().Item.Key.Should().Be("title");
    }

    [Fact]
    public void Build_OrphanWithMissingParent_BecomesRoot()
    {
        var items = new List<FormItem>
        {
            Node("title", zone: "main"),
            Node("row-1", parent: "ghost", zone: "main", kind: FormItemKind.Row),
            Node("slug", parent: "row-1", zone: "main"),
        };

        var roots = FormLayoutTree.Build(items, "main");

        roots.Select(r => r.Item.Key).Should().Equal("title", "row-1");
        roots[1].Children.Single().Item.Key.Should().Be("slug");
    }

    [Fact]
    public void Build_Cycle_TerminatesAndShowsEveryNodeOnce()
    {
        var items = new List<FormItem>
        {
            Node("a", parent: "b", zone: "main", kind: FormItemKind.Row),
            Node("b", parent: "a", zone: "main", kind: FormItemKind.Row),
        };

        var roots = FormLayoutTree.Build(items, "main");

        Flatten(roots).Select(i => i.Key).Should().Equal("a", "b");
    }

    [Fact]
    public void Build_EmptyLayout_IsEmpty()
        => FormLayoutTree.Build([], "main").Should().BeEmpty();

    static IEnumerable<FormItem> Flatten(IEnumerable<FormLayoutNode> nodes)
        => nodes.SelectMany(n => new[] { n.Item }.Concat(Flatten(n.Children)));

    static FormItem Node(string key, string? parent = null, string? zone = null,
                         FormItemKind kind = FormItemKind.Field, string? width = null)
        => new() { Key = key, Parent = parent, Zone = zone, Kind = kind, Width = width };
}
