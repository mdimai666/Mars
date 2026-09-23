using Mars.CodeCompletion.Host.Abstractions;
using Microsoft.CodeAnalysis;

namespace Mars.CodeCompletion.Tests;

public class TestMsg
{
    public object? Payload { get; set; }
}

public class TestGlobals
{
    public string NodeId = "";
    public TestMsg msg = new();

    public void Send(object payload)
    {
    }

    public int Add(int a, int b) => a + b;
}

public class TestCodeContextProvider : ICodeContextProvider
{
    public const string Id = "test.script";

    public string ContextId => Id;

    public Type? HostObjectType => typeof(TestGlobals);

    public IReadOnlyList<string> Imports { get; } = ["System", "System.Collections.Generic", "System.Linq"];

    public IReadOnlyCollection<MetadataReference> GetMetadataReferences() => [];
}
