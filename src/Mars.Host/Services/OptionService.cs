using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.Options;
using Mars.Host.Shared.Exceptions;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Options.Models;
using Mars.Shared.Common;
using Mars.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mars.Host.Services;

// Singletone
internal class OptionService : IOptionService
{
    internal Dictionary<Type, object> localCache = [];
    internal static JsonSerializerOptions serializerOptions = new();

    public SysOptions SysOption => ((SysOptions)localCache[typeof(SysOptions)]) ?? new();
    public bool IsDevelopment { get; }

    internal Dictionary<string, Type> RegisteredOptions { get; set; } = [];
    internal Dictionary<string, Type> OptionsAppendToInitialSiteData { get; set; } = [];

    internal Dictionary<Type, object> ConstOptions { get; set; } = [];

    private readonly IOptionRepository _optionRepository;
    private readonly IEventManager _eventManager;
    private readonly IMemoryCache _memoryCache;
    private readonly IHostEnvironment _environment;
    private Dictionary<Type, Action<object>> onChangeActions = [];
    private IServiceScope _scope;

    private FileHostingInfo? _fileHostingInfo;

    public OptionService(
        IServiceScopeFactory scopeFactory,
        IEventManager eventManager,
        IMemoryCache memoryCache,
        IHostEnvironment environment)
    {
        _scope = scopeFactory.CreateScope();
        IsDevelopment = environment.IsDevelopment();

        _optionRepository = _scope.ServiceProvider.GetRequiredService<IOptionRepository>();
        _eventManager = eventManager;
        _memoryCache = memoryCache;
        _environment = environment;
    }

    public void SaveOption<T>(T option) where T : class
        => SaveOptionAsync(option, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    public async Task SaveOptionAsync<T>(T option, CancellationToken cancellationToken)
        where T : class
    {
        string key = typeof(T).Name;

        if (!RegisteredOptions.ContainsKey(key)) throw new OptionNotRegisteredException(key);

        var exist = GetOptionFromRepo<T>();

        if (option is INormalizableBeforeWriteValue normalizableOption) normalizableOption.NormalizeBeforeWrite();

        if (exist is null)
        {
            await _optionRepository.Create(new CreateOptionQuery<T> { Key = key, Value = option }, cancellationToken);
        }
        else
        {
            await _optionRepository.Update(new UpdateOptionQuery<T> { Key = key, Value = option }, cancellationToken);
        }

        SetOptionOnMemory(option);

        if (onChangeActions.TryGetValue(typeof(T), out Action<object>? action))
        {
            action(option);
            var eventTopic = _eventManager.Defaults.OptionUpdate(typeof(T).Name);
            _eventManager.TriggerEvent(new ManagerEventPayload(eventTopic, option));
        }

        if (typeof(T) == typeof(SysOptions)) _fileHostingInfo = null;
    }

    private T? GetOptionFromRepo<T>() where T : class
    {
        Type t = typeof(T);
        string key = t.Name;

        var exist = _optionRepository.GetKey<T>(key).ConfigureAwait(false).GetAwaiter().GetResult();

        if (exist is not null)
        {
            if (exist is INormalizableAfterReadValue normalizableAfterRead) normalizableAfterRead.NormalizeAfterRead();
        }
        return exist;
    }

    public T GetOption<T>() where T : class, new()
    {
        Type t = typeof(T);
        string key = t.Name;

        if (!RegisteredOptions.ContainsKey(key)) throw new OptionNotRegisteredException(key);

        localCache.TryGetValue(t, out var opt);

        if (opt is null)
        {
            T _opt = GetOptionFromRepo<T>() ?? new();
            opt = _opt;
            localCache.Add(t, opt);
        }

        return (T)opt;
    }

    public object GetOption(Type type)
    {
        try
        {
            MethodInfo method = GetType().GetMethod(nameof(GetOption), [])!;
            MethodInfo generic = method.MakeGenericMethod(type);
            return generic.Invoke(this, null)!;
        }
        catch (Exception ex) when (ex.InnerException is OptionNotRegisteredException _ex)
        {
            throw _ex;
        }
    }

    public object GetOptionByClass(string className)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className, nameof(className));

