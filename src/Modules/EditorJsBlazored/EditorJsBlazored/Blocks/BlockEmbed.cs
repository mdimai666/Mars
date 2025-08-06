using System.Text.RegularExpressions;

namespace EditorJsBlazored.Blocks;

public class BlockEmbed : IEditorJsBlock
{
    public string Service { get; set; } = "";
    public string Source { get; set; } = "";
    public string Embed { get; set; } = "";
    public float? Height { get; set; }
    public float? Width { get; set; }
    public string Caption { get; set; } = "";

    public string GetHtml()
    {
        var (html, embedUrl, id) = Parse(this);
        return html;
    }

    class SParser
    {
        public Regex regex { get; set; } = default!;
        public string embedUrl { get; set; } = default!;
        public string html { get; set; } = default!;
        public int? height { get; set; } = default!;
        public int? width { get; set; } = default!;

        public Func<string[], string?>? id = default!;
    }

    public static (string html, string embedUrl, string? id) Parse(BlockEmbed block)
    {
        //z.Matches

        Dictionary<string, SParser> dict = new()
        {

            ["vimeo"] = new SParser
            {
                regex = new Regex(@"(?:http[s]?:\/\/)?(?:www.)?(?:player.)?vimeo\.co(?:.+\/([^\/]\d+)(?:#t=[\d]+)?s?$)"),
                embedUrl = "https://player.vimeo.com/video/<%= remote_id %>?title=0&byline=0",
                html = @"<iframe frameborder=""0""></iframe>",
                height = 320,
                width = 580
            },
            ["youtube"] = new SParser
            {
                regex = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:(?:youtu\.be\/)|(?:youtube\.com)\/(?:v\/|u\/\w\/|embed\/|watch))(?:(?:\?v=)?([^#&?=]*))?((?:[?&]\w*=\w*)*)"),
                embedUrl = "https://www.youtube.com/embed/<%= remote_id %>",
                html = @"<iframe frameborder=""0"" allowfullscreen></iframe>",
                height = 320,
                width = 580,
                //id = ([s, i]) => {
                //  if (!i && s)
                //    return s;
                //  const r = {
                //    start: "start",
                //    end: "end",
                //    t: "start",
                //    // eslint-disable-next-line camelcase
                //    time_continue: "start",
                //    list: "list"
                //  };
                //  let e = i.slice(1).split("&").map((o) => {
                //    const [l, t] = o.split("=");
                //    return !s && l === "v" ? (s = t, null) : !r[l] || t === "LL" || t.startsWith("RDMM") || t.startsWith("FL") ? null : `${r[l]}=${t}`;
                //  }).filter((o) => !!o);
                //  return s + "?" + e.join("&");
                //}
                id = (matches) =>
                {
                    string s = matches[0];
                    string i = matches[1];

                    if (string.IsNullOrEmpty(i) && !string.IsNullOrEmpty(s))
                    {
                        return s;
                    }

                    Dictionary<string, string> r = new()
                    {
                        { "start", "start" },
                        { "end", "end" },
                        { "t", "start" },
                        { "time_continue", "start" },
                        { "list", "list" }
                    };

                    List<string> e = [];
                    foreach (string o in i.Substring(1).Split('&'))
                    {
                        string[] parts = o.Split('=');
                        string l = parts[0];
                        string t = parts[1];

                        if (string.IsNullOrEmpty(s) && l == "v")
                        {
                            s = t;
                        }
                        else if (!r.ContainsKey(l) || t == "LL" || t.StartsWith("RDMM") || t.StartsWith("FL"))
                        {
                            continue;
                        }
                        else
                        {
                            e.Add($"{r[l]}={t}");
                        }
                    }

                    return s + "?" + string.Join("&", e);
                }
            },
            ["coub"] = new SParser
            {
                regex = new Regex(@"https?:\/\/coub\.com\/view\/([^\/\?\&]+)"),
                embedUrl = "https://coub.com/embed/<%= remote_id %>",
                html = @"<iframe frameborder=""0"" allowfullscreen></iframe>",
                height = 320,
                width = 580
            },
            ["vine"] = new SParser
            {
                regex = new Regex(@"https?:\/\/vine\.co\/v\/([^\/\?\&]+)"),
                embedUrl = "https://vine.co/v/<%= remote_id %>/embed/simple/",
                html = @"<iframe frameborder=""0"" allowfullscreen></iframe>",
                height = 320,
                width = 580
            },
            ["imgur"] = new SParser
            {
                regex = new Regex(@"https?:\/\/(?:i\.)?imgur\.com.*\/([a-zA-Z0-9]+)(?:\.gifv)?"),
                embedUrl = "http://imgur.com/<%= remote_id %>/embed",
                html = @"<iframe allowfullscreen=""true"" scrolling=""no"" id=""imgur-embed-iframe-pub-<%= remote_id %>"" extra-class=""imgur-embed-iframe-pub"" border: 1px solid #000""></iframe>",
                height = 500,
                width = 540
            },
            ["gfycat"] = new SParser
            {
                regex = new Regex(@"https?:\/\/gfycat\.com(?:\/detail)?\/([a-zA-Z]+)"),
                embedUrl = "https://gfycat.com/ifr/<%= remote_id %>",
                html = @"<iframe frameborder='0' scrolling='no' allowfullscreen ></iframe>",
                height = 436,
                width = 580
            },
            ["twitch-channel"] = new SParser
            {
                regex = new Regex(@"https?:\/\/www\.twitch\.tv\/([^\/\?\&]*)\/?$"),
                embedUrl = "https://player.twitch.tv/?channel=<%= remote_id %>",
                html = @"<iframe frameborder=""0"" allowfullscreen=""true"" scrolling=""no""></iframe>",
                height = 366,
                width = 600
            },
            ["twitch-video"] = new SParser
            {
                regex = new Regex(@"https?:\/\/www\.twitch\.tv\/(?:[^\/\?\&]*\/v|videos)\/([0-9]*)"),
                embedUrl = "https://player.twitch.tv/?video=v<%= remote_id %>",
                html = @"<iframe frameborder=""0"" allowfullscreen=""true"" scrolling=""no""></iframe>",
                height = 366,
                width = 600
            },
            ["yandex-music-album"] = new SParser
            {
                regex = new Regex(@"https?:\/\/music\.yandex\.ru\/album\/([0-9]*)\/?$"),
                embedUrl = "https://music.yandex.ru/iframe/#album/<%= remote_id %>/",
                html = @"<iframe frameborder=""0""></iframe>",
                height = 400,
                width = 540
            },
            ["yandex-music-track"] = new SParser
            {
                regex = new Regex(@"https?:\/\/music\.yandex\.ru\/album\/([0-9]*)\/track\/([0-9]*)"),
                embedUrl = "https://music.yandex.ru/iframe/#track/<%= remote_id %>/",
                html = @"<iframe frameborder=""0""></iframe>",
                height = 100,
                width = 540,
                //id = (s) => s.join("/")
                id = (s) => string.Join("/", s)
            },
            ["yandex-music-playlist"] = new SParser
            {
                regex = new Regex(@"https?:\/\/music\.yandex\.ru\/users\/([^\/\?\&]*)\/playlists\/([0-9]*)"),
                embedUrl = "https://music.yandex.ru/iframe/#playlist/<%= remote_id %>/show/cover/description/",
                html = @"<iframe frameborder=""0""></iframe>",
                height = 400,
                width = 540,
                //id = (s) => s.join("/")
                id = (s) => string.Join("/", s)
            },
            ["codepen"] = new SParser
            {
                regex = new Regex(@"https?:\/\/codepen\.io\/([^\/\?\&]*)\/pen\/([^\/\?\&]*)"),
                embedUrl = "https://codepen.io/<%= remote_id %>?height=300&theme-id=0&default-tab=css,result&embed-version=2",
                html = "<iframe scrolling='no' frameborder='no' allowtransparency='true' allowfullscreen='true'></iframe>",
                height = 300,
                width = 600,
                //id = (s) => s.join("/embed/")
                id = (s) => string.Join("/embed/", s)
            },
            ["instagram"] = new SParser
            {
                regex = new Regex(@"^https:\/\/(?:www\.)?instagram\.com\/(?:reel|p)\/(.*)"),
                embedUrl = "https://www.instagram.com/p/<%= remote_id %>/embed",
                html = @"<iframe style=""margin: 0 auto;"" frameborder=""0"" scrolling=""no"" allowtransparency=""true""></iframe>",
                height = 505,
                width = 400,
                //id = (s) => {
                //  var i;
                //  return (i = s == null ? void 0 : s[0]) == null ? void 0 : i.split("/")[0];
                //}
                id = (s) =>
                {
                    if (s == null || s.Length == 0)
                    {
                        return null;
                    }

                    string firstElement = s[0];
                    if (string.IsNullOrEmpty(firstElement))
                    {
                        return null;
                    }

                    return firstElement.Split('/')[0];
                }
            },
            ["twitter"] = new SParser
            {
                regex = new Regex(@"^https?:\/\/(www\.)?(?:twitter\.com|x\.com)\/.+\/status\/(\d+)"),
                embedUrl = "https://platform.twitter.com/embed/Tweet.html?id=<%= remote_id %>",
                html = @"<iframe style=""margin: 0 auto;"" frameborder=""0"" scrolling=""no"" allowtransparency=""true""></iframe>",
                height = 300,
                width = 600,
                //id = (s) => s[1]
                id = (s) => s[1],
            },
            ["pinterest"] = new SParser
            {
                regex = new Regex(@"https?:\/\/([^\/\?\&]*).pinterest.com\/pin\/([^\/\?\&]*)\/?$"),
                embedUrl = "https://assets.pinterest.com/ext/embed.html?id=<%= remote_id %>",
                html = @"<iframe scrolling='no' frameborder='no' allowtransparency='true' allowfullscreen='true' style='width:100%; min-height:400px; max-height:1000px;'></iframe>",
                //id = (s) => s[1]
                id = (s) => s[1],
            },
            ["facebook"] = new SParser
            {
                regex = new Regex(@"https?:\/\/www.facebook.com\/([^\/\?\&]*)\/(.*)"),
                embedUrl = "https://www.facebook.com/plugins/post.php?href=https://www.facebook.com/<%= remote_id %>&width=500",
                html = "<iframe scrolling='no' frameborder='no' allowtransparency='true' allowfullscreen='true' style='width:100%; min-height:500px; max-height:1000px;'></iframe>",
                //id = (s) => s.join("/")
                id = (s) => string.Join("/", s)
            },
            ["aparat"] = new SParser
            {
                regex = new Regex(@"(?:http[s]?:\/\/)?(?:www.)?aparat\.com\/v\/([^\/\?\&]+)\/?"),
                embedUrl = "https://www.aparat.com/video/video/embed/videohash/<%= remote_id %>/vt/frame",
                html = @"<iframe style=""margin: 0 auto;"" frameborder=""0"" scrolling=""no"" allowtransparency=""true""></iframe>",
                height = 300,
                width = 600
            },
            ["miro"] = new SParser
            {
                regex = new Regex(@"https:\/\/miro.com\/\S+(\S{12})\/(\S+)?"),
                embedUrl = "https://miro.com/app/live-embed/<%= remote_id %>",
                html = @"<iframe width=""700"" height=""500"" style=""margin: 0 auto;"" allowFullScreen frameBorder=""0"" scrolling=""no""></iframe>",
                height = 500,
                width = 700,
            },
            ["github"] = new SParser
            {
                regex = new Regex(@"https?:\/\/gist.github.com\/([^\/\?\&]*)\/([^\/\?\&]*)"),
                embedUrl = @"data:text/html;charset=utf-8,<head><base target=""_blank"" /></head><body><script src=""https://gist.github.com/<%= remote_id %>"" ></script></body>",
                html = @"<iframe frameborder=""0"" style=""margin: 0 auto;""></iframe>",
                height = 350,
                width = 600,
                //    id: (s) => `${s.join("/")}.js`
                id = (s) => $"{string.Join("/", s)}.js"
            }
        };

        if (dict.TryGetValue(block.Service, out var parser))
        {
            var groups = parser.regex.Match(block.Embed).Groups;
            var id = parser.id is null ? groups[1].Value : parser.id(groups.Values.Skip(1).Select(s => s.Value).ToArray());
            var embedUrl = parser.embedUrl.Replace("<%= remote_id %>", id);

            var cssClasses = "editorjs block-embed service-" + block.Service;
            var additionAttributes = "";
            if (block.Width > 0) additionAttributes += $" width=\"{block.Width}\"";
            if (block.Height > 0) additionAttributes += $" height=\"{block.Height}\"";

            var html = parser.html.Insert(7, @$" src=""{embedUrl}"" class=""{cssClasses}"" {additionAttributes}");

            return (html, embedUrl, id);
        }

        Console.Error.WriteLine($"provider '{block.Service}' not implement");
        return ("", "", null);
    }
}
