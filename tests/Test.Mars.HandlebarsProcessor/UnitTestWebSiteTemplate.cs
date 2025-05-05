using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Shared.WebSite.SourceProviders;
using Test.Mars.Host.TestHostApp;

namespace Test.Mars.HandlebarsProcessor;

public class UnitTestWebSiteTemplate : UnitTestHostBaseClass
{
    [Fact]
    public void TestCreate()
    {
        string path = @"C:\Users\D\Documents\VisualStudio\2023\100web\test_Mars_templator\site\public";
        //IWebFilesService webFilesService = _serviceProvider.GetRequiredService<IWebFilesService>();

        WebFilesReadFilesystemService wfs = new WebFilesReadFilesystemService();
        WebTemplateFilesystemSource templateSource = new WebTemplateFilesystemSource(path, wfs);
        WebSiteTemplate t = new WebSiteTemplate(templateSource.ReadParts());
        //t.ScanSite();
    }

    [Fact]
    public void TestLocalizerLoad()
    {

#pragma warning disable CS0219 // Variable is assigned but its value is never used
        string stringKey = "Dima";
        string exceptValue = "Дима";
#pragma warning restore CS0219 // Variable is assigned but its value is never used

        string localizeResurcesDir = @"C:\Users\D\Documents\VisualStudio\2023\medpost_website\Resources";
        string localizeResurcesDirFileName = Path.Combine(localizeResurcesDir, "AppRes.resx");
        //LocalizerXmlResLoaderFactory f = new LocalizerXmlResLoaderFactory(localizeResurcesDirFileName);

        //IStringLocalizer _localizer = f.GetLocalizer("ru");

        //Assert.Equal(exceptValue, _localizer[stringKey]);

        throw new NotImplementedException();
    }
}