        if (RegisteredOptions.TryGetValue(className, out var type))
        {
            return GetOption(type);
        }
        throw new OptionNotRegisteredException(className);
    }

    public void SetOptionOnMemory<T>(T option) where T : class
    {
        Type t = typeof(T);
        string key = t.Name;

        if (!localCache.TryAdd(t, option))
        {
            localCache[t] = option;
        }

        if (typeof(T) == typeof(SysOptions)) _fileHostingInfo = null;
    }

    public void RegisterOption<T>(Action<T>? onChangeHook = null, bool appendToInitialSiteData = false)
    {
        Type t = typeof(T);
        string key = t.Name;

        RegisteredOptions.Add(key, t);

        if (appendToInitialSiteData)
        {
            OptionsAppendToInitialSiteData.Add(key, t);
        }

        if (onChangeHook is not null)
        {
            onChangeActions.Add(t, e => onChangeHook((T)e));
            //var eventTopic = EventManager.Defaults.Options.OptionUpdate(typeof(T).Name);
            //_eventManager.TriggerEvent(new ManagerEventPayload(eventTopic, valAsObject));
        }
    }

    /// <summary>
    /// add or update ConstOption
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="appendToInitialSiteData"></param>
    public void SetConstOption<T>(T value, bool appendToInitialSiteData = false) where T : class
    {
        Type t = typeof(T);
        if (!ConstOptions.TryAdd(t, value))
        {
            ConstOptions[t] = value;
        }

        if (appendToInitialSiteData)
        {
            string key = t.Name;
            if (OptionsAppendToInitialSiteData.ContainsKey(key)) OptionsAppendToInitialSiteData.Remove(key);
            OptionsAppendToInitialSiteData.Add(key, t);
        }
    }

    public T? GetConstOption<T>() where T : class
    {
        return (T?)ConstOptions.GetValueOrDefault(typeof(T));
    }

    public List<OptionSummary> GetOptionsForInitialSiteData()
    {
        CheckAllRegisteredOptionInLocalCache();

        var keys = OptionsAppendToInitialSiteData.Select(s => s.Key);
        var options = localCache.Where(s => keys.Contains(s.Key.Name))
            .Select(AsSummary)
            .ToList();

        var constOption = ConstOptions.Where(s => keys.Contains(s.Key.Name))
            .Select(AsSummary)
            .ToList();

        return [.. options, .. constOption];
    }

    private OptionSummary AsSummary(KeyValuePair<Type, object> opt)
        => new()
        {
            Key = opt.Key.Name,
            Type = opt.Key.FullName!,
            Value = System.Text.Json.JsonSerializer.Serialize(opt.Value, serializerOptions)
        };

    void CheckAllRegisteredOptionInLocalCache()
    {
        var keys = RegisteredOptions.Select(s => s.Key).ToList();
        var localKeys = localCache.Keys.Select(s => s.Name).ToList();

        var diff = keys.Except(localKeys);

        if (diff.Count() == 0) return;

        var fromDb = _optionRepository.ListAll(CancellationToken.None)
                            .ConfigureAwait(false).GetAwaiter().GetResult()
                            .Where(opt => keys.Contains(opt.Key))
                            .ToList();

        foreach (var opt in fromDb)
        {
            string className = opt.Key;
            if (RegisteredOptions.TryGetValue(className, out var t))
            {
                var val = JsonSerializer.Deserialize(opt.Value, t, serializerOptions)!;
                if (val is INormalizableAfterReadValue normalizableAfterRead) normalizableAfterRead.NormalizeAfterRead();
                if (localCache.ContainsKey(t))
                {
                    localCache[t] = val;
                }
                else
                {
                    localCache.Add(t, val);
                }
            }
        }

        var fromDbKeys = fromDb.Select(s => s.Key);
        var nonExistInDb = RegisteredOptions.Keys.Except(fromDbKeys).ToList();

        foreach (var optKey in nonExistInDb)
        {
            string className = optKey;
            if (RegisteredOptions.TryGetValue(className, out var t))
            {
                var val = Activator.CreateInstance(t)!;
                if (val is INormalizableAfterReadValue normalizableAfterRead) normalizableAfterRead.NormalizeAfterRead();

                if (localCache.ContainsKey(t))
                {
                    localCache[t] = val;
                }
                else
                {
                    localCache.Add(t, val);
                }
            }
        }

    }

    public void SetOptionByClass(string className, string jsonString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className, nameof(className));

        var val = JsonSerializer.Deserialize<JsonObject>(jsonString, serializerOptions);

        if (RegisteredOptions.TryGetValue(className, out var t))
        {
            object serializedValue = val.Deserialize(t)!;

            try
            {
                MethodInfo method = GetType().GetMethods().FirstOrDefault(s => s.Name == nameof(SaveOption) && s.GetParameters().Count() == 1)!;
                MethodInfo generic = method.MakeGenericMethod(t);
                _ = generic.Invoke(this, [serializedValue])!;
            }
            catch (Exception ex) when (ex.InnerException is OptionNotRegisteredException _ex)
            {
                throw _ex;
            }
        }
        else
        {
            throw new OptionNotRegisteredException(className);
        }
    }

    public UserActionResult<SmtpSettingsModel> SaveSmtpSettings(SmtpSettingsModel form)
    {
        try
        {
            SaveOption(form);
            return UserActionResult<SmtpSettingsModel>.Success(form, "Успешно сохранено");
        }
        catch (Exception ex)
        {
            return UserActionResult<SmtpSettingsModel>.Exception(ex);
        }
    }

    public string RobotsTxt()
    {
        return ((SEOOption)localCache[typeof(SEOOption)]).RobotsTxt;
    }

    public SmtpSettingsModel MailSettings =>
        ((SmtpSettingsModel)localCache[typeof(SmtpSettingsModel)]);

    public FileHostingInfo FileHostingInfo()
        => _fileHostingInfo ??= new()
        {
            Backend = new Uri(SysOption.SiteUrl),
            PhysicalPath = new Uri(Path.Join(_environment.ContentRootPath, "wwwroot", "upload"), UriKind.Absolute),
            RequestPath = "upload"
        };

    public string GetDefaultDatabaseConnectionString() => IOptionService.Configuration.GetConnectionString("DefaultConnection")!;
}
