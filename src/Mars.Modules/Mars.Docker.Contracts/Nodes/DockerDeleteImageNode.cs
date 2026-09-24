using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Docker.Contracts.Nodes;

[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerDeleteImageNode : Node
{
    public override string TypeId => "module.docker.DeleteImageNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker rmi" : Name;

    [Display(Name = "image (name:tag or id)")]
    [Required]
    public string Image { get; set; } = "";

    public DockerDeleteImageNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
