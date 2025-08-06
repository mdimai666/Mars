using System.Xml;
using System.Xml.Linq;
//using Microsoft.Build.Evaluation;

namespace Mars.Plugin;

public static class NuspecHelper
{
    public static string CreateNuspec(NuspecManifest manifest, string minMarsVersion)
    {
        string AddElementIfNotNull(string tagName, string? value, string additionalAttributes = "")
        {
            var attr = string.IsNullOrWhiteSpace(additionalAttributes) ? "" : $" {additionalAttributes.Trim()}";
            return !string.IsNullOrWhiteSpace(value) ? $"<{tagName}{attr}>{System.Security.SecurityElement.Escape(value!)}</{tagName}>" : "";
        }

        string AddStringIfNotNull(string stringLine, string? value)
        {
            return !string.IsNullOrWhiteSpace(value) ? stringLine : "";
        }

        // Формируем зависимости
        var dependenciesXml = "";
        foreach (var group in manifest.Dependencies)
        {
            dependenciesXml += $"<group targetFramework=\"{group.TargetFramework}\">\n";

            foreach (var dep in group.Dependencies)
            {
                var version = string.IsNullOrWhiteSpace(dep.Version) ? minMarsVersion : dep.Version;

                var excludeAttr = string.IsNullOrWhiteSpace(dep.Exclude) ? "" : $" exclude=\"{dep.Exclude}\"";

                dependenciesXml += $"\t\t\t<dependency id=\"{dep.Id}\" version=\"{version}\"{excludeAttr} />\n";
            }

            dependenciesXml += "\t\t</group>\n";
        }

        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                  <metadata>
                    <id>{System.Security.SecurityElement.Escape(manifest.PackageId)}</id>
                    <version>{System.Security.SecurityElement.Escape(manifest.Version)}</version>
                    {AddElementIfNotNull("authors", manifest.Authors)}
                    {AddElementIfNotNull("license", manifest.License, "type=\"expression\"")}
                    {AddElementIfNotNull("licenseUrl", manifest.LicenseUrl)}
                    {AddElementIfNotNull("icon", manifest.Icon)}
                    {AddElementIfNotNull("projectUrl", manifest.ProjectUrl)}
                    {AddElementIfNotNull("description", manifest.Description)}
                    {AddElementIfNotNull("tags", manifest.Tags)}
                    {AddStringIfNotNull($"<repository type=\"{manifest.Repository?.Type}\" url=\"{manifest.Repository?.Url}\" branch\"{manifest.Repository?.Branch}\" commit=\"{manifest.Repository?.Commit}\" />", manifest.Repository?.Url)}
                    <dependencies>
                        {dependenciesXml.Trim()}
                    </dependencies>
                    <frameworkReferences>
                      <group targetFramework="net9.0">
                        <frameworkReference name="Microsoft.AspNetCore.App" />
                      </group>
                    </frameworkReferences>
                  </metadata>
                </package>
                """;
    }

    /// <summary>
    /// ReadFromFileContent
    /// </summary>
    /// <param name="fileXmlContent">xml content</param>
    /// <exception cref="XmlException" />
    public static NuspecManifest ReadFromFileContent(string fileXmlContent)
    {
        var doc = XDocument.Parse(fileXmlContent);
        var ns = doc.Root!.GetDefaultNamespace();
        var metadata = doc.Root.Element(ns + "metadata");

        var manifest = new NuspecManifest
        {
            PackageId = metadata?.Element(ns + "id")?.Value ?? "",
            Version = metadata?.Element(ns + "version")?.Value ?? "",
            Authors = metadata?.Element(ns + "authors")?.Value,
            License = metadata?.Element(ns + "license")?.Value,
            LicenseUrl = metadata?.Element(ns + "licenseUrl")?.Value,
            Icon = metadata?.Element(ns + "icon")?.Value,
            ProjectUrl = metadata?.Element(ns + "projectUrl")?.Value,
            Description = metadata?.Element(ns + "description")?.Value,
            Tags = metadata?.Element(ns + "tags")?.Value ?? "",
            Repository = ReadRepository(metadata, ns),
            Dependencies = ReadDependencies(metadata, ns)
        };

        return manifest;
    }

    private static NuspecRepository? ReadRepository(XElement? metadata, XNamespace ns)
    {
        var repo = metadata?.Element(ns + "repository");
        if (repo == null) return null;

        return new NuspecRepository
        {
            Type = repo.Attribute("type")?.Value ?? "git",
            Url = repo.Attribute("url")?.Value ?? "",
            Commit = repo.Attribute("commit")?.Value ?? "",
            Branch = repo.Attribute("branch")?.Value ?? ""
        };
    }

    private static List<NuspecDependencyGroup> ReadDependencies(XElement? metadata, XNamespace ns)
    {
        var result = new List<NuspecDependencyGroup>();
        var groups = metadata?.Element(ns + "dependencies")?.Elements(ns + "group");
        if (groups == null) return result;

        foreach (var group in groups)
        {
            var framework = group.Attribute("targetFramework")?.Value ?? "";
            var deps = group.Elements(ns + "dependency")
                .Select(d => new NuspecDependency
                {
                    Id = d.Attribute("id")?.Value ?? "",
                    Version = d.Attribute("version")?.Value ?? "",
                    Exclude = d.Attribute("exclude")?.Value
                }).ToList();

            result.Add(new NuspecDependencyGroup
            {
                TargetFramework = framework,
                Dependencies = deps
            });
        }

        return result;
    }

    //public static NuspecManifest LoadFromCsproj(string csprojPath)
    //{
    //    var project = new Project(csprojPath);

    //    var manifest = new NuspecManifest();

    //    // Читаем свойства из PropertyGroup
    //    manifest.PackageId = project.GetPropertyValue("PackageId");
    //    if (string.IsNullOrWhiteSpace(manifest.PackageId))
    //        manifest.PackageId = project.GetPropertyValue("AssemblyName"); // fallback

    //    manifest.Version = project.GetPropertyValue("Version");
    //    manifest.Authors = project.GetPropertyValue("Authors");
    //    manifest.License = project.GetPropertyValue("PackageLicenseExpression");
    //    manifest.LicenseUrl = project.GetPropertyValue("PackageLicenseUrl");
    //    manifest.Icon = project.GetPropertyValue("PackageIcon");
    //    manifest.ProjectUrl = project.GetPropertyValue("PackageProjectUrl");
    //    manifest.Description = project.GetPropertyValue("Description");
    //    manifest.Tags = project.GetPropertyValue("PackageTags");

    //    // Читаем repository info (если есть)
    //    var repoType = project.GetPropertyValue("RepositoryType");
    //    var repoUrl = project.GetPropertyValue("RepositoryUrl");
    //    var repoCommit = project.GetPropertyValue("RepositoryCommit");

    //    if (!string.IsNullOrWhiteSpace(repoUrl))
    //    {
    //        manifest.Repository = new NuspecRepository
    //        {
    //            Type = string.IsNullOrWhiteSpace(repoType) ? "git" : repoType,
    //            Url = repoUrl,
    //            Commit = repoCommit ?? ""
    //        };
    //    }

    //    // Читаем зависимости из ItemGroup -> PackageReference
    //    var dependencies = new List<NuspecDependencyGroup>();
    //    var doc = XDocument.Load(csprojPath);
    //    var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

    //    var groups = new Dictionary<string, NuspecDependencyGroup>();

    //    foreach (var pr in doc.Descendants(ns + "PackageReference"))
    //    {
    //        var version = pr.Attribute("Version")?.Value ?? pr.Element(ns + "Version")?.Value ?? "";
    //        var id = pr.Attribute("Include")?.Value ?? pr.Element(ns + "Include")?.Value ?? "";
    //        var targetFramework = pr.Parent?.Attribute("Condition")?.Value ?? "any";

    //        // Пример: Condition=" '$(TargetFramework)' == 'net9.0' "
    //        string tf = "any";
    //        if (!string.IsNullOrWhiteSpace(targetFramework))
    //        {
    //            var start = targetFramework.IndexOf("==");
    //            if (start >= 0)
    //            {
    //                var valPart = targetFramework.Substring(start + 2).Trim().Trim('\'', '"');
    //                if (!string.IsNullOrWhiteSpace(valPart))
    //                    tf = valPart;
    //            }
    //        }

    //        if (!groups.TryGetValue(tf, out var group))
    //        {
    //            group = new NuspecDependencyGroup { TargetFramework = tf };
    //            groups[tf] = group;
    //        }

    //        if (!string.IsNullOrWhiteSpace(id))
    //        {
    //            group.Dependencies.Add(new NuspecDependency
    //            {
    //                Id = id,
    //                Version = version
    //            });
    //        }
    //    }

    //    manifest.Dependencies = groups.Values.ToList();

    //    return manifest;
    //}
}

public class NuspecManifest
{
    public string PackageId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// example: "MyPlugin.dll"
    /// </summary>
    public string? Authors { get; set; } = string.Empty;
    public string? License { get; set; } = string.Empty;
    public string? LicenseUrl { get; set; } = string.Empty;
    public string? Icon { get; set; } = string.Empty;
    public string? ProjectUrl { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;

    public NuspecRepository? Repository { get; set; }
    public List<NuspecDependencyGroup> Dependencies { get; set; } = [];
}

public class NuspecDependencyGroup
{
    public string TargetFramework { get; set; } = string.Empty;
    public List<NuspecDependency> Dependencies { get; set; } = [];
}

public class NuspecDependency
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Exclude { get; set; } = string.Empty;
}

public class NuspecRepository
{
    public string Type { get; set; } = "git";
    public string Url { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
}
