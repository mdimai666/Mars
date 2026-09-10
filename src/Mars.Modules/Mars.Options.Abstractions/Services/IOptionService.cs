using Mars.Contracts.Dto.Files;
using Mars.Core.Exceptions;
using Mars.Options.Abstractions.Dto;
using Mars.Options.Abstractions.Exceptions;

namespace Mars.Options.Abstractions.Services;

/// <summary>
/// Singletone service
/// </summary>
public interface IOptionService
{
    bool IsDevelopment { get; }

    event Action<object> OnOptionUpdate;

    ///<summary>
    ///SaveOption
    /// </summary>
    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    void SaveOption<T>(T option) where T : class;

    /// <summary>
    /// SaveOptionAsync
    /// </summary>
    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    Task SaveOptionAsync<T>(T option, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// GetOption
    /// </summary>
    /// <exception cref="OptionNotRegisteredException" />
    T GetOption<T>() where T : class, new();
    object GetOption(Type type);
    object GetOptionByClass(string className);

    /// <summary>
    /// Имена классов зарегистрированных опций, доступных через GetOptionByClass/SetOptionByClass.
    /// </summary>
    IReadOnlyList<string> GetRegisteredOptionClasses();

    void SetOptionOnMemory<T>(T option) where T : class;
    void RegisterOption<T>(Action<T>? onChangeHook = null, bool appendToInitialSiteData = false);
    void SetConstOption<T>(T value, bool appendToInitialSiteData = false) where T : class;
    T? GetConstOption<T>() where T : class;
    List<OptionSummary> GetOptionsForInitialSiteData();

    /// <summary>
    /// Обновить опцию по имени класса (см. GetRegisteredOptionClasses).
    /// </summary>
    /// <exception cref="MarsValidationException" />
    /// <exception cref="OptionNotRegisteredException" />
    void SetOptionByClass(string className, string jsonString);

    /// <summary>
    /// Настройки пути к /upload
    /// </summary>
    /// <returns></returns>
    FileHostingInfo FileHostingInfo();
    string GetDefaultDatabaseConnectionString();

}
