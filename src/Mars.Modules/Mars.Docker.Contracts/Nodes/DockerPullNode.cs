using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Docker.Contracts.Nodes;

[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerPullNode : Node
{
    public override string TypeId => "module.docker.PullNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker pull" : Name;

    [Display(Name = "image")]
    [Required]
    public string Image { get; set; } = "";
    public string ImageKind { get; set; } = InputValueKind.Const;

    [Display(Name = "tag")]
    public string Tag { get; set; } = "latest";
    public string TagKind { get; set; } = InputValueKind.Const;

    public DockerPullNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
