using Flurl.Http;
using Mars.Admin;
using Mars.Admin.Components;
using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Components.Forms.Editors;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Admin.Framework.Interfaces;
using Mars.Admin.Pages.PostsViews.Forms;
using Mars.Admin.Startups;
using Mars.AiChat.Front;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Datasource.Front;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Mars.Nodes.Workspace;
using Mars.Plugin.Front;
using Mars.SemanticKernel.Front;
using Mars.WebApp.Nodes.Front;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Toolbelt.Blazor.Extensions.DependencyInjection;

//Info: Для быстрой разработки через dotnet watch запускайте Dev/DevAdmin.DevServer

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Error);

var logger = builder.Logging.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
logger.LogTrace("=== Application startup begin ===");

string? backendUrl = builder.Configuration["BackendUrl"];
if (string.IsNullOrEmpty(backendUrl))
{
    backendUrl = builder.HostEnvironment.BaseAddress.TrimEnd('/').Replace("/dev", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
    if (backendUrl == "http://localhost:5185") backendUrl = "http://localhost:5003";
    logger.LogTrace("BackendUrl from BaseAddress: {BackendUrl}", backendUrl);
    Q.BackendUrl = backendUrl;
}

builder.ConfigureAppLanguage();

var httpClient = new HttpClient() { BaseAddress = new Uri(backendUrl) };
builder.Services.AddScoped(sp => httpClient.EnableIntercept(sp));
builder.Services.AddScoped<IFlurlClient>(sp => new FlurlClient(httpClient));

builder.Services.AddHttpClientInterceptor();

var safeMode = await builder.DetectIsSafeMode(logger);

builder.Services.AddMarsAdminFramework(builder.Configuration, typeof(Program));

// формы аргументов XAction: заменяем null-презентер на диалоги FluentUI
builder.Services.Replace(ServiceDescriptor.Scoped<Mars.Admin.Framework.Services.IXActionFormPresenter, Mars.Admin.Shared.FluentDialogXActionFormPresenter>());

builder.Services.AddScoped<Mars.Admin.Shared.ActionCenter.ActionCenterService>();
builder.Services.AddScoped<Mars.Admin.Shared.ActionCenter.RecentPagesService>();

#if DEBUG
builder.Services.AddScoped<Mars.Admin.Framework.Services.IFrontActionRunner, Mars.Admin.Shared.FrontDemoActionRunner>();
#endif

Q.SetupHostingInfo(new BackendHostingInfo { Backend = new Uri(Q.BackendUrl) });
CodeEditor2.ToolbarComponents.Add(typeof(CodeEditorExtraToolbar));
ContentWrapper.GeneralSectionActions = typeof(Mars.Admin.Shared.GeneralSectionActions);

// редакторы значений — общие для всех полей: текстовые (в т.ч. тяжёлые WYSIWYG/код/блочный),
// строковые и даты. Название делает редактор предлагаемым в выборе редактора поля; безымянные
// регистрации (обёртки провайдеров, встроенные дефолты) доступны только явным ключом дескриптора
FormEditorLocator.Register(MetaFieldEditorCatalog.Wysiwyg, typeof(FormWysiwygEditor), false, "WYSIWYG (Quill)",
    FormFieldType.String, FormFieldType.Text);
FormEditorLocator.Register(MetaFieldEditorCatalog.Code, typeof(FormCodeEditor), false, "Код (Monaco)",
    FormFieldType.String, FormFieldType.Text);
FormEditorLocator.Register(MetaFieldEditorCatalog.BlockEditor, typeof(FormBlockEditor), false, "Блочный (Editor.js)",
    FormFieldType.String, FormFieldType.Text);
FormEditorLocator.Register(MetaFieldEditorCatalog.Color, typeof(FormColorEditor), false, "Цвет", FormFieldType.String);
FormEditorLocator.Register(MetaFieldEditorCatalog.Url, typeof(FormUrlEditor), false, "URL-адрес", FormFieldType.String);
FormEditorLocator.Register(MetaFieldEditorCatalog.Email, typeof(FormEmailEditor), false, "Email", FormFieldType.String);
FormEditorLocator.Register(MetaFieldEditorCatalog.Time, typeof(FormTimeEditor), false, "Время", FormFieldType.DateTime);
FormEditorLocator.Register(MetaFieldEditorCatalog.DateTime, typeof(FormDateTimeEditor), false, "Дата и время",
    FormFieldType.DateTime);

// доменный редактор системного слота формы поста (общий слой Mars.Forms): пикер категорий
// привязан к типу поста, поэтому остаётся редактором провайдера
FormEditorLocator.Register(PostFormEditors.Categories, typeof(PostCategoriesEditor), true, FormFieldType.Relation);

// общие редакторы слотов: теги и отображение значения строкой годятся любому провайдеру
FormEditorLocator.Register(FormEditorCatalog.Tags, typeof(FormTagsEditor), true, "Теги", FormFieldType.String);
FormEditorLocator.Register(FormEditorCatalog.TextDisplay, typeof(FormTextDisplayEditor), false,
    "Текст только для чтения", FormFieldType.String, FormFieldType.Relation);

// доменные панели настроек метаполей в общем редакторе определений (скоуп meta, все типы)
FormFieldTypeSettingsLocator.Register(MetaFieldSettingsPanel.Scope, typeof(MetaFieldSettingsPanel));

// доменные настройки системных слотов типа поста (язык кода выбранного редактора)
FormFieldTypeSettingsLocator.Register(SystemFieldSettingsPanel.Scope, typeof(SystemFieldSettingsPanel));

// доменные редакторы метаполей: у связей и медиа нет типизированного CLR-значения, поэтому
// их строки правят собственные компоненты (остальные значения метаполей — общие редакторы выше)
FormEditorLocator.Register(MetaFormEditors.Relation, typeof(MetaValueRelationEditor), false, FormFieldType.Relation);
FormEditorLocator.Register(MetaFormEditors.RelationMulti, typeof(MetaValueRelationEditor), true, FormFieldType.Relation);
FormEditorLocator.Register(MetaFormEditors.File, typeof(MetaValueFileEditor), false, FormFieldType.File, FormFieldType.Image);
FormEditorLocator.Register(MetaFormEditors.FileMulti, typeof(MetaValueFileEditor), true, FormFieldType.File, FormFieldType.Image);

logger.LogTrace("Adding workspace services...");
builder.Services.AddHotKeys2();
builder.Services.AddNodeWorkspace()
                .AddMarsWebAppNodesFront()
                .AddDatasourceWorkspace()
                .AddSemanticKernelFront()
                .AddAiChatFront()
                .AddMarsFormsFront();

builder.ConfigureWebSockets(backendUrl);

if (safeMode)
{
    logger.LogTrace("Loading remote plugin assemblies disabled on safe mode");
}
else
{
    logger.LogTrace("Loading remote plugin assemblies...");
    await builder.AddRemotePluginAssemblies(Q.BackendUrl, logger);
}

logger.LogTrace("Building application...");
var app = builder.Build();

logger.LogTrace("Initializing services...");
app.Services.UseMarsAdminFramework()
            .UseNodeWorkspace()
            .UseMarsWebAppNodesFront()
            .UseDatasourceWorkspace()
            .UseSemanticKernelFront()
            .UseAiChatFront()
            .UseMarsFormsFront();

// кастомные формы аргументов XAction (перекрывают генерик-форму по схеме)
app.Services.GetRequiredService<Mars.Admin.Framework.Services.IXActionFormProvider>()
            .Register(Mars.Admin.Shared.RegenerateGeneratedMetaValuesForm.CommandId, typeof(Mars.Admin.Shared.RegenerateGeneratedMetaValuesForm));

SmartSaveExtensions.Setup(app.Services.GetRequiredService<IMessageService>());

if (!safeMode)
{
    logger.LogTrace("Using remote plugin assemblies...");
    app.UseRemotePluginAssemblies();
}

logger.LogTrace("=== Application startup complete, running... ===");
await app.RunAsync();
