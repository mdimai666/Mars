using System.Reflection;
using Mars.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Shared.Localizer;

public class HbcLocalizerResx : IStringLocalizer
{
    private readonly IStringLocalizer _localizer;

    Dictionary<string, string> _dict;

    //public MyLocalizer(StringLocalizer<AppRes> sl)
    //{
    //    _localizer = sl;

    //    _dict = new Dictionary<string, string>
    //    {
    //        ["MedDepartment.many"] = "Отделения"
    //    };
    //}
    public HbcLocalizerResx(IStringLocalizerFactory factory, Dictionary<string, string> dict)
    {
        var type = typeof(AppRes);
        var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName!);
        _localizer = factory.Create("MyResourceType", assemblyName.Name!);

        _dict = dict;
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (_dict.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, value);
            }
            return _localizer[name];
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (_dict.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, value);
            }
            return _localizer[name, arguments];
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizer.GetAllStrings(includeParentCultures);
    }
}

public class DictLocalizer : IStringLocalizer
{
    Dictionary<string, string> _dict;

    public DictLocalizer(Dictionary<string, string> dict)
    {
        _dict = dict;
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (_dict.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, value);
            }
            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (_dict.TryGetValue(name, out var value))
            {
                return new LocalizedString(name, string.Format(value, arguments));
            }
            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _dict.Select(s => new LocalizedString(s.Key, s.Value));
    }
}
