using System.ComponentModel.DataAnnotations;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;

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
    public string ImageKind { get; set; } = InputValueKind.Const;

    [Display(Name = "command (space separated)")]
    public string Command { get; set; } = "";
    public string CommandKind { get; set; } = InputValueKind.Const;

    [Display(Name = "env (KEY=VALUE per line)")]
    public string Env { get; set; } = "";
    public string EnvKind { get; set; } = InputValueKind.Const;

    [Display(Name = "stdin (empty = msg payload)")]
    public string Stdin { get; set; } = "";
    public string StdinKind { get; set; } = InputValueKind.Const;

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
