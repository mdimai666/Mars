using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Network;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpInFormSaveFilesNode/HttpInFormSaveFilesNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpInFormSaveFilesNode : Node, INodeOutputValueSpec
{
    public override string TypeId => "core.HttpInFormSaveFilesNode";

    [Required]
    public string FilePathTemplate { get; set; } = "media/{yyyy}/{file_name}";

    public bool SaveInMediaFiles { get; set; }
    public bool AllowSaveFileOutsideUploads { get; set; }

    public HttpInFormSaveFilesNode()
    {
        Color = "#e7e6af";
        Inputs = [new()];
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/http-form-files.svg";
    }

    /// <summary>Пути FileListItem объявлены вручную: тип живёт в Mars.Media.Abstractions, из Core не виден.</summary>
    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        if (!SaveInMediaFiles)
        {
            yield return new OutputValueSpec(nameof(NodeMsg.Payload), "string[]", Description: "saved file paths");
            yield return new OutputValueSpec("Payload[]", "string");
            yield break;
        }

        yield return new OutputValueSpec(nameof(NodeMsg.Payload), VarNode.ObjectTypeName, Description: "saved media files (FileListItem[])");
        yield return new OutputValueSpec("Payload[]", VarNode.ObjectTypeName);
        yield return new OutputValueSpec("Payload[].Id", "Guid");
        yield return new OutputValueSpec("Payload[].Name", "string");
        yield return new OutputValueSpec("Payload[].Ext", "string");
        yield return new OutputValueSpec("Payload[].Size", "long");
        yield return new OutputValueSpec("Payload[].Url", "string");
        yield return new OutputValueSpec("Payload[].UrlRelative", "string");
        yield return new OutputValueSpec("Payload[].IsImage", "bool");
        yield return new OutputValueSpec("Payload[].FilePhysicalPath", "string");
        yield return new OutputValueSpec("Payload[].FileVirtualPath", "string");
    }

    public static IReadOnlyCollection<string> ExampleTemplates =
    [
        "media/{yyyy}/{file_name}=\"media/{yyyy}/{file_name}\"→ media/2026/file.txt",
        "media/{yyyy}/{uniqueFileName}=\"media/{yyyy}/{uniqueFileName}\"→ media/2026/file_20260529_206c1bc9.txt",
        "media/{yyyy}/{MM}/{file_name}=\"media/{yyyy}/{MM}/{file_name}\"→ media/2026/05/avatar.png",
        "{yy}-{MM}-{DD}/{field_name}_{guid}{file_ext}=\"{yy}-{MM}-{DD}/{field_name}_{guid}{file_ext}\"→ 26-05-09/user_photo_d3b07384d113edec49eaa6238ad5ff00.png",
        "archive/{yyyy}/{file_name_only}_{HH}{mm}{file_ext}=\"archive/{yyyy}/{file_name_only}_{HH}{mm}{file_ext}\"→ archive/2026/avatar_1504.png",
    ];
}
