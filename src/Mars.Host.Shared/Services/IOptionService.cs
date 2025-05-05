using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.Options;
using Mars.Host.Shared.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Options;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Shared.Services;

/// <summary>
/// Singletone service
/// </summary>
public interface IOptionService
{
    static ConfigurationManager Configuration { get; set; } = default!;

    SysOptions SysOption { get; }
    bool IsDevelopment { get; }

    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    void SaveOption<T>(T option) where T : class;

    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    Task SaveOptionAsync<T>(T option, CancellationToken cancellationToken) where T : class;

    /// <exception cref="OptionNotRegisteredException" />
    T GetOption<T>() where T : class, new();
    object GetOption(Type type);
    object GetOptionByClass(string className);
    void SetOptionOnMemory<T>(T option) where T : class;
    void RegisterOption<T>(Action<T>? onChangeHook = null, bool appendToInitialSiteData = false);
    void SetConstOption<T>(T value, bool appendToInitialSiteData = false) where T : class;
    T? GetConstOption<T>() where T : class;
    List<OptionSummary> GetOptionsForInitialSiteData();

    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    void SetOptionByClass(string className, string jsonString);
    UserActionResult<SmtpSettingsModel> SaveSmtpSettings(SmtpSettingsModel form);
    string RobotsTxt();
    SmtpSettingsModel MailSettings { get; }

    FileHostingInfo FileHostingInfo();
    
}
