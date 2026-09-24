using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Docker.Contracts.Nodes;

/// <summary>
/// Команда в работающем контейнере (docker exec).
/// </summary>
[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerExecNode : Node
{
    public override string TypeId => "module.docker.ExecNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker exec" : Name;

    [Display(Name = "container (id or name)")]
    [Required]
    public string ContainerName { get; set; } = "";

    [Display(Name = "command (space separated)")]
    [Required]
    public string Command { get; set; } = "";

    [Display(Name = "env (KEY=VALUE per line)")]
    public string Env { get; set; } = "";

    [Display(Name = "working dir")]
    public string WorkingDir { get; set; } = "";

    [Display(Name = "user")]
    public string User { get; set; } = "";

    [Display(Name = "timeout, sec (0 = default)")]
    public int TimeoutSeconds { get; set; }

    public DockerExecNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
