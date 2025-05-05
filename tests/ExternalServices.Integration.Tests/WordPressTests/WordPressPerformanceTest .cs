using Mars.Integration.Tests.Attributes;
using FluentAssertions;
using Flurl.Http;

namespace ExternalServices.Integration.Tests.WordPressTests;

public class WordPressPerformanceTest : IClassFixture<WordPressFixture>
{
    private readonly WordPressFixture _fixture;

    public WordPressPerformanceTest(WordPressFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact(Skip = "not implement")]
    public async Task WordPress_Connection()
    {
        var url = _fixture.WordPressUrl;

        var html = await _fixture.Client.Request(url).GetStringAsync();

        //all below not work
        //var req = await _fixture.Client.Request("/wp-my-auth.php").WithAutoRedirect(true).WithCookies(out var jar).GetAsync();
        //var status = req.StatusCode;
        //var cookies = req.Cookies;

        //var req2 = await _fixture.Client.Request("/wp-json/wp/v2/posts").WithCookies(jar).AllowAnyHttpStatus().PostJsonAsync(new { title = "123", content = "222", status = "publish" });

        html.Should().Contain("WordPress");
    }

    [IntegrationFact(Skip = "client cannot auth")] //still on work. now Flurl client not authorized
    public async Task TestResponseTimeFor10000RandomPostPages()
    {
        var random = new Random();
        var totalTime = TimeSpan.Zero;

        for (int i = 0; i < 10000; i++)
        {
            int postId = random.Next(1, 100); // Assuming there are 100 posts
            var startTime = DateTime.UtcNow;

            var response = await _fixture.Client.Request($"/?p={postId}").GetStringAsync();

            var endTime = DateTime.UtcNow;
            totalTime += endTime - startTime;
        }

        var averageResponseTime = totalTime.TotalMilliseconds / 10000;
        Console.WriteLine($"Average response time for 10,000 random post pages: {averageResponseTime} ms");
    }

}
