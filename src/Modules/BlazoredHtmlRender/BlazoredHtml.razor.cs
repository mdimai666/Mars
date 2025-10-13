using System.Reflection;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using OneOf;

namespace BlazoredHtmlRender;

public partial class BlazoredHtml
{
    public static Dictionary<string, Type> ComponentsDict = [];

    string _html = "";
    ErrorBoundary? eb;
    [Parameter] public string Html { get => _html; set { _html = value; eb?.Recover(); } }
    [Inject] ILogger<BlazoredHtml> _logger { get; set; } = default!;

    RenderFragment renderHtml => (builder) =>
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(Html ?? "");

        RenderList(builder, doc.DocumentNode.ChildNodes, _logger);

    };

    static object? SetValue(PropertyInfo info, object? value, ILogger<BlazoredHtml> logger)
    {
        if (value is null)
            return null;

        // Определяем реальный тип (если Nullable<T> — достаём T)
        var targetType = IsNullableType(info.PropertyType)
            ? Nullable.GetUnderlyingType(info.PropertyType)!
            : info.PropertyType;

        try
        {
            // Enum и Nullable<Enum>
            if (targetType.IsEnum)
            {
                // Пытаемся парсить по имени или числовому значению
                if (Enum.TryParse(targetType, value.ToString()!, ignoreCase: true, out var parsed))
                    return parsed;

                logger.LogWarning($"[BlazoredHtml] ⚠ Не удалось преобразовать '{value}' в {targetType.Name}");
                return Activator.CreateInstance(targetType);
            }

            // Guid
            if (targetType == typeof(Guid))
                return Guid.Parse(value.ToString()!);

            // bool без значения → true (HTML-style)
            if (targetType == typeof(bool) && (value is "" or null))
                return true;

            // Остальные простые типы
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BlazoredHtml] Ошибка конверсии '{info.Name}' ({info.PropertyType.Name}): {ex.Message}");
            return null;
        }
    }

    static bool IsNullableType(Type type)
    {
        return type.IsGenericType
               && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
    }

    void RenderList(RenderTreeBuilder builder, HtmlNodeCollection ChildNodes, ILogger<BlazoredHtml> logger)
    {
        RenderList(builder, ChildNodes, this, logger);
    }

    static void RenderList(RenderTreeBuilder builder, HtmlNodeCollection ChildNodes, IHandleEvent Context, ILogger<BlazoredHtml> logger)
    {
        int index = 0;

        foreach (var d in ChildNodes)
        {
            if (d.NodeType == HtmlNodeType.Text)
            {
                builder.AddMarkupContent(index, d.InnerHtml);
                index++;
            }
            else if (d.NodeType == HtmlNodeType.Comment)
            {

            }
            else if (d.Name.Length > 4 && ComponentsDict.ContainsKey(d.OriginalName))
            {
                Type com = ComponentsDict[d.OriginalName];

                //Console.WriteLine($">OpenComponent='{d.OriginalName}'");
                builder.OpenComponent(index, com);

                var props = com.GetProperties().ToDictionary(s => s.Name);

                foreach (var a in d.Attributes)
                {
                    bool isComHasProp = props.ContainsKey(a.OriginalName);

                    if (isComHasProp)
                    {
                        PropertyInfo prop = props[a.OriginalName];
                        bool isStringProp = prop.PropertyType == typeof(string);

                        if (isStringProp)
                        {
                            builder.AddAttribute(index, a.Name, a.Value);
                        }
                        else
                        {
                            //Console.WriteLine($"prop.PropertyType='{prop.PropertyType}'");
                            if (prop.PropertyType == typeof(Guid))
                            {
                                builder.AddAttribute(index, a.Name, Guid.Parse(a.Value));
                            }
                            else
                            {
                                //will cast
                                object? castedValue;

                                //if blazor component have BOOL attrubute - default will be TRUE
                                if (a.Value == "" && prop.PropertyType == typeof(bool))
                                {
                                    ////Type type = prop.PropertyType;
                                    ////object defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
                                    //var x = Activator.CreateInstance(com);
                                    ////castedValue = defaultValue;
                                    //prop.SetValue(x, null, null);
                                    //var def = prop.GetValue(x);
                                    //castedValue = def;
                                    castedValue = true;

                                }
                                else if (prop.PropertyType == typeof(OneOf<System.String, RenderFragment, MarkupString>))
                                {
                                    OneOf<System.String, RenderFragment, MarkupString> x = new();
                                    x = a.Value;
                                    castedValue = x;

                                }
                                else if (prop.PropertyType == typeof(EventCallback<MouseEventArgs>))
                                {

                                    //var intp = new DynamicExpresso.Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
                                    //intp.EnableAssignment(AssignmentOperators.All);
                                    ////intp.SetVariable("this", Context);
                                    ////intp.SetVariable(nameof(Console), ref Console);
                                    //intp.Set(nameof(Console), ref Console);

                                    //Parameter pa = new Parameter("e", new MouseEventArgs());
                                    //var de = intp.ParseAsDelegate<Action<MouseEventArgs>>(a.Value, "e");
                                    //intp.Eval(a.Value, new Parameter("e", Context));

                                    EventCallback<MouseEventArgs> e = new(Context, () =>
                                    {
                                        //TODO:EventCallback
                                        logger.LogInformation(">TODO:EventCallback:" + a.OriginalName + " => " + a.Value);
                                    });

                                    castedValue = e;
                                }
                                else
                                {
                                    castedValue = SetValue(prop, a.Value, logger);
                                }
                                builder.AddAttribute(index, a.Name, castedValue);
                            }
                        }

                    }
                    else
                    {
                        builder.AddAttribute(index, a.Name, a.Value);

                    }
                    index++;
                }

                if (d.HasChildNodes)
                {
                    RenderFragment ff = new(builder =>
                    {
                        //builder.AddContent(0, "xqxw");
                        RenderList(builder, d.ChildNodes, Context, logger);
                    });

                    builder.AddAttribute(index++, "ChildContent", ff);
                }

                builder.CloseComponent();
            }
            else
            {
                //Console.WriteLine($">OpenElement='{d.OriginalName}'");
                builder.OpenElement(index, d.OriginalName);

                foreach (var a in d.Attributes)
                {
                    builder.AddAttribute(index, a.Name, a.Value);
                }

                if (d.HasChildNodes)
                {
                    RenderList(builder, d.ChildNodes, Context, logger);
                }
                else
                {
                    builder.AddMarkupContent(index, d.InnerHtml);
                }
                builder.CloseElement();
                index++;
            }
            index++;
        }
    }

    public static Dictionary<string, Type> GetAssemblyComponents(Assembly assembly, bool shortname = false)
    {
        var type = typeof(ComponentBase);

        var types = assembly.GetTypes()
            .Where(p =>
                type.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract
            );

        Dictionary<string, Type> dict = [];

        string[] ignores = { "App", "_Imports" };

        foreach (var d in types)
        {
            if (ignores.Contains(d.Name)) continue;

            var attributes = d.GetCustomAttributes() ?? [];

            bool isLayout = d.IsSubclassOf(typeof(LayoutComponentBase));
            bool isPage = attributes.Any(s => s is RouteAttribute);

            //if (!isLayout && !isPage)
            if (!isLayout)
            {
                dict.Add(d.FullName!, d);
                if (shortname && dict.ContainsKey(d.Name) == false)
                {
                    dict.Add(d.Name, d);
                }
            }

        }

        return dict;
    }

    static object _lock = new { };

    public static void AddComponentsFromAssembly(Assembly assembly, bool tryAddShortName)
    {
        lock (_lock)
        {
            var components = GetAssemblyComponents(assembly, tryAddShortName);

            foreach (var a in components)
            {
                if (BlazoredHtml.ComponentsDict.ContainsKey(a.Key))
                {
                    if (BlazoredHtml.ComponentsDict.TryAdd(a.Value.FullName!, a.Value) == false)
                    {
                        Console.WriteLine($"BlazoredHtml.ComponentsDict > key already exist = {a.Key}");
                    }
                }
                else
                {
                    BlazoredHtml.ComponentsDict.Add(a.Key, a.Value);
                }
            }
        }
    }

}
