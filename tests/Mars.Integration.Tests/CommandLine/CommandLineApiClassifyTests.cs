using FluentAssertions;
using Mars.CommandLine;

namespace Mars.Integration.Tests.CommandLine;

/// <summary>
/// Классификация вызова для fast-path тонкого клиента (Program.cs).
/// Инфраструктура не нужна — только дерево команд System.CommandLine.
/// </summary>
public class CommandLineApiClassifyTests
{
    static CliInvocationKind Classify(params string[] args)
    {
        return new CommandLineApi(typeof(Program).Assembly, [typeof(InfoCommand)]).Remote.ClassifyInvocation(args);
    }

    [Fact]
    public void NoCommand_RunServer()
    {
        Classify().Should().Be(CliInvocationKind.RunServer);
        Classify("--disable-logs").Should().Be(CliInvocationKind.RunServer);
        Classify("-cfg", "a.json").Should().Be(CliInvocationKind.RunServer);
        Classify("-cfg", "a.json", "--disable-logs").Should().Be(CliInvocationKind.RunServer);
    }

    [Fact]
    public async Task HelpDetection_Survives_ChildProcessCallSequence()
    {
        // последовательность дочернего процесса: CheckGlobalOption (блокировка логов/UDS),
        // InvokeBaseCommands, затем классификация — на той же инстанции CommandLineApi
        var api = new CommandLineApi(typeof(Program).Assembly, [typeof(InfoCommand)]);
        api.CheckGlobalOption<bool>("--disable-logs", ["-h"]);
        api.CheckGlobalOption<bool>("--no-uds", ["-h"]);

        var (invoked, isHelpCmd) = await api.InvokeBaseCommands(["-h"]);
        invoked.Should().BeFalse();
        isHelpCmd.Should().BeTrue();

        api.Remote.ClassifyInvocation(["-h"]).Should().Be(CliInvocationKind.HelpOrVersion);
    }

    [Fact]
    public void HelpDetection_StableAcrossRepeatedCalls()
    {
        // в дочернем процессе классификация вызывается несколько раз на одной инстанции
        // (InvokeBaseCommands, затем InvokeAsync) — повторные вызовы обязаны давать тот же результат
        var api = new CommandLineApi(typeof(Program).Assembly, [typeof(InfoCommand)]);
        api.Remote.ClassifyInvocation(["-h"]).Should().Be(CliInvocationKind.HelpOrVersion);
        api.Remote.ClassifyInvocation(["-h"]).Should().Be(CliInvocationKind.HelpOrVersion);
        api.Remote.ClassifyInvocation(["-h"]).Should().Be(CliInvocationKind.HelpOrVersion);
    }

    [Fact]
    public void BaseCommands_InProcessOnly()
    {
        // migrate против живого сервера опасен — базовые команды не форвардятся
        Classify("info").Should().Be(CliInvocationKind.BaseCommand);
        Classify("migrate").Should().Be(CliInvocationKind.BaseCommand);
        Classify("-cfg", "a.json", "info").Should().Be(CliInvocationKind.BaseCommand);
    }

    [Fact]
    public void ModuleCommands_RemoteCandidates()
    {
        Classify("node", "inject", "5").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("ds", "backup", "-f", "x.sql").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("aichat", "send", "-m", "hello").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("node", "list", "--local").Should().Be(CliInvocationKind.RemoteCandidate);
    }

    [Fact]
    public void Status_ProbeOnlyNoAppBuild()
    {
        Classify("status").Should().Be(CliInvocationKind.StatusQuery);
        Classify("status", "--local").Should().Be(CliInvocationKind.StatusQuery);
        Classify("-cfg", "a.json", "status").Should().Be(CliInvocationKind.StatusQuery);
    }

    [Fact]
    public void WebAppAssemblyCommands_RemoteCandidates()
    {
        // option/user/role зарегистрированы в сборке WebApp и видны парсеру уже до ConfigureApp —
        // они обязаны форвардиться живому инстансу, а не поднимать второй сервер (regression:
        // раньше парсинг без ошибок ошибочно классифицировался как RunServer)
        Classify("option", "maintenance", "true").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("option", "list").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("option").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("user", "add", "--username", "u", "-p", "x").Should().Be(CliInvocationKind.RemoteCandidate);
        Classify("role", "list").Should().Be(CliInvocationKind.RemoteCandidate);
    }
}
