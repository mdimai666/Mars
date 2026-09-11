using FluentAssertions;
using Mars.Admin.Framework.Components.Forms;
using Mars.Forms.Contracts;

namespace Mars.Admin.Framework.Tests.Components;

/// <summary>
/// Черновик раскладки: добавление узлов сетки, перенос поддеревом, проверка допустимости
/// родителей, палитра неразмещённых полей и выгрузка раскладки без дескрипторов.
/// </summary>
public class FormLayoutDraftTests
{
    [Fact]
    public void AddContainer_AppendsTabToZone()
    {
        var draft = Draft();

        var tab = draft.AddContainer("main", "Основное")!;

        tab.Kind.Should().Be(FormItemKind.Container);
        tab.Zone.Should().Be("main");
        tab.Parent.Should().BeNull();
        draft.RootsOf("main").Last().Key.Should().Be(tab.Key);
        draft.HasContainer("main").Should().BeTrue();
        draft.HasContainer("side").Should().BeFalse();
    }

    [Fact]
    public void AddRowColumnsAndField_BuildsGrid_EmptyColumnStays()
    {
        var draft = Draft();
        var tab = draft.AddContainer("main", "Основное")!;
        var row = draft.AddRow(tab.Key)!;
        var left = draft.AddColumn(row.Key, FormItemWidths.Half)!;
        var right = draft.AddColumn(row.Key, FormItemWidths.Half)!;

        var title = draft.PlaceField("title", left.Key)!;

        title.Parent.Should().Be(left.Key);
        title.Zone.Should().Be("main", "зона наследуется от контейнера");
        draft.ChildrenOf(row.Key).Select(c => c.Key).Should().Equal(left.Key, right.Key);
        draft.ChildrenOf(right.Key).Should().BeEmpty("пустая колонка занимает место, но детей не имеет");

        var tree = FormLayoutTree.Build(draft.Items, "main");
        var tabRoot = tree.Single(node => node.Item.Key == tab.Key);
        tabRoot.Children.Single().Children.Select(c => c.Item.Key).Should().Equal(left.Key, right.Key);
    }

    [Fact]
    public void PlaceField_PutsRemovedFieldBack_IntoItsOwnZone()
    {
        var draft = Draft();
        draft.UnplacedFields.Should().BeEmpty("все поля провайдера уже стоят в раскладке");

        draft.Remove("title").Should().BeTrue();
        draft.UnplacedFields.Select(f => f.Key).Should().Equal("title");

        draft.PlaceField("title", null).Should().NotBeNull();

        draft.UnplacedFields.Should().BeEmpty();
        draft.Find("title")!.Zone.Should().Be("main", "поле возвращается в свою зону по умолчанию");
        draft.Find("title")!.Field.Should().NotBeNull("дескриптор берётся из определения провайдера");
    }

    [Fact]
    public void PlaceField_IntoForbiddenParent_IsRejected()
    {
        var draft = Draft();

        draft.PlaceField("slug", "title").Should().BeNull("поле не может держать детей");
        draft.PlaceField("ghost", null).Should().BeNull();
        draft.Find("slug")!.Parent.Should().BeNull("неудачная попытка ничего не двигает");
    }

    [Fact]
    public void Move_TakesSubtreeToAnotherParent()
    {
        var draft = Draft();
        var first = draft.AddContainer("main", "Первый")!;
        var second = draft.AddContainer("main", "Второй")!;
        var row = draft.AddRow(first.Key)!;
        var column = draft.AddColumn(row.Key, FormItemWidths.Half)!;
        var slug = draft.PlaceField("slug", column.Key)!;

        draft.Move(row.Key, second.Key).Should().BeTrue();

        draft.Find(row.Key)!.Parent.Should().Be(second.Key);
        draft.Find(column.Key)!.Parent.Should().Be(row.Key, "поддерево переезжает целиком");
        draft.Find(slug.Key)!.Parent.Should().Be(column.Key);
        draft.ChildrenOf(first.Key).Should().BeEmpty();
        draft.ChildrenOf(second.Key).Select(c => c.Key).Should().Equal(row.Key);
    }

    [Fact]
    public void Move_IntoOwnSubtree_IsRejected()
    {
        var draft = Draft();
        var row = draft.AddRow(null)!;
        var column = draft.AddColumn(row.Key, FormItemWidths.Half)!;

        draft.Move(row.Key, column.Key).Should().BeFalse();
        draft.Move(row.Key, row.Key).Should().BeFalse();
    }

