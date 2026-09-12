using FluentAssertions;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Forms.Tests.Front;

/// <summary>
/// Реестр панелей настроек — экземпляр, открытый для регистрации: общие панели скоупа
/// плюс панели конкретного типа поля; записи собираются в момент запроса.
/// </summary>
public class FormFieldTypeSettingsLocatorTests
{
    const string PostScope = "post.systemfields";

    [Fact]
    public void PanelsFor_ReturnsScopePanels_ThenTypePanels()
    {
        var locator = new FormFieldTypeSettingsLocator();
        locator.Register(PostScope, typeof(ScopePanel));
        locator.Register(PostScope, typeof(TextPanel), FormFieldType.Text);

        locator.PanelsFor(PostScope, FormFieldType.Text).Should().Equal(typeof(ScopePanel), typeof(TextPanel));
        locator.PanelsFor(PostScope, FormFieldType.String).Should().Equal(typeof(ScopePanel));
    }

    [Fact]
    public void PanelsFor_UnknownScope_IsEmpty()
    {
        new FormFieldTypeSettingsLocator().PanelsFor(PostScope, FormFieldType.Text).Should().BeEmpty();
    }

    [Fact]
    public void RegistrationAfterRead_IsPickedUp()
    {
        var locator = new FormFieldTypeSettingsLocator();
        locator.PanelsFor(PostScope, FormFieldType.Text).Should().BeEmpty();

        locator.Register(PostScope, typeof(TextPanel), FormFieldType.Text);

        locator.PanelsFor(PostScope, FormFieldType.Text).Should().Equal(typeof(TextPanel));
    }

    [Fact]
    public void SamePanel_InScope_IsRegisteredOnce()
    {
        var locator = new FormFieldTypeSettingsLocator();
        locator.Register(PostScope, typeof(TextPanel), FormFieldType.Text);
        locator.Register(PostScope, typeof(TextPanel), FormFieldType.Text);

        locator.PanelsFor(PostScope, FormFieldType.Text).Should().Equal(typeof(TextPanel));
    }

    sealed class ScopePanel;

    sealed class TextPanel;
}
