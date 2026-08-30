using Mars.CommandLine.Remote;

namespace Mars.Server.Tests.CommandLine;

public class CliUrlsParserTests
{
    [Theory]
    [InlineData("http://localhost:5003", CliUrlHostKind.Localhost, 5003)]
    [InlineData("http://localhost", CliUrlHostKind.Localhost, 80)]
    [InlineData("http://*:8080", CliUrlHostKind.Any, 8080)]
    [InlineData("http://+:80", CliUrlHostKind.Any, 80)]
    [InlineData("http://127.0.0.1:9000", CliUrlHostKind.Ip, 9000)]
    [InlineData("http://[::]:88", CliUrlHostKind.Ip, 88)]
    [InlineData("localhost:5003", CliUrlHostKind.Localhost, 5003)] // схема по умолчанию http
    public void Parse_SingleUrl_ReturnsOneEndpoint(string url, CliUrlHostKind kind, int port)
    {
        var plan = CliUrlsParser.Parse(url);

        var endpoint = Assert.Single(plan.Endpoints);
        Assert.Equal(kind, endpoint.Kind);
        Assert.Equal(port, endpoint.Port);
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void Parse_MultipleUrls_ReturnsAllEndpoints()
    {
        var plan = CliUrlsParser.Parse("http://localhost:5003;http://*:8080");

        Assert.Equal(2, plan.Endpoints.Count);
        Assert.Equal(5003, plan.Endpoints[0].Port);
        Assert.Equal(CliUrlHostKind.Localhost, plan.Endpoints[0].Kind);
        Assert.Equal(8080, plan.Endpoints[1].Port);
        Assert.Equal(CliUrlHostKind.Any, plan.Endpoints[1].Kind);
    }

    [Fact]
    public void Parse_Https_SkippedWithWarning()
    {
        var plan = CliUrlsParser.Parse("https://localhost:5004;http://localhost:5003");

        var endpoint = Assert.Single(plan.Endpoints);
        Assert.Equal(5003, endpoint.Port);
        var warning = Assert.Single(plan.Warnings);
        Assert.Contains("https", warning);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_Empty_NoEndpoints(string? urls)
    {
        var plan = CliUrlsParser.Parse(urls);
        Assert.Empty(plan.Endpoints);
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void Parse_Garbage_SkippedWithWarning()
    {
        var plan = CliUrlsParser.Parse("::not a url::");

        Assert.Empty(plan.Endpoints);
        Assert.Single(plan.Warnings);
    }
}
