using System.Reflection;
using System.Web;
using HandlebarsDotNet;
using Mars.Host.Data.Contexts;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.QueryLang;
using Microsoft.EntityFrameworkCore;

namespace Mars.WebSiteProcessor.Handlebars.HandlebarsFunc;

/// <summary>
/// при вызове {{#help}} выводит справочную информацию по зарегистрированным хелперам и функциям
/// </summary>
public class MyHandlebarsHelpBlock
{
    List<HbsHelperItem> _hbsHelperItems;
    List<HbsContextFunctionItem> _hbsContextFunctionItems;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IQueryLangHelperAvailableMethodsProvider _queryLangHelperAvailableMethodsProvider;

    public MyHandlebarsHelpBlock(in HelperOptions options,
                                ITemplatorFeaturesLocator tflocator,
                                IMetaModelTypesLocator metaModelTypesLocator,
                                IQueryLangHelperAvailableMethodsProvider queryLangHelperAvailableMethodsProvider)
    {
        _hbsHelperItems = ReadHelperItems(options);
        _hbsContextFunctionItems = ReadFunctionItems(tflocator);
        _metaModelTypesLocator = metaModelTypesLocator;
        _queryLangHelperAvailableMethodsProvider = queryLangHelperAvailableMethodsProvider;
    }

    public void WriteTo(in EncodedTextWriter output)
    {
        output.WriteSafeString("""
        <div class="card">
            <div class="card-body">
        """);

        void startDetails(in EncodedTextWriter output, string title)
        {
            output.WriteSafeString($"<details><summary>{title}</summary>");
        }

        void endDetails(in EncodedTextWriter output)
        {
            output.WriteSafeString("</details>");
        }

        startDetails(output, "Available Handlebars Helpers");
        OutputHelpersTable(output, _hbsHelperItems);
        endDetails(output);

        startDetails(output, "Registered #context functions");
        output.WriteSafeString("""
                <code>
                <pre class="p-2 border">
                {{#context}}
                x == 1 + 2
                y == x*2
                req = Req(...)
                posts = ef.post.Take(10)
                page == int.Parse(_req.Query["page"]??"1")
                userId == _user.Id
                {{/context}}
                </pre>
                </code>
                """);
        OutputFunctionsTable(output, _hbsContextFunctionItems, "Registered #context functions");
        endDetails(output);

        var items = ReadEfEntitiesItems(_metaModelTypesLocator);
        startDetails(output, "Registered #context ef Models");
        OutputQueryLangLinqDatabaseQueryHandlerTable(output, items);
        endDetails(output);

        var efMethods = ReadEfStringQueryAvailableMethodItems(_queryLangHelperAvailableMethodsProvider);
        startDetails(output, "Supported Query ef.&lt;type&gt;.&lt;Method&gt; methods");
        OutputFunctionsTable(output, efMethods, "Supported Query ef.&lt;type&gt;.&lt;Method&gt; methods");
        endDetails(output);

        startDetails(output, "Useful tricks");
        OutputUsefulTricks(output);
        endDetails(output);

        output.WriteSafeString("""
            </div>
        </div>
        """);
    }

    string? HtmlEncode(string? html) => HttpUtility.HtmlEncode(html);

