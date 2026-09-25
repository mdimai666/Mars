using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Docker.Contracts.Nodes;

/// <summary>
/// Управление состоянием контейнера: start / stop / restart / pause / unpause / delete.
/// </summary>
[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerStateNode : Node
{
    public override string TypeId => "module.docker.StateNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker state" : Name;

    [Display(Name = "container (id or name)")]
    [Required]
    public string ContainerName { get; set; } = "";
    public string ContainerNameKind { get; set; } = InputValueKind.Const;

    [Display(Name = "action")]
    public DockerStateAction Action { get; set; } = DockerStateAction.Start;

    public DockerStateNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
