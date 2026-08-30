using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

public enum RestartPolicyKindResponse
{
    [EnumMember(Value = "")]
    Undefined,

    [EnumMember(Value = "no")]
    No,

    [EnumMember(Value = "always")]
    Always,

    [EnumMember(Value = "on-failure")]
    OnFailure,

    [EnumMember(Value = "unless-stopped")]
    UnlessStopped
}
