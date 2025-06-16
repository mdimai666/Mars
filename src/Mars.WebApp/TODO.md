# Markdown File

# CLI
[ ] - Execute scripts
[x] - Mainterance mode change
[ ] - sql execute
[ ] - inject node

# Admin
[ ] - style editor
[ ] - post type list view mode
[.] - PostType Manage page edit
[ ] - Add hotkeys page
[ ] - Privacy page
[ ] - Cookies alert
[ ] - Builder statusbar - App status add app timezone & db timezone

# Options
[ ] - add X-Frame-Options
[ ] - option default user
[x] - option edit as json

# backend
[ ] - fix user delete; now on delete all deepends entity will remove
[x] - Add scheduler
[x] - EventManager topic template
[ ] - Add OpenTelemetry
[.] - dont use one type and create normal controllers like {Type}Request, {Type}Response

[x] - rm AnketaQuestion
[-] - rm Feedback
[x] - rm PpmiComment
[x] - rm PpmiFileEntity

[ ] - on rename postType rename all posts too
[ ] - check post.Slug conflict

[ ] - on remove user check another admin exist
[ ] - on remove user disable cascading delete
[ ] - on resolve empty_user
[ ] - on DEV plugin rebuild restart
[ ] - use https://github.com/microsoft/Power-Fx

[ ] - use VersionToken on save

[ ] - edit node as json
[ ] - on deploy nodes use version token

[ ] - copy IOptional function
[+-] - files split by years

# Core Libs
[.] - rm Mars.Core any depends
[ ] - check requirments of DateOnlyJsonConverter in actione System.Text.Json 

# asp net core
[ ] - read https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-9.0
[ ] - .AddDefaultTokenProviders()
[ ] - add https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-9.0&tabs=visual-studio
[ ] - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn?view=aspnetcore-9.0&tabs=windows
[ ] - add patch https://stackoverflow.com/questions/36767759/using-net-core-web-api-with-jsonpatchdocument

# Scripting
[-] - add Microsoft.DiaSymReader

# Nodes
[x] - Excel node
[ ] - add Env support to variables get
[ ] - add commands like Inject for "Power Toys: Commant Palette Util"

## Entity upgrade plan
[x] - Setup 'Modified' -> DateTimeOffset? ModifiedAt
[x] - Setup DateTimeOffset? 'DeletedAt'
[ ] - 

# Features
[ ] - Add AI functions https://devblogs.microsoft.com/dotnet/local-ai-models-with-dotnet-aspire/
[ ] - NodeJs Plugin (Vite, Vue, React from admin)  https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-aspire-apps-with-nodejs
