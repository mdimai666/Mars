using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/HttpInFormSaveFilesNode/HttpInFormSaveFilesNode{.lang}.md")]
[Display(GroupName = "network")]
public class HttpInFormSaveFilesNode : Node
{
    [Required]
    public string FilePathTemplate { get; set; } = "media/{yyyy}/{file_name}";

    public bool SaveInMediaFiles { get; set; }
    public bool AllowSaveFileOutsideUploads { get; set; }

    public HttpInFormSaveFilesNode()
    {
        Color = "#e7e6af";
        Inputs = [new()];
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/web-48.png";
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
