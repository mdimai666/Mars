using FluentAssertions;
using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using static Mars.Server.Tests.Forms.PostFormTestHost;

namespace Mars.Server.Tests.Forms;

/// <summary>
/// Форма владельца без системных слотов (пользователи, категории постов): плоское дерево метаполей
/// в одну зону, порядок по <c>Order</c>, состав — как в прежнем рендере списка метаполей.
/// </summary>
public class MetaFieldsFormBuilderTests
{
    [Fact]
    public void Build_OrdersByOrder_InSingleZone()
    {
        var form = MetaFieldsFormBuilder.Build("user.default", "Пользователь",
            [Meta("bio", 2), Meta("nickname", 1)]);

        form.OwnerModel.Should().Be("user.default");
        form.Items.Select(i => i.Key).Should().Equal("nickname", "bio");
        form.Items.Should().OnlyContain(i => i.Zone == SystemFieldsCatalog.Zones.Main);
        form.Items.Should().OnlyContain(i => i.Kind == FormItemKinds.Field);
    }

    [Fact]
    public void Build_ExcludesDisabledAndQuery_AndHiddenOnClient()
    {
        var fields = new[]
        {
            Meta("a", 0),
            Meta("off", 1, disabled: true),
            Meta("calc", 2, type: MetaFieldType.Query),
            Meta("secret", 3, hidden: true),
        };

        MetaFieldsFormBuilder.Build("postcategory.default", "Категория", fields)
                             .Items.Select(i => i.Key).Should().Equal("a", "secret");

        MetaFieldsFormBuilder.Build("postcategory.default", "Категория", fields, client: true)
                             .Items.Select(i => i.Key).Should().Equal("a");
    }

    [Fact]
    public void Build_CarriesDescriptors_WithMetaFormEditorKeys()
    {
        var form = MetaFieldsFormBuilder.Build("user.default", "Пользователь",
            [Meta("bio", 0, type: MetaFieldType.Text), Meta("photos", 1, type: MetaFieldType.Image)]);

        var bio = form.Items.First(i => i.Key == "bio").Field!;
        bio.Type.Should().Be(FormFieldType.Text);
        bio.Editor.Should().Be(MetaFormEditors.Value);
        bio.SettingsOnForm.Should().BeFalse("правила и редактор метаполя живут на определении поля");

        form.Items.First(i => i.Key == "photos").Field!.Editor.Should().Be(MetaFormEditors.File);
    }

    [Fact]
    public void Manifest_DeclaresSingleZone_AndNoValuesTransport()
    {
        var manifest = MetaFieldsFormBuilder.Build("user.default", "Пользователь", []).Manifest!;

        manifest.Zones.Select(z => z.Key).Should().Equal(SystemFieldsCatalog.Zones.Main);
        manifest.Capabilities.CanAddFields.Should().BeFalse();
        manifest.Capabilities.CanReadValues.Should().BeFalse();
        manifest.Capabilities.CanSubmit.Should().BeFalse();
    }

    [Fact]
    public void OwnerModels_ArePrefixedByOwnerAndType()
    {
        MetaFieldsFormBuilder.UserOwnerModel("default").Should().Be("user.default");
        MetaFieldsFormBuilder.PostCategoryOwnerModel("rubric").Should().Be("postcategory.rubric");
    }
}
