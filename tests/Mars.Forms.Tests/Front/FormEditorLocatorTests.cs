using FluentAssertions;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Mars.Forms.Front.Editors;

namespace Mars.Forms.Tests.Front;

/// <summary>
/// Реестр редакторов — экземпляр с состоянием, собранным в конструкторе: встроенные примитивы
/// плюс регистрации потребителя. Тесты не влияют друг на друга, а чтение безопасно из нескольких
/// потоков (после сборки реестр не меняется).
/// </summary>
public class FormEditorLocatorTests
{
    [Fact]
    public void DefaultEditor_ResolvesByFieldType()
    {
        var locator = Locator();

        locator.GetDefaultEditor(FormFieldType.String, false).Should().Be(typeof(FormStringEditor));
        locator.GetDefaultEditor(FormFieldType.Text, false).Should().Be(typeof(FormTextEditor));
        locator.GetDefaultEditor(FormFieldType.Decimal, false).Should().Be(typeof(FormNumberEditor));
        locator.GetDefaultEditor(FormFieldType.Bool, false).Should().Be(typeof(FormBoolEditor));
        locator.GetDefaultEditor(FormFieldType.DateTime, false).Should().Be(typeof(FormDateEditor));
        locator.GetDefaultEditor(FormFieldType.Select, false).Should().Be(typeof(FormSelectEditor));
    }

    [Fact]
    public void DefaultEditor_ForMultiple_ResolvesListEditor()
    {
        Locator().GetDefaultEditor(FormFieldType.String, true).Should().Be(typeof(FormListEditor));
    }

    [Fact]
    public void DefaultEditor_ForSelectMany_ResolvesChoicesEditor()
    {
        var locator = Locator();

        // множественный выбор — чекбоксы вариантов поля, а не общий список значений
        locator.GetDefaultEditor(FormFieldType.SelectMany, false).Should().Be(typeof(FormChoicesEditor));
        locator.GetEditorComponent(FormEditorCatalog.Choices, FormFieldType.SelectMany, false)
               .Should().Be(typeof(FormChoicesEditor));
    }

    [Fact]
    public void DefaultEditor_ForDomainTypes_IsNull_ProviderRegistersItsOwn()
    {
        var locator = Locator();

        locator.GetDefaultEditor(FormFieldType.Relation, false).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Image, true).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Object, false).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Computed, false).Should().BeNull();
    }

    [Fact]
    public void ExplicitKey_ResolvesWhenCompatible()
    {
        Locator().GetEditorComponent(FormEditorCatalog.Multiline, FormFieldType.String, false)
                  .Should().Be(typeof(FormTextEditor));
    }

    [Fact]
    public void IncompatibleOrUnknownKey_ReturnsNull()
    {
        var locator = Locator();

        locator.GetEditorComponent(FormEditorCatalog.Date, FormFieldType.String, false).Should().BeNull();
        locator.GetEditorComponent(FormEditorCatalog.Text, FormFieldType.String, true).Should().BeNull();
        locator.GetEditorComponent("", FormFieldType.String, false).Should().BeNull();
        locator.GetEditorComponent("plugin.unknown", FormFieldType.String, false).Should().BeNull();
    }

    [Fact]
    public void UnnamedRegistration_IsResolvableButNotOffered()
    {
        // доменный редактор провайдера: доступен по ключу дескриптора, в выборе не предлагается
        var locator = Locator(Registration("post.picker.author", FormFieldType.Relation));

        locator.GetEditorComponent("post.picker.author", FormFieldType.Relation, false).Should().Be(typeof(FakeEditor));
        locator.EditorsFor(FormFieldType.Relation, false).Select(editor => editor.Key)
               .Should().NotContain("post.picker.author");
    }

    [Fact]
    public void NamedRegistration_IsOfferedInCatalog()
    {
        // название делает редактор предлагаемым в выборе редактора поля
        var locator = Locator(Registration("plugin.rich", FormFieldType.Text, "Рич-текст"));

        locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false).Should().Be(typeof(FakeEditor));
        locator.EditorsFor(FormFieldType.Text, false).Should().Contain(("plugin.rich", "Рич-текст"));
    }

    [Fact]
    public void Registration_OverridesBuiltIn_ForSameKey()
    {
        var locator = Locator(new FormEditorRegistration(FormEditorCatalog.Date, typeof(FakeEditor), false, null,
            [FormFieldType.DateTime]));

        locator.GetDefaultEditor(FormFieldType.DateTime, false).Should().Be(typeof(FakeEditor));
    }

    [Fact]
    public void Reading_IsThreadSafe()
    {
        var locator = Locator(Registration("plugin.rich", FormFieldType.Text, "Рич-текст"));
        var failures = 0;

        Parallel.For(0, 2000, _ =>
        {
            if (locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false) != typeof(FakeEditor))
                Interlocked.Increment(ref failures);

            if (locator.GetDefaultEditor(FormFieldType.Text, false) != typeof(FormTextEditor))
                Interlocked.Increment(ref failures);

            if (locator.EditorsFor(FormFieldType.Text, false).Count == 0)
                Interlocked.Increment(ref failures);
        });

        failures.Should().Be(0);
    }

    //=====================================

    static FormEditorLocator Locator(params FormEditorRegistration[] registrations) => new(registrations);

    static FormEditorRegistration Registration(string key, FormFieldType fieldType, string? title = null)
        => new(key, typeof(FakeEditor), false, title, [fieldType]);

    sealed class FakeEditor;
}
