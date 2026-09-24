namespace Mars.Docker.Contracts.Nodes;

public static class DockerNodesMeta
{
    public const string GroupName = "docker";
    public const string Color = "#2496ED";
    public const string Icon = "_content/Mars.Docker.Front/nodes/docker.svg";
}

public enum DockerStateAction
{
    Start,
    Stop,
    Restart,
    Pause,
    Unpause,
    Delete,
}