    private List<HbsHelperItem> ReadHelperItems(in HelperOptions options)
    {
        var configurationProp = options.Frame.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(p => p.Name == "Configuration");
        var configuration = (ICompiledHandlebarsConfiguration)configurationProp.GetValue(options.Frame)!;

        var list = new List<HbsHelperItem>();

        foreach (var helper in configuration.BlockHelpers)
        {
            //var prop = typeof(HandlebarsDotNet.Helpers.BlockHelpers.DelegateBlockHelperDescriptor)
            //                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            //                .First(p => p.Name == "_helper");

            if (helper.Value.Value is HandlebarsDotNet.Helpers.BlockHelpers.MissingBlockHelperDescriptor)
            {
                continue;
            }

            if (helper.Value.Value is not HandlebarsDotNet.Helpers.BlockHelpers.DelegateBlockHelperDescriptor)
            {
                list.Add(new HbsHelperItem(helper.Key.PathInfo.TrimmedPath, HandlebarsHelperType.Block, null, null));
                continue;
            }

            var prop = helper.Value.Value.GetType()
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                            .FirstOrDefault(p => p.Name == "_helper");

            var _helperMethod = prop.GetValue(helper.Value.Value) as Delegate;
            var displayAttr = _helperMethod.Method.GetCustomAttribute<TemplatorHelperInfoAttribute>();
            //var t = _helper.GetType();
            list.Add(new HbsHelperItem(helper.Key.PathInfo.TrimmedPath, HandlebarsHelperType.Block, displayAttr, _helperMethod));
        }

        foreach (var helper in configuration.Helpers/*.OrderBy(h => h.Key)*/)
        {
            if (helper.Value.Value is HandlebarsDotNet.Helpers.LateBindHelperDescriptor) //skip simple variables. Like _dev, bodyClass, ...
            {
                continue;
            }

            if (helper.Value.Value is HandlebarsDotNet.Helpers.MissingHelperDescriptor)
            {
                continue;
            }

            if (helper.Value.Value is not HandlebarsDotNet.Helpers.DelegateHelperDescriptor and not HandlebarsDotNet.Helpers.DelegateHelperWithOptionsDescriptor)
            {
                list.Add(new HbsHelperItem(helper.Key.PathInfo.TrimmedPath, HandlebarsHelperType.Inline, null, null));
                continue;
            }

            var prop = helper.Value.Value.GetType()
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                            .FirstOrDefault(p => p.Name == "_helper");
            var _helperMethod = prop.GetValue(helper.Value.Value) as Delegate;
            var displayAttr = _helperMethod.Method.GetCustomAttribute<TemplatorHelperInfoAttribute>();
            //var t = _helper.GetType();
            list.Add(new HbsHelperItem(helper.Key.PathInfo.TrimmedPath, HandlebarsHelperType.Inline, displayAttr, _helperMethod));
        }

        return list;
    }

    private void OutputHelpersTable(in EncodedTextWriter output, List<HbsHelperItem> list)
    {
        output.WriteSafeString("<h3>Available Handlebars Helpers</h3>");
        output.WriteSafeString("<table class=\"table\">");
        output.WriteSafeString("""
                                <tr>
                                    <th>Helper Name</th>
                                    <th>Type</th>
                                    <th>Example</th>
                                    <th>Description</th>
                                </tr>
                                """);

        foreach (var item in list.OrderByDescending(s => s.Type).ThenBy(s => s.Key))
        {
            output.WriteSafeString("<tr>");
            output.WriteSafeString($"<td>{item.Key}</td>");
            output.WriteSafeString($"<td class=\"text-secondary\" >{item.Type}</td>");
            output.WriteSafeString($"<td><code>{HtmlEncode(item.Attribute?.Example)}</code></td >");
            if (item.Attribute is not null)
            {
                output.WriteSafeString($"<td>{HtmlEncode(item.Attribute?.Description)}</td>");
            }
            else
            {
                output.WriteSafeString("<td></td>");
            }
            output.WriteSafeString("</tr>");
        }

        output.WriteSafeString("</table>");
    }

    private List<HbsContextFunctionItem> ReadFunctionItems(ITemplatorFeaturesLocator tflocator)
    {
        return tflocator.Functions.Select(f =>
        {
            var attr = f.Value.Method.GetCustomAttribute<TemplatorHelperInfoAttribute>();
            return new HbsContextFunctionItem(f.Key, attr);
        }).ToList();
    }

    private void OutputFunctionsTable(in EncodedTextWriter output, IEnumerable<HbsContextFunctionItem> list, string title)
    {
        output.WriteSafeString($"<h3>{title}</h3>");
        output.WriteSafeString("<table class=\"table\">");
        output.WriteSafeString("""
                                <tr>
                                    <th>Helper Name</th>
                                    <th>Example</th>
                                    <th>Description</th>
                                </tr>
                                """);

        foreach (var item in list)
        {
            output.WriteSafeString("<tr>");
            output.WriteSafeString($"<td>{item.Key}</td>");
            output.WriteSafeString($"<td><code>{HtmlEncode(item.Attribute?.Example)}</code></td>");
            output.WriteSafeString($"<td>{HtmlEncode(item.Attribute?.Description)}</td>");
            output.WriteSafeString("</tr>");
        }

        output.WriteSafeString("</table>");
    }

