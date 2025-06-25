using Mars.Host.Shared.WebSite.Models;

namespace Mars.WebSiteProcessor.Handlebars.Parsers;

public static class HandlebarsContextHelperFunctionBodyParser
{
    public static DataQueryRequest FunctionBodyParse(string rows, string? key = null)
    {
        key ??= Guid.NewGuid().ToString();
        var arr = ParseBodyStringToKeyValuePairs(rows);

        DataQueryRequest dataQuery = new()
        {
            Key = key,
            Queries = arr
        };

        var dict = new Dictionary<string, string>(arr.Count(s => s.Key != "_"));

        foreach (var item in arr.Where(s => s.Key != "_"))
        {
            dict[item.Key] = item.Value;
        }

        return dataQuery;
    }

    #region Tools
    static List<KeyValuePair<string, string>> ParseBodyStringToKeyValuePairs(string body)
    {
        var lines = body.Split('\n').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s) && !s.StartsWith("//") && s.Contains('='));

        List<KeyValuePair<string, string>> list = [];

        foreach (var line in lines)
        {
            var sp = line.Split('=', 2);
            if (sp.Length != 2) throw new Exception($"cannot parse #context row '{line}', row must have '=';");

            list.Add(new(sp[0].Trim(), sp[1].Trim()));
        }

        return list;
    }
    #endregion

}
