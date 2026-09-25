using FluentAssertions;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Mars.Forms.Front.Editors;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Forms.Tests.Front;

/// <summary>
/// Реестр редакторов — экземпляр, открытый для регистрации откуда угодно: записи складываются
/// в список, а поиск по ключу собирается в момент запроса. Поэтому регистрация, сделанная после
/// первого чтения (например, из загруженного позже плагина), видна сразу, а параллельное чтение
/// не мешает регистрации.
/// </summary>
public class FormEditorLocatorTests
{
    [Fact]
    public void DefaultEditor_ResolvesByFieldType()
    {
        var locator = new FormEditorLocator();

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
        new FormEditorLocator().GetDefaultEditor(FormFieldType.String, true).Should().Be(typeof(FormListEditor));
    }

    [Fact]
    public void DefaultEditor_ForSelectMany_ResolvesChoicesEditor()
    {
        var locator = new FormEditorLocator();

        // множественный выбор — чекбоксы вариантов поля, а не общий список значений
        locator.GetDefaultEditor(FormFieldType.SelectMany, false).Should().Be(typeof(FormChoicesEditor));
        locator.GetEditorComponent(FormEditorCatalog.Choices, FormFieldType.SelectMany, false)
               .Should().Be(typeof(FormChoicesEditor));
    }

    [Fact]
    public void DefaultEditor_ForDomainTypes_IsNull_ProviderRegistersItsOwn()
    {
        var locator = new FormEditorLocator();

        locator.GetDefaultEditor(FormFieldType.Relation, false).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Image, true).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Object, false).Should().BeNull();
        locator.GetDefaultEditor(FormFieldType.Computed, false).Should().BeNull();
    }

    [Fact]
    public void ExplicitKey_ResolvesWhenCompatible()
    {
        new FormEditorLocator().GetEditorComponent(FormEditorCatalog.Multiline, FormFieldType.String, false)
                               .Should().Be(typeof(FormTextEditor));
    }

    [Fact]
    public void IncompatibleOrUnknownKey_ReturnsNull()
    {
        var locator = new FormEditorLocator();

        locator.GetEditorComponent(FormEditorCatalog.Date, FormFieldType.String, false).Should().BeNull();
        locator.GetEditorComponent(FormEditorCatalog.Text, FormFieldType.String, true).Should().BeNull();
        locator.GetEditorComponent("", FormFieldType.String, false).Should().BeNull();
        locator.GetEditorComponent("plugin.unknown", FormFieldType.String, false).Should().BeNull();
    }

    [Fact]
    public void UnnamedRegistration_IsResolvableButNotOffered()
    {
        // доменный редактор провайдера: доступен по ключу дескриптора, в выборе не предлагается
        var locator = new FormEditorLocator();
        locator.Register("post.picker.author", typeof(FakeEditor), false, null, FormFieldType.Relation);

        locator.GetEditorComponent("post.picker.author", FormFieldType.Relation, false).Should().Be(typeof(FakeEditor));
        locator.EditorsFor(FormFieldType.Relation, false).Select(editor => editor.Key)
               .Should().NotContain("post.picker.author");
    }

    [Fact]
    public void NamedRegistration_IsOfferedInCatalog()
    {
        // название делает редактор предлагаемым в выборе редактора поля
        var locator = new FormEditorLocator();
        locator.Register("plugin.rich", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);

        locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false).Should().Be(typeof(FakeEditor));
        locator.EditorsFor(FormFieldType.Text, false).Should().Contain(("plugin.rich", "Рич-текст"));
    }

    [Fact]
    public void RegistrationAfterRead_IsPickedUp()
    {
        var locator = new FormEditorLocator();
        locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false)
               .Should().BeNull("реестр ещё не знает ключ");

        locator.Register("plugin.rich", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);

        locator.GetEditorComponent("plugin.rich", FormFieldType.Text, false).Should().Be(typeof(FakeEditor));
        locator.EditorsFor(FormFieldType.Text, false).Should().Contain(("plugin.rich", "Рич-текст"));
    }

    [Fact]
    public void Registration_OverridesBuiltIn_ForSameKey()
    {
        var locator = new FormEditorLocator();
        locator.Register(FormEditorCatalog.Date, typeof(FakeEditor), false, null, FormFieldType.DateTime);

        locator.GetDefaultEditor(FormFieldType.DateTime, false).Should().Be(typeof(FakeEditor));
    }

    [Fact]
    public void RegisteredFromServiceProvider_IsVisibleToReaders()
    {
        // админка наполняет реестр после сборки контейнера, резолвя синглтон из провайдера
        var services = new ServiceCollection();
        services.AddMarsFormsFront();

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IFormEditorLocator>()
                .Register("plugin.rich", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);

        provider.GetRequiredService<IFormEditorLocator>()
                .GetEditorComponent("plugin.rich", FormFieldType.Text, false)
                .Should().Be(typeof(FakeEditor));
    }

    [Fact]
    public void Reading_IsThreadSafe_DuringRegistration()
    {
        var locator = new FormEditorLocator();
        locator.Register("plugin.rich", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);
        var failures = 0;

        Parallel.For(0, 2000, i =>
        {
            if (i % 100 == 0)
                locator.Register($"plugin.editor{i}", typeof(FakeEditor), false, "Рич-текст", FormFieldType.Text);

            if (locator.GetDefaultEditor(FormFieldType.Text, false) != typeof(FormTextEditor))
                Interlocked.Increment(ref failures);

            if (locator.GetEditorComponent(FormEditorCatalog.Multiline, FormFieldType.Text, false) != typeof(FormTextEditor))
                Interlocked.Increment(ref failures);

            if (!locator.EditorsFor(FormFieldType.Text, false).Any(editor => editor.Key == "plugin.rich"))
                Interlocked.Increment(ref failures);
        });

        failures.Should().Be(0);
    }

    sealed class FakeEditor;
}
