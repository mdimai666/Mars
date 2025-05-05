using System.Collections;
using System.Globalization;
using System.Resources.NetStandard;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Localizer;
using Microsoft.Extensions.Localization;

namespace Mars.Localizer.Services;

public class LocalizerXmlResLoaderFactory : IAppFrontLocalizer
{
    Dictionary<string, LocalizerFileNode> nodes = new();

    LocalizerFileNode mainNode = default!;

    public LocalizerXmlResLoaderFactory(string mainResxFilePath)
    {
        Load(mainResxFilePath);
    }

    void Load(string mainResxFilePath)
    {
        var dotExt = Path.GetExtension(mainResxFilePath);

        var fileName = Path.GetFileNameWithoutExtension(mainResxFilePath);

        var dir = Path.GetDirectoryName(mainResxFilePath)!;

        var files = Directory.EnumerateFiles(dir, fileName + ".*" + dotExt);



        var mainLocalizer = CreateLocalizer(mainResxFilePath);

        nodes.Clear();
        mainNode = new LocalizerFileNode(fileName, "", mainResxFilePath, mainLocalizer);

        foreach (var file in files)
        {
            string _fileName = Path.GetFileNameWithoutExtension(file);
            string localeString = _fileName.Replace(fileName + ".", "");

            var localizer = CreateLocalizer(file);
            var ci = new CultureInfo(localeString);
            var node = new LocalizerFileNode(_fileName, ci.TwoLetterISOLanguageName, file, localizer);

            nodes.Add(ci.TwoLetterISOLanguageName, node);
        }
    }

    DictLocalizer CreateLocalizer(string localeFilePath)
    {
        ResXResourceReader rsxr = new ResXResourceReader(localeFilePath);
        Dictionary<string, string> dict = new();
        foreach (DictionaryEntry d in rsxr)
        {
            //Console.WriteLine(d.Key.ToString() + ":\t" + d.Value.ToString());
            dict.Add(d.Key.ToString()!, d.Value.ToString()!);
        }

        rsxr.Close();

        DictLocalizer res = new DictLocalizer(dict);

        return res;
    }

    class LocalizerFileNode
    {
        public string FileName { get; set; }
        public string Locale { get; set; }
        public string Path { get; set; }
        public CultureInfo CultureInfo { get; set; }

        public IStringLocalizer Localizer { get; set; }

        public LocalizerFileNode(string fileName, string locale, string path, IStringLocalizer localizer)
        {
            FileName = fileName;
            Locale = locale;
            Path = path;
            Localizer = localizer;
            CultureInfo = new CultureInfo(locale);
        }
    }

    public IStringLocalizer GetLocalizer(string? locale = null)
    {
        locale ??= Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        if (nodes.ContainsKey(locale))
        {
            return nodes[locale].Localizer;
        }

        return mainNode.Localizer;
    }

    public void Refresh()
    {
        Load(mainNode.Path);
    }
}
