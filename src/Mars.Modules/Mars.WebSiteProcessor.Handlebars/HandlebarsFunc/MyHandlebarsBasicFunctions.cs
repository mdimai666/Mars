using System.Text.RegularExpressions;
using System.Web;
using Mars.Host.Shared.Templators;
using Mars.Core.Extensions;
using HandlebarsDotNet;
//using Humanizer;
using Newtonsoft.Json;

namespace Mars.Host.Templators.HandlebarsFunc;

public static class MyHandlebarsBasicFunctions
{
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

    public static void EqualStringBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#eq}} helper must have exactly two arguments");
        }

        //var left = arguments.At<string>(0);
        //var right = arguments[1] as string;
        var left = arguments[0].ToString();
        var right = arguments[1].ToString();
        if (left?.Equals(right) ?? false) options.Template(output, context);
        else options.Inverse(output, context);
    }

    public static void NotEqualStringBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length != 2)
        {
            throw new HandlebarsException("{{#neq}} helper must have exactly two arguments");
        }

        //var left = arguments.At<string>(0);
        //var right = arguments[1] as string;
        var left = arguments[0].ToString();
        var right = arguments[1].ToString();
        if (left?.Equals(right) ?? false == false) options.Template(output, context);
        else options.Inverse(output, context);
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



    public static void DateFormatHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 2)
        {
            throw new HandlebarsException("{{dateFormat <DateTime> \"yyyy-MM-dd HH:mm\"}} helper must have exactly 2 arguments");
        }

        var left = args[0];
        if (left.ToString() == "undefined")
        {
            output.WriteSafeString(left);
            return;
        }
        var right = args[1] as string;

        if (left is not DateTime && left.ToString() == "now")
            left = DateTime.Now;

        if (left is not DateTime dateTime) return;
        //throw new HandlebarsException("supposed to be DateTime");

        var formatString = right;

        output.WriteSafeString(dateTime.ToString(formatString));
    }

    public static void DateHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{date <DateTime>}} helper must have exactly 1 arguments");
        }

        var left = args[0];

        if (left is not DateTime dateTime)
            throw new ArgumentException("supposed to be DateTime");


        output.WriteSafeString(dateTime.ToShortDateString());
    }

    //TODO: uncomment
    //public static void DateRelativeHelper(EncodedTextWriter output, Context context, Arguments args)
    //{
    //    if (args.Length != 1)
    //    {
    //        throw new HandlebarsException("{{#date_relative <DateTime>}} helper must have exactly 1 arguments");
    //    }

    //    var left = args[0];

    //    if (left is not DateTime dateTime)
    //        throw new ArgumentException("supposed to be DateTime");


    //    output.WriteSafeString((dateTime).Humanize(utcDate: false));
    //}

    public static void ParseDateandFormatHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 3)
        {
            throw new HandlebarsException("{{parsedateandformat <string> <parseformat> <outformat>}} helper must have exactly 3 arguments");
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

    public static void nl2br_Helper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#nl2br \"text\"}} helper must have exactly 1 arguments");
        }

        var text = args[0] as string;
        output.WriteSafeString(text?.ReplaceLineEndings("<br>"));
    }


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

    public static void ToJsonHelper(EncodedTextWriter output, Context context, Arguments args)
    {
        if (args.Length != 1)
        {
            throw new HandlebarsException("{{#asjson \"object\"}} helper must have exactly 1 arguments");
        }

        var obj = args[0];

        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);

        output.WriteSafeString(json);
        //return jobj;
    }

    public static void ForLoopBlock(EncodedTextWriter output, BlockHelperOptions options, Context context, Arguments arguments)
    {
        if (arguments.Length < 2 || arguments.Length > 3)
        {
            throw new HandlebarsException("{{#for @start @end @step? }} helper must have exactly 2-3 arguments");
        }

        var start = int.Parse(arguments[0].ToString());
        var end = int.Parse(arguments[1].ToString());
        var step = 1;
        if (arguments.Length == 3)
        {
            step = int.Parse(arguments[2].ToString());
            if (step < 1) throw new HandlebarsException("#for step cannot be less than 1");
        }

        HandlebarsDotNet.Iterators.ArrayIterator<int> arrayIterator = new();

        List<int> list = new List<int>();
        for (int i = start; i <= end; i += step)
        {
            list.Add(i);
        }

        arrayIterator.Iterate(output, options.Frame, context.Properties.ToArray(), list.ToArray(), options.Template, null);
    }

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
}
