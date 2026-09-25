using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Mars.Cms.Abstractions.Services;
using Mars.Core.Extensions;
using Mars.QueryLang.Services;
using Mars.Server.Abstractions.Interfaces;
using Mars.SiteEngine.Abstractions.Constants.Website;
using Mars.SiteEngine.Abstractions.Templators;
using Mars.SiteEngine.Abstractions.WebSite.Scripts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Scriban;
using Scriban.Runtime;

namespace Mars.SiteEngine.Scriban.Extensions;

/// <summary>
/// Сайт-функции Scriban (scope "site"). Семантика повторяет сайт-хелперы Handlebars;
/// условия/циклы/сравнения не дублируются — в Scriban они нативные (if/for/==/&gt;/&lt;).
/// </summary>
public static class SiteScribanFunctions
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    //=========================================================
    // QueryLang: {{ context "posts = ef.Post.Take(3)" key? cache? }}
    //=========================================================
    public static object? Context(TemplateContext tctx, string body, string? key = null, string? cache = null)
    {
        var rctx = ScribanRenderContext.From(tctx);

        bool isCache = string.IsNullOrEmpty(cache) == false;
        string cacheKey = $"context.--{key}";
        IMemoryCache? memoryCache = null;

        if (isCache && string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("context function with 'cache' must have 'key'");
        }
        else if (isCache)
        {
            memoryCache = rctx.ServiceProvider.GetRequiredService<IMemoryCache>();

            if (memoryCache.TryGetValue(cacheKey, out IEnumerable<KeyValuePair<string, object>>? vv))
            {
                foreach (var x in vv!)
                {
                    rctx.DataObject[x.Key] = x.Value;
                }
                return null;
            }
        }

        var queryRows = DataQueryBodyParser.FunctionBodyParse(body, key);

        Dictionary<string, object>? dCopy = null;
        if (isCache)
        {
            dCopy = SnapshotData(rctx.DataObject);
        }

        if (queryRows.Queries.Any())
            rctx.PageContext.DataQueries.Add(key ?? Guid.NewGuid().ToString(), queryRows);

        ContextQueryProcessor.Process(rctx.PageContext, rctx.ServiceProvider, rctx.CancellationToken)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        // процессор пишет результаты в TemplateContextVariables — доносим новое в данные шаблона
        foreach (var (varKey, varVal) in rctx.PageContext.TemplateContextVariables)
        {
            if (!rctx.DataObject.ContainsKey(varKey))
            {
                rctx.DataObject[varKey] = varVal!;
            }
        }

        if (isCache && dCopy is not null)
        {
            var dResult = SnapshotData(rctx.DataObject);

            IEnumerable<KeyValuePair<string, object>> diff = dCopy.Except(dResult).Concat(dResult.Except(dCopy));

            var tsCache = DataQueryBodyParser.ParseTimespan(cache ?? "10m");
            memoryCache?.Set(cacheKey, diff, tsCache ?? TimeSpan.FromMinutes(5));
        }

        return null;
    }

    static Dictionary<string, object> SnapshotData(ScriptObject dataObject)
        => dataObject.ToDictionary(entry => entry.Key, entry => entry.Value);

    //=========================================================
    // help: {{ help }} — список зарегистрированных сайт-функций
    //=========================================================
    public static object? Help(TemplateContext tctx)
    {
        var rctx = ScribanRenderContext.From(tctx);

        var names = rctx.Functions.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("<ul class=\"mb-0\">");
        foreach (var name in names)
        {
            sb.Append("<li><code>").Append(HttpUtility.HtmlEncode(name)).Append("</code></li>");
        }
        sb.Append("</ul>");

        return sb.ToString();
    }

    //=========================================================
    // iff: {{ if iff "x > 2" }}...{{ end }}
    //=========================================================
    public static object? Iff(TemplateContext tctx, string expression)
    {
        var rctx = ScribanRenderContext.From(tctx);

        try
        {
            var ppt = new XInterpreter(rctx.PageContext);
            var par = ppt.GetParameters();
            return ppt.Get.Eval<bool>(expression, par);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("iff: " + ex.Message, ex);
        }
    }

    //=========================================================
    // raw_block: {{ raw_block "block1" }}
    //=========================================================
    public static object? RawBlock(TemplateContext tctx, string blockName)
    {
        var rctx = ScribanRenderContext.From(tctx);

        var template = rctx.WebSiteTemplate
            ?? throw new InvalidOperationException("raw_block: WebSiteTemplate not available in render context");

        var block = template.Parts.FirstOrDefault(s => s.Name == blockName);

        return block?.Content;
    }

    //=========================================================
    // render_post_content: {{ render_post_content post_id }}
    //=========================================================
    public static object? RenderPostContent(TemplateContext tctx, object? arg1)
    {
        var rctx = ScribanRenderContext.From(tctx);

        if (arg1 == null)
            throw new InvalidOperationException("render_post_content: argument is null");

        Guid postId;
        if (arg1 is Guid guid)
        {
            postId = guid;
        }
        else if (arg1 is string st && Guid.TryParse(st, out var parsed))
        {
            postId = parsed;
        }
        else
        {
            throw new InvalidOperationException($"render_post_content: not implement exception. argument=\"{arg1}\" of type '{arg1.GetType()}'.");
        }

        var postService = rctx.ServiceProvider.GetService<IPostService>();
        return (postService.GetDetail(postId, renderContent: true, default)).ConfigureAwait(false).GetAwaiter().GetResult()?.Content;
    }

    //=========================================================
    // site_head / site_footer: {{ site_head }}
    //=========================================================
    public static object? SiteHead(TemplateContext tctx)
    {
        var rctx = ScribanRenderContext.From(tctx);
        var builder = rctx.ServiceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
        return builder.HeadScriptsRender();
    }

    public static object? SiteFooter(TemplateContext tctx)
    {
        var rctx = ScribanRenderContext.From(tctx);
        var builder = rctx.ServiceProvider.GetRequiredKeyedService<ISiteScriptsBuilder>(AppFrontConstants.SiteScriptsBuilderKey);
        return builder.FooterScriptsRender();
    }

    //=========================================================
    // text/html
    //=========================================================
    public static object? TextExcerpt(TemplateContext tctx, string? text, int count = 100)
    {
        if (string.IsNullOrEmpty(text)) return null;
        return text.StripHTML()?.TextEllipsis(count);
    }

    public static object? TextEllipsis(TemplateContext tctx, string? text, int count = 100)
    {
        if (string.IsNullOrEmpty(text)) return null;
        return text.TextEllipsis(count);
    }

    public static object? Nl2Br(TemplateContext tctx, string? text)
        => text?.ReplaceLineEndings("<br>");

    public static object? YoutubeId(TemplateContext tctx, string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var match = Regex.Match(url, @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?|watch)\/|.*[?&amp;]v=)|youtu\.be\/)([\w-]{11})");

        return match.Success ? match.Groups[1].Value : url;
    }

    public static object? StripHtml(TemplateContext tctx, string? html)
    {
        if (string.IsNullOrEmpty(html)) return null;
        return html.StripHTML();
    }

    public static object? Encode(TemplateContext tctx, string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        return HttpUtility.HtmlEncode(text);
    }

    public static object? ToJson(TemplateContext tctx, object? value)
        => JsonSerializer.Serialize(value, _jsonSerializerOptions);

    public static object? ToHumanizedSize(TemplateContext tctx, object? value)
    {
        if (value is long longSize) return longSize.ToHumanizedSize();
        if (value is int intSize) return intSize.ToHumanizedSize();
        if (long.TryParse(value?.ToString(), out long size)) return size.ToHumanizedSize();
        return "";
    }

    //=========================================================
    // dates
    //=========================================================
    public static object? DateFormat(TemplateContext tctx, object? value, string? format = null)
    {
        if (value is null) return null;

        if (value is string st && st == "now")
            value = DateTime.Now;
        else if (value is string str && DateTime.TryParse(str, out var parsedDate))
            value = parsedDate;

        if (value is DateTime dateTime)
            return dateTime.ToString(format);
        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.ToString(format);

        return value.ToString();
    }

    public static object? ParseDateAndFormat(TemplateContext tctx, string? value, string parseFormat, string? outFormat)
    {
        if (string.IsNullOrEmpty(value)) return null;

        DateTime date = DateTime.ParseExact(value, parseFormat, null);
        return date.ToString(outFormat);
    }
}
