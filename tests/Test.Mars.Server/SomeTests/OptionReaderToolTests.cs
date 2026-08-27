using Mars.Host.Options;

namespace Test.Mars.Host.SomeTests;

public class OptionReaderToolTests
{
    [Theory]
    [InlineData(["http://localhost:5000;http://localhost:801", 5000])]
    [InlineData(["https://example.com:443", 443])]
    [InlineData(["http://+:5000", 5000])]
    [InlineData(["https://*:443", 443])]
    [InlineData(["http://localhost:8080", 8080])]
    [InlineData(["http://localhost", 80])]
    [InlineData(["https://example.com", 443])]
    [InlineData(["http://127.0.0.1:81", 81])]
    [InlineData(["http://[::]:88", 88])]
    [InlineData(["http://[::]", 80])]
    public void ExtractPortFromUrls_ShouldReturnCorrectPort(string url, int expectPort)
    {
        //Arrange
        //Act
        OptionReaderTool.ExtractPortFromUrls(url, out var port);

        //Assert
        Assert.Equal(expectPort, port);
    }

    [Theory]
    [InlineData(["www|s"])]
    [InlineData(["x.x::984984"])]
    public void ExtractPortFromUrls_InvalidUrls_ShouldReturnLocalhost80(string invalidUrl)
    {
        //Act
        var isValid = OptionReaderTool.ExtractPortFromUrls(invalidUrl, out var port);
        //Assert
        Assert.False(isValid);
        Assert.Equal(80, port);
    }

    [Theory]
    [InlineData(["http://localhost:5000;http://localhost:801", "http://localhost:5000"])]
    [InlineData(["https://example.com:443", "https://example.com"])]
    [InlineData(["http://+:5000", "http://localhost:5000"])]
    [InlineData(["https://*:443", "https://localhost"])]
    [InlineData(["http://localhost:8080", "http://localhost:8080"])]
    [InlineData(["http://localhost", "http://localhost"])]
    [InlineData(["https://example.com", "https://example.com"])]
    [InlineData(["http://1.1.1.1:80", "http://1.1.1.1"])]
    [InlineData(["http://[::]", "http://localhost"])]
    [InlineData(["http://[::]:80", "http://localhost"])]
    [InlineData(["http://[::]:801", "http://localhost:801"])]
    [InlineData(["http://[::1]:801", "http://[::1]:801"])]
    [InlineData(["https://[2001:db8::1]", "https://[2001:db8::1]"])]
    public void NormalizeUrl_ShouldReturnNormalizedUrl(string urls, string expectUrl)
    {
        //Arrange
        //Act
        var isValid = OptionReaderTool.NormalizeUrl(urls, out var url);

        //Assert
        Assert.True(isValid);
        Assert.Equal(expectUrl, url);
    }

    [Theory]
    [InlineData(["77y87y+xsx"])]
    [InlineData(["65465464+1"])]
    public void NormalizeUrl_InvalidUrls_ShouldReturnLocalhost80(string invalidUrl)
    {
        //Act
        var isValid = OptionReaderTool.NormalizeUrl(invalidUrl, out var url);
        //Assert
        Assert.False(isValid);
        Assert.Equal("http://localhost", url);
    }
}