    private IEnumerable<HbsQueryLangLinqDatabaseQueryHandlerItem> ReadEfEntitiesItems(IMetaModelTypesLocator metaModelTypesLocator)
    {
        foreach (var (key, postType) in metaModelTypesLocator.PostTypesDict())
        {
            var listName = key.ToLower() + "s";

            var attr = new TemplatorHelperInfoAttribute(key, $"""{listName} = ef.{key}.Table(page, 20)""", postType.Title);
            yield return new HbsQueryLangLinqDatabaseQueryHandlerItem(key, attr);
        }

        var memberDbSetsByName = typeof(MarsDbContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && (p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)))
                .ToDictionary(s => s.Name);

        foreach (var prop in memberDbSetsByName)
        {
            var listName = prop.Key.ToLower();

            var attr = new TemplatorHelperInfoAttribute(prop.Key, $"""{listName} = ef.{prop.Key}.Take(10)""", "");
            yield return new HbsQueryLangLinqDatabaseQueryHandlerItem(prop.Key, attr);
        }
    }

    private void OutputQueryLangLinqDatabaseQueryHandlerTable(in EncodedTextWriter output, IEnumerable<HbsQueryLangLinqDatabaseQueryHandlerItem> list)
    {
        output.WriteSafeString("<h3>Registered #context ef Models</h3>");
        output.WriteSafeString("<table class=\"table\">");
        output.WriteSafeString("""
                                <tr>
                                    <th>Type name</th>
                                    <th>Example</th>
                                    <th>Description</th>
                                </tr>
                                """);

        foreach (var item in list)
        {
            output.WriteSafeString("<tr>");
            output.WriteSafeString($"<td>{item.Key}</td>");
            output.WriteSafeString($"<td><code>{HtmlEncode(item.Attribute?.Example)}</code></td>");
            output.WriteSafeString($"<td>{HtmlEncode(item.Attribute?.Description)}</td>");
            output.WriteSafeString("</tr>");
        }

        output.WriteSafeString("</table>");
    }

    private IEnumerable<HbsContextFunctionItem> ReadEfStringQueryAvailableMethodItems(IQueryLangHelperAvailableMethodsProvider provider)
    {
        return provider.AvailableMethods().Select(x => new HbsContextFunctionItem(x.Shortcut, x)).OrderBy(s => s.Key);
    }

    private void OutputUsefulTricks(in EncodedTextWriter output)
    {
        output.WriteSafeString("""
        <table class="table">
            <tr>
                <th>title</th>
                <th>item</th>
            </tr>
            <tr>
                <td>Uri id pattern</td>
                <td>
                    <code>
                        @page "/posts/{id}"<br/>
                        post = news.First(post.Id == Guid.Parse(id))
                    </code>
                </td>
            </tr>
            <tr>
                <td>Uri Slug pattern</td>
                <td>
                    <code>
                        @page "/posts/{Slug}"<br/>
                        post = ef.post.First(post.Slug.ToLower() == Slug.ToLower())
                    </code>
                </td>
            </tr>
            <tr>
                <td>Paginator</td>
                <td>
                    <code>
                        page == int.Parse(_req.Query["page"]??"1")<br/>
                        table = ef.post.OrderByDescending(CreatedAt).Table(page,10)<br/>
                        posts == table.Items
                    </code>
                </td>
            </tr>
            <tr>
                <td>Union</td>
                <td>
                    <code>posts = Union(myType.Where(post.pinned==true).Take(1), myType.OrderByDescending(Slug))</code>
                </td>
            </tr>
        </table>
        """);
    }
}

internal enum HandlebarsHelperType
{
    Block,
    Inline,
    Decorator
}

internal record HbsHelperItem(string Key, HandlebarsHelperType Type, TemplatorHelperInfoAttribute? Attribute, Delegate? Delegate);
internal record HbsContextFunctionItem(string Key, TemplatorHelperInfoAttribute? Attribute);
internal record HbsQueryLangLinqDatabaseQueryHandlerItem(string Key, TemplatorHelperInfoAttribute? Attribute);
