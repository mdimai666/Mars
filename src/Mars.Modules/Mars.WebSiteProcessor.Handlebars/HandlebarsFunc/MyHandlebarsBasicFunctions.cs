using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using HandlebarsDotNet;
using Mars.Core.Extensions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.QueryLang;
using Mars.WebSiteProcessor.Handlebars.HandlebarsFunc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Templators.HandlebarsFunc;

public static class MyHandlebarsBasicFunctions
{
    [TemplatorHelperInfo("eq", "{{#eq @left @right}}", "Conditionally renders if left == right.")]
    public static void EqualBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#eq}} helper must have exactly two arguments");
        }

        //var left = arguments.At<string>(0);
        //var right = arguments[1] as string;
        var left = arguments[0];
        var right = arguments[1];
        if (left?.Equals(right) ?? false) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("neq", "{{#neq @left @right}}", "Conditionally renders if left != right.")]
    public static void NotEqualBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#neq}} helper must have exactly two arguments");
        }

        var left = arguments[0];
        var right = arguments[1];

        if (left?.Equals(right) ?? false)
        {
            options.Inverse(output, context);
        }
        else
        {
            options.Template(output, context);
        }
    }

    [TemplatorHelperInfo("gt", "{{#gt @left @right}}", "Conditionally renders if left > right.")]
    public static void GreaterThanBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#gt}} helper must have exactly two arguments");
        }

        int left = (int)arguments[0];
        int right = (int)arguments[1];
        if (left > right) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("gte", "{{#gte @left @right}}", "Conditionally renders if left >= right.")]
    public static void GreaterThanOrEqualBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#gte}} helper must have exactly two arguments");
        }

        int left = (int)arguments[0];
        int right = (int)arguments[1];
        if (left >= right) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("lt", "{{#lt @left @right}}", "Conditionally renders if left < right.")]
    public static void LessThanBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#lt}} helper must have exactly two arguments");
        }

        int left = (int)arguments[0];
        int right = (int)arguments[1];
        if (left < right) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("lte", "{{#lte @left @right}}", "Conditionally renders if left <= right.")]
    public static void LessThanOrEqualBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#lte}} helper must have exactly two arguments");
        }

        int left = (int)arguments[0];
        int right = (int)arguments[1];
        if (left <= right) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("eqstr", "{{#eqstr @left @right}}", "Conditionally renders if left == right.")]
    public static void EqualStringBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#eqstr}} helper must have exactly two arguments");
        }

        //var left = arguments.At<string>(0);
        //var right = arguments[1] as string;
        var left = arguments[0].ToString();
        var right = arguments[1].ToString();
        if (left?.Equals(right) ?? false) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("neqstr", "{{#neqstr @left @right}}", "Conditionally renders if left != right.")]
    public static void NotEqualStringBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#neqstr}} helper must have exactly two arguments");
        }

        //var left = arguments.At<string>(0);
        //var right = arguments[1] as string;
        var left = arguments[0].ToString();
        var right = arguments[1].ToString();
        if (left?.Equals(right) ?? false == false) options.Template(output, context);
        else options.Inverse(output, context);
    }

    [TemplatorHelperInfo("and", "{{#and @args[]}}", "Conditionally renders if all arguments are truthy.")]
    public static void AndBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        foreach (var arg in arguments)
        {
            if (!IsTruthy(arg))
            {
                options.Inverse(output, context);
                return;
            }
        }
        options.Template(output, context);
    }

    [TemplatorHelperInfo("or", "{{#or @args[]}}", "Conditionally renders if any argument is truthy.")]
    public static void OrBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        foreach (var arg in arguments)
        {
            if (IsTruthy(arg))
            {
                options.Template(output, context);
                return;
            }
        }
        options.Inverse(output, context);
    }

    [TemplatorHelperInfo("isEmpty", "{{#isEmpty @value}}", "Conditionally renders if the value is empty (null, empty string, empty collection).")]
    public static void IsEmptyBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 1)
        {
            throw new HandlebarsException("{{#isEmpty}} helper must have exactly one argument");
        }

        var value = arguments[0];
        if (value == null ||
            (value is string s && s.Length == 0) ||
            (value is ICollection c && c.Count == 0) ||
            (value is IEnumerable e && !e.GetEnumerator().MoveNext()))
        {
            options.Template(output, context);
        }
        else
        {
            options.Inverse(output, context);
        }
    }

    [TemplatorHelperInfo("contains", "{{#contains @source @item}}", "Conditionally renders if the source contains the item.")]
    public static void ContainsBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#contains}} helper must have exactly two arguments");
        }

        var source = arguments[0];
        var item = arguments[1];

        bool result = source switch
        {
            string s when item is string sub => s.Contains(sub),
            IEnumerable e => e.Cast<object>().Contains(item),
            _ => false
        };

        if (result)
        {
            options.Template(output, context);
        }
        else
        {
            options.Inverse(output, context);
        }
    }

    private static bool IsTruthy(object value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            ICollection c => c.Count > 0,
            IEnumerable e => e.Cast<object>().Any(),
            int i => i != 0,
            double d => d != 0,
            _ => true
        };
    }

    public static void if_divided_by_Block(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments args)
    {
        if (args.Length < 1 && args.Length > 3)
        {
            throw new HandlebarsException("{{#if_divided_by num count shift?}} helper must have exactly 1-2 arguments");
        }

        int num = 0;
        int div = 0;

        int shift = 0;

        var left = args[0]?.ToString();
        var right = args[1]?.ToString();

        int.TryParse(left, out num);
        int.TryParse(right, out div);
        if (args.Length == 3)
        {
            int.TryParse(args[2]?.ToString(), out shift);
        }

        if (num == 0 || div == 0)
        {
            options.Inverse(output, context);
            return;
        }

        if ((num + shift) % div == 0)
        {
            options.Template(output, context);
        }
        else
        {
            options.Inverse(output, context);
        }
    }

    [TemplatorHelperInfo("dateFormat", "{{#dateFormat @DateTime \"yyyy-MM-dd HH:mm\"}}", "Formats a DateTime or DateTimeOffset according to the specified format string.")]
    public static void DateFormatHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 2)
        {
            throw new HandlebarsException("{{dateFormat @DateTime \"yyyy-MM-dd HH:mm\"}} helper must have exactly 2 arguments");
        }

        var left = args[0];
        if (left.ToString() == "undefined")
        {
            output.WriteSafeString(left);
            return;
        }
        var right = args[1] as string;

        if (left is not DateTime or DateTimeOffset && left.ToString() == "now")
            left = DateTime.Now;

        var formatString = right;

        if (left is DateTime dateTime)
            output.WriteSafeString(dateTime.ToString(formatString));
        else if (left is DateTimeOffset dateTimeOffset)
            output.WriteSafeString(dateTimeOffset.ToString(formatString));
    }

    [TemplatorHelperInfo("date", "{{#date @DateTime}}", "Formats a DateTime or DateTimeOffset to a short date string.")]
    public static void DateHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{date @DateTime}} helper must have exactly 1 arguments");
        }

        var left = args[0];

        if (left is DateTime dateTime)
        {
            output.WriteSafeString(dateTime.ToShortDateString());
        }
        else if (left is DateTimeOffset dateTimeOffset)
        {
            output.WriteSafeString(dateTimeOffset.LocalDateTime);
        }
        else
            throw new ArgumentException("supposed to be DateTime");

    }

    //TODO: uncomment
    //public static void DateRelativeHelper(EncodedTextWriter output, Context context, Arguments args)
    //{
    //    if (args.Length != 1)
    //    {
    //        throw new HandlebarsException("{{#date_relative @DateTime}} helper must have exactly 1 arguments");
    //    }

    //    var left = args[0];

    //    if (left is not DateTime dateTime) add support datetime offset
    //        throw new ArgumentException("supposed to be DateTime");

    //    output.WriteSafeString((dateTime).Humanize(utcDate: false));
    //}

    [TemplatorHelperInfo("parsedateandformat", "{{#parsedateandformat @string @parseformat @outformat}}", "Parse string to date and formats a DateTime or DateTimeOffset to a short date string.")]
    public static void ParseDateandFormatHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 3)
        {
            throw new HandlebarsException("{{parsedateandformat @string @parseformat @outformat>}} helper must have exactly 3 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var left = args[0]?.ToString();
        var parseformat = args[1].ToString();
        var format = args[2].ToString();

        DateTime date = DateTime.ParseExact(left, parseformat, null);

        output.WriteSafeString(date.ToString(format));
    }

    [TemplatorHelperInfo("text_excerpt", "{{#text_excerpt @string @count?}}", "Generates an excerpt from the text, stripping HTML and truncating to the specified character count.")]
    public static void TextExcerptHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length < 1 && args.Length > 2)
        {
            throw new HandlebarsException("{{#text_excerpt \"html text\" count?}} helper must have exactly 1-2 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var left = args[0]?.ToString();

        var formatString = left?.StripHTML();

        int _default = 100;

        int count = _default;
        if (args.Length == 2)
        {
            int.TryParse(args[1]?.ToString(), out count);
        }
        formatString = formatString?.TextEllipsis(count);

        output.WriteSafeString(formatString);
    }

    [TemplatorHelperInfo("text_ellipsis", "{{#text_ellipsis @string @count?}}", "Truncates the text to the specified character count, adding an ellipsis if necessary.")]
    public static void TextEllipsisHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length < 1 && args.Length > 2)
        {
            throw new HandlebarsException("{{#text_ellipsis \"html text\" count?}} helper must have exactly 1-2 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var left = args[0]?.ToString();

        var formatString = left;

        int _default = 100;

        int count = _default;
        if (args.Length == 2)
        {
            int.TryParse(args[1]?.ToString(), out count);
        }
        formatString = formatString.TextEllipsis(count);

        output.WriteSafeString(formatString);
    }

    [TemplatorHelperInfo("nl2br", "{{#nl2br @text}}", "Converts newlines in the text to <br> tags.")]
    public static void nl2br_Helper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#nl2br \"text\"}} helper must have exactly 1 arguments");
        }

        var text = args[0] as string;
        output.WriteSafeString(text?.ReplaceLineEndings("<br>"));
    }

    [TemplatorHelperInfo("youtubeId", "{{#youtubeId @url}}", "Extracts the YouTube video ID from a YouTube URL.")]
    public static void youtubeId_Helper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#youtubeId \"url\"}} helper must have exactly 1 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var url = args[0] as string;

        var match = Regex.Match(url, @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?|watch)\/|.*[?&amp;]v=)|youtu\.be\/)([\w-]{11})");

        if (match.Success)
        {
            output.WriteSafeString(match.Groups[1].Value);
        }
        else
        {
            output.WriteSafeString(url);
        }
    }

    [TemplatorHelperInfo("striphtml", "{{#striphtml @html}}", "Strips HTML tags from the text.")]
    public static void StripHtmlHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#striphtml \"html text\"}} helper must have exactly 1 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var left = args[0]?.ToString();

        var formatString = left?.StripHTML();

        output.WriteSafeString(formatString);
    }

    [TemplatorHelperInfo("encode", "{{#encode @text}}", "Encodes the text for HTML output, escaping special characters.")]
    public static void EncodeHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#encode \"html text\"}} helper must have exactly 1 arguments");
        }

        if (string.IsNullOrEmpty(args[0]?.ToString()))
        {
            return;
        }

        var left = args[0] as string;

        var formatString = HttpUtility.HtmlEncode(left);

        output.WriteSafeString(formatString);
    }

    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [TemplatorHelperInfo("tojson", "{{#tojson @object}}", "Serializes an object to JSON format.")]
    public static void ToJsonHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#tojson \"object\"}} helper must have exactly 1 arguments");
        }

        var obj = args[0];

        var json = JsonSerializer.Serialize(obj, _jsonSerializerOptions);

        output.WriteSafeString(json);
        //return jobj;
    }

    [TemplatorHelperInfo("for", "{{#for @start @end @step?}}", "Iterates from start to end.")]
    public static void ForLoopBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length < 2 || arguments.Length > 3)
        {
            throw new HandlebarsException("{{#for @start @end @step? }} helper must have exactly 2-3 arguments");
        }

        var start = int.Parse(arguments[0].ToString()!);
        var end = int.Parse(arguments[1].ToString()!);
        var step = 1;
        if (arguments.Length == 3)
        {
            step = int.Parse(arguments[2].ToString()!);
            if (step < 1) throw new HandlebarsException("#for step cannot be less than 1");
        }

        HandlebarsDotNet.Iterators.ArrayIterator<int> arrayIterator = new();

        List<int> list = [];
        for (int i = start; i <= end; i += step)
        {
            list.Add(i);
        }

        arrayIterator.Iterate(output, options.Frame, context.Properties.ToArray(), list.ToArray(), options.Template, null);
    }

    [TemplatorHelperInfo("ToHumanizedSize", "{{#ToHumanizedSize @size}}", "Converts a size in bytes to a human-readable format.")]
    public static void ToHumanizedSize(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#ToHumanizedSize \"val\"}} helper must have exactly 1 arguments");
        }

        string outVal;

        if (args[0] is long long_size)
        {
            outVal = long_size.ToHumanizedSize();
        }
        else if (args[0] is int int_size)
        {
            outVal = int_size.ToHumanizedSize();
        }
        else if (long.TryParse(args[0].ToString(), out long size))
        {
            outVal = size.ToHumanizedSize();
        }
        else
        {
            outVal = "";
        }

        output.WriteSafeString(outVal);
    }

    static MyHandlebarsHelpBlock? _helpBlock;

    [TemplatorHelperInfo("help", "{{#help}}", "Displays information about available helpers.")]
    public static void HelpHelper(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments arguments)
    {
        var renderContext = options.Data.RenderContext();
        var tflocator = renderContext.ServiceProvider.GetRequiredService<ITemplatorFeaturesLocator>();
        var mlocator = renderContext.ServiceProvider.GetRequiredService<IMetaModelTypesLocator>();
        var queryLangHelperAvailableMethodsProvider = renderContext.ServiceProvider.GetRequiredService<IQueryLangHelperAvailableMethodsProvider>();

#if DEBUG
        var helpBlock = new MyHandlebarsHelpBlock(options, tflocator, mlocator, queryLangHelperAvailableMethodsProvider);
        helpBlock.WriteTo(output);
#else
        _helpBlock ??= new MyHandlebarsHelpBlock(options, tflocator, mlocator, queryLangHelperAvailableMethodsProvider);
        _helpBlock.WriteTo(output);
#endif
    }
}
