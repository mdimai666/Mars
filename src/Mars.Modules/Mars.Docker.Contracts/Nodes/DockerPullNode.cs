using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Docker.Contracts.Nodes;

[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerPullNode : Node
{
    public override string TypeId => "module.docker.PullNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker pull" : Name;

    [Display(Name = "image")]
    [Required]
    public string Image { get; set; } = "";

    [Display(Name = "tag")]
    public string Tag { get; set; } = "latest";

    public DockerPullNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
