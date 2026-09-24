using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;

namespace Mars.Docker.Contracts.Nodes;

/// <summary>
/// Одноразовый прогон контейнера (docker run --rm): создаёт, стартует, ждёт выхода,
/// забирает вывод и удаляет контейнер.
/// </summary>
[Display(GroupName = DockerNodesMeta.GroupName)]
public class DockerRunNode : Node
{
    public override string TypeId => "module.docker.RunNode";

    public override string Label => string.IsNullOrWhiteSpace(Name) ? "docker run" : Name;

    [Display(Name = "image")]
    [Required]
    public string Image { get; set; } = "";

    [Display(Name = "command (space separated)")]
    public string Command { get; set; } = "";

    [Display(Name = "env (KEY=VALUE per line)")]
    public string Env { get; set; } = "";

    [Display(Name = "stdin (empty = msg payload)")]
    public string Stdin { get; set; } = "";

    [Display(Name = "timeout, sec (0 = default)")]
    public int TimeoutSeconds { get; set; }

    [Display(Name = "keep container after exit")]
    public bool KeepContainer { get; set; }

    public DockerRunNode()
    {
        Color = DockerNodesMeta.Color;
        Icon = DockerNodesMeta.Icon;
        Inputs = [new()];
        Outputs = [new()];
    }
}