    [Fact]
    public void Move_ToForbiddenParent_IsRejected()
    {
        var draft = Draft();
        var tab = draft.AddContainer("main", "Основное")!;
        var row = draft.AddRow(tab.Key)!;
        var column = draft.AddColumn(row.Key, FormItemWidths.Half)!;

        draft.Move(column.Key, tab.Key).Should().BeFalse("колонка живёт только в ряду");
        draft.Move(column.Key, null).Should().BeFalse();
        draft.Move(tab.Key, row.Key).Should().BeFalse("контейнер не вкладывается");
    }

    [Fact]
    public void Move_BeforeNode_ReordersSiblings()
    {
        var draft = Draft();
        var row = draft.AddRow(null)!;
        var column = draft.AddColumn(row.Key, FormItemWidths.Full)!;
        var title = draft.PlaceField("title", column.Key)!;
        var slug = draft.PlaceField("slug", column.Key)!;

        draft.ChildrenOf(column.Key).Select(i => i.Key).Should().Equal(title.Key, slug.Key);

        draft.Move(slug.Key, column.Key, title.Key).Should().BeTrue();

        draft.ChildrenOf(column.Key).Select(i => i.Key).Should().Equal(slug.Key, title.Key);
    }

    [Fact]
    public void Move_BetweenZones_AdoptsTargetZone()
    {
        var draft = Draft();
        var row = draft.AddRow(null)!;
        row.Zone.Should().Be("main", "первая зона — по умолчанию");

        var column = draft.AddColumn(row.Key, FormItemWidths.Half)!;
        var title = draft.PlaceField("title", column.Key)!;
        title.Zone.Should().Be("main");

        var tab = draft.AddContainer("side", "Публикация")!;

        draft.Move(title.Key, tab.Key).Should().BeTrue();

        draft.Find(title.Key)!.Zone.Should().Be("side", "зона переезжает вместе с узлом");
        draft.Find(title.Key)!.Parent.Should().Be(tab.Key);
        draft.Find(column.Key)!.Parent.Should().Be(row.Key);
    }

    [Fact]
    public void Remove_StructureNode_TakesSubtree_FieldsReturnToPalette()
    {
        var draft = Draft();
        var row = draft.AddRow(null)!;
        var column = draft.AddColumn(row.Key, FormItemWidths.Half)!;
        draft.PlaceField("title", column.Key);

        draft.Remove(row.Key).Should().BeTrue();

        draft.Items.Select(item => item.Key).Should().Equal("slug", "status");
        draft.UnplacedFields.Select(f => f.Key).Should().Equal("title");
    }

    [Fact]
    public void Properties_AreEditable()
    {
        var draft = Draft();
        var column = draft.AddColumn(draft.AddRow(null)!.Key, FormItemWidths.Half)!;
        var title = draft.PlaceField("title", column.Key)!;

        draft.SetWidth(column.Key, FormItemWidths.Third).Should().BeTrue();
        draft.SetTitle(title.Key, "Заголовок").Should().BeTrue();
        draft.SetVisible(title.Key, false).Should().BeTrue();

        draft.Find(column.Key)!.Width.Should().Be(FormItemWidths.Third);
        draft.Find(title.Key)!.Title.Should().Be("Заголовок");
        draft.Find(title.Key)!.Visible.Should().BeFalse();

        draft.SetWidth("ghost", FormItemWidths.Half).Should().BeFalse();
        draft.SetTitle(title.Key, "  ").Should().BeTrue();
        draft.Find(title.Key)!.Title.Should().BeNull("пустой заголовок — это его отсутствие");
    }

    [Fact]
    public void ToSettings_StripsDescriptors()
    {
        var draft = Draft();
        draft.PlaceField("title", null);

        var settings = draft.ToSettings();

        settings.Items.Should().OnlyContain(item => item.Field == null);
        settings.Items.Select(item => item.Key).Should().Contain("title");
    }

    static FormLayoutDraft Draft()
    {
        var items = new List<FormItem>
        {
            Field("title", "main"),
            Field("slug", "main"),
            Field("status", "side"),
        };

        return new FormLayoutDraft(items, [new() { Key = "main", Title = "Основное" },
                                           new() { Key = "side", Title = "Публикация" }]);
    }

    static FormItem Field(string key, string zone) => new()
    {
        Key = key,
        Zone = zone,
        Field = new FormFieldDescriptor { Key = key, Title = key, Type = FormFieldType.String },
    };
}
