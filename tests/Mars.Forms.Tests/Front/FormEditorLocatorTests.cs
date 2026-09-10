using FluentAssertions;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Mars.Forms.Front.Editors;

namespace Mars.Forms.Tests.Front;

public class FormEditorLocatorTests
{
    readonly FormEditorLocator _locator = new();

    [Fact]
    public void DefaultEditor_ResolvesByFieldType()
    {
        _locator.GetDefaultEditor(FormFieldType.String, false).Should().Be(typeof(FormStringEditor));
        _locator.GetDefaultEditor(FormFieldType.Text, false).Should().Be(typeof(FormTextEditor));
        _locator.GetDefaultEditor(FormFieldType.Decimal, false).Should().Be(typeof(FormNumberEditor));
        _locator.GetDefaultEditor(FormFieldType.Bool, false).Should().Be(typeof(FormBoolEditor));
        _locator.GetDefaultEditor(FormFieldType.DateTime, false).Should().Be(typeof(FormDateEditor));
        _locator.GetDefaultEditor(FormFieldType.Select, false).Should().Be(typeof(FormSelectEditor));
    }

    [Fact]
    public void DefaultEditor_ForMultiple_ResolvesListEditor()
    {
        _locator.GetDefaultEditor(FormFieldType.String, true).Should().Be(typeof(FormListEditor));
    }

    [Fact]
    public void DefaultEditor_ForSelectMany_ResolvesChoicesEditor()
    {
        // множественный выбор — чекбоксы вариантов поля, а не общий список значений
        _locator.GetDefaultEditor(FormFieldType.SelectMany, false).Should().Be(typeof(FormChoicesEditor));
        _locator.GetEditorComponent(FormEditorCatalog.Choices, FormFieldType.SelectMany, false)
                .Should().Be(typeof(FormChoicesEditor));
    }

    [Fact]
    public void DefaultEditor_ForDomainTypes_IsNull_ProviderRegistersItsOwn()
    {
        _locator.GetDefaultEditor(FormFieldType.Relation, false).Should().BeNull();
        _locator.GetDefaultEditor(FormFieldType.Image, true).Should().BeNull();
        _locator.GetDefaultEditor(FormFieldType.Object, false).Should().BeNull();
        _locator.GetDefaultEditor(FormFieldType.Computed, false).Should().BeNull();
    }

    [Fact]
    public void ExplicitKey_ResolvesWhenCompatible()
    {
        _locator.GetEditorComponent(FormEditorCatalog.Multiline, FormFieldType.String, false).Should().Be(typeof(FormTextEditor));
    }

    [Fact]
    public void IncompatibleOrUnknownKey_ReturnsNull()
    {
        _locator.GetEditorComponent(FormEditorCatalog.Date, FormFieldType.String, false).Should().BeNull();
        _locator.GetEditorComponent(FormEditorCatalog.Text, FormFieldType.String, true).Should().BeNull();
        _locator.GetEditorComponent("", FormFieldType.String, false).Should().BeNull();
        _locator.GetEditorComponent("plugin.unknown", FormFieldType.String, false).Should().BeNull();
    }

    [Fact]
    public void EditorsFor_OffersOnlyNamedEditors()
    {
        // безымянная регистрация (доменный редактор провайдера) доступна по ключу, но не в выборе
        FormEditorLocator.Register("post.picker.author", typeof(FakeEditor), false, FormFieldType.Relation);

        _locator.GetEditorComponent("post.picker.author", FormFieldType.Relation, false).Should().Be(typeof(FakeEditor));
        _locator.EditorsFor(FormFieldType.Relation, false).Select(e => e.Key).Should().NotContain("post.picker.author");
    }

    [Fact]
    public void Register_WithTitle_ShowsItInCatalog()
    {
        // название делает редактор предлагаемым в выборе редактора поля
        FormEditorLocator.Register("plugin.rich", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);

        _locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false).Should().Be(typeof(FakeEditor));
        _locator.EditorsFor(FormFieldType.Text, false).Should().Contain(("plugin.rich", "Рич-текст"));
    }

    sealed class FakeEditor;
}
