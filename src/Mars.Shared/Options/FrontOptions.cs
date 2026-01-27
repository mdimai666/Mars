namespace Mars.Shared.Options;

public class FrontOptions
{
    public List<FrontOptionHostItem> HostItems { get; set; } = [new()];
}

public class FrontOptionHostItem
{
    string _url = "";
    public string Url { get => _url; set => _url = value.ToLower().TrimEnd('/'); }
    public string HostHtml { get; set; } = "@Body";
}

public static class FrontOptionsHelper
{

    public const string Bootstrap5HandlebarsTemplate1 = """
        <!DOCTYPE html>
        <html lang="ru">

        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />

            <base href="/" />
            {{{#site_head}}}

            {{#>head}}
            <title>{{{SysOptions.SiteName}}}</title>
            <meta name="description" content="{{{SysOptions.SiteDescription}}}">
            {{/head}}

            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/css/bootstrap.min.css" rel="stylesheet"
                integrity="sha384-LN+7fdVzj6u52u30Kp6M/trliBMCMKTyK833zpbD+pXdCLuTusPj697FH4R/5mcr" crossorigin="anonymous">
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">
        </head>
        <body class="{{bodyClass}}" {{bodyAttrs}}>
            <app id="app" class="d-flex flex-column flex-fill">
                @Body
            </app>

            {{{#site_footer}}}

            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/js/bootstrap.bundle.min.js"
                integrity="sha384-ndDqU0Gzau9qJ1lfW4pNLlhNTkCfHzAVBReH9diLvGRem5+R9g2FzA8ZGN954O5Q"
                crossorigin="anonymous"></script>
        </body>
        </html>
        """;

    public const string Bootstrap5HandlebarsFullTemplate1 = """
        @DefaultLayout null

        <!DOCTYPE html>
        <html lang="ru">

        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />

            <base href="/" />
            {{{#site_head}}}

            {{#>head}}
            <title>{{{SysOptions.SiteName}}}</title>
            <meta name="description" content="{{{SysOptions.SiteDescription}}}">
            {{/head}}

            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/css/bootstrap.min.css" rel="stylesheet"
                integrity="sha384-LN+7fdVzj6u52u30Kp6M/trliBMCMKTyK833zpbD+pXdCLuTusPj697FH4R/5mcr" crossorigin="anonymous">
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css">
        </head>
        <body class="{{bodyClass}}" {{bodyAttrs}}>
            <app id="app" class="d-flex flex-column flex-fill">
                {{>header1}}
                @Body

                {{#if $user.isadmin}}
                    <div class="errors-container">
                    {{#each $errors}}
                        <div class="alert alert-danger">
                            <pre>{{.}}</pre>
                        </div>
                    {{/each}}
                    </div>
                {{/if}}
            </app>
            {{{#site_footer}}}

            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/js/bootstrap.bundle.min.js"
                integrity="sha384-ndDqU0Gzau9qJ1lfW4pNLlhNTkCfHzAVBReH9diLvGRem5+R9g2FzA8ZGN954O5Q"
                crossorigin="anonymous"></script>
        </body>
        </html>
        """;
}
