using FluentAssertions;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Nodes;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Docker.Tests;

public class DockerNodeOutputSpecTests
{
    [Fact]
    public void RunAndExec_DeclareResultDtoPaths()
    {
        //Arrange
        var expected = new[] { "Payload", "Payload.ExitCode", "Payload.Stdout", "Payload.Stderr", "Payload.TimedOut" };

        //Act
        var run = NodeOutputValueSpecReader.ReadStatics(typeof(DockerRunNodeImpl));
        var exec = NodeOutputValueSpecReader.ReadStatics(typeof(DockerExecNodeImpl));

        //Assert
        run.Select(s => s.Path).Should().BeEquivalentTo(expected);
        exec.Select(s => s.Path).Should().BeEquivalentTo(expected);
        run.Should().Contain(s => s.Path == "Payload.ExitCode" && s.VarType == "long");
        run.Should().Contain(s => s.Path == "Payload.Stdout" && s.VarType == "string");
        run.Should().Contain(s => s.Path == "Payload.TimedOut" && s.VarType == "bool");
    }

    [Fact]
    public void PullDeleteImageState_DeclareStringPayload()
    {
        //Arrange & Act
        var pull = NodeOutputValueSpecReader.ReadStatics(typeof(DockerPullNodeImpl));
        var delete = NodeOutputValueSpecReader.ReadStatics(typeof(DockerDeleteImageNodeImpl));
        var state = NodeOutputValueSpecReader.ReadStatics(typeof(DockerStateNodeImpl));

        //Assert
        pull.Should().ContainSingle(s => s.Path == "Payload" && s.VarType == "string");
        delete.Should().ContainSingle(s => s.Path == "Payload" && s.VarType == "string");
        state.Should().ContainSingle(s => s.Path == "Payload" && s.VarType == "string");
    }

    [Fact]
    public void KindFields_DefaultToConst()
    {
        //Arrange & Act
        var run = new DockerRunNode();
        var exec = new DockerExecNode();
        var pull = new DockerPullNode();
        var delete = new DockerDeleteImageNode();
        var state = new DockerStateNode();

        //Assert
        new[]
        {
            run.ImageKind, run.CommandKind, run.EnvKind, run.StdinKind,
            exec.ContainerNameKind, exec.CommandKind, exec.EnvKind, exec.WorkingDirKind, exec.UserKind,
            pull.ImageKind, pull.TagKind,
            delete.ImageKind,
            state.ContainerNameKind,
        }.Should().AllBe(InputValueKind.Const);
    }
}
