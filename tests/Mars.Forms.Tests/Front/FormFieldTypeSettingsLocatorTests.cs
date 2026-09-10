using FluentAssertions;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Forms.Tests.Front;

/// <summary>
/// Реестр панелей настроек — экземпляр, собранный в конструкторе: общие панели скоупа
/// плюс панели конкретного типа поля.
/// </summary>
public class FormFieldTypeSettingsLocatorTests
{
    const string PostScope = "post.systemfields";

    [Fact]
    public void PanelsFor_ReturnsScopePanels_ThenTypePanels()
    {
        var locator = Locator(
            new FormFieldTypeSettingsRegistration(PostScope, typeof(ScopePanel), []),
            new FormFieldTypeSettingsRegistration(PostScope, typeof(TextPanel), [FormFieldType.Text]));

        locator.PanelsFor(PostScope, FormFieldType.Text).Should().Equal(typeof(ScopePanel), typeof(TextPanel));
        locator.PanelsFor(PostScope, FormFieldType.String).Should().Equal(typeof(ScopePanel));
    }

    [Fact]
    public void PanelsFor_UnknownScope_IsEmpty()
    {
        Locator().PanelsFor(PostScope, FormFieldType.Text).Should().BeEmpty();
    }

    [Fact]
    public void SamePanel_InScope_IsRegisteredOnce()
    {
        var locator = Locator(
            new FormFieldTypeSettingsRegistration(PostScope, typeof(TextPanel), [FormFieldType.Text]),
            new FormFieldTypeSettingsRegistration(PostScope, typeof(TextPanel), [FormFieldType.Text]));

        locator.PanelsFor(PostScope, FormFieldType.Text).Should().Equal(typeof(TextPanel));
    }

    //=====================================

    static FormFieldTypeSettingsLocator Locator(params FormFieldTypeSettingsRegistration[] registrations)
        => new(registrations);

    sealed class ScopePanel;

    sealed class TextPanel;
}
