using System.Text.Json.Serialization;
using FluentAssertions;
using Flurl.Http;
using Mars.Integration.Tests.Attributes;
using Xunit.Abstractions;

namespace ExternalServices.Integration.Tests.WordPressTests;

public class WordPressPerformanceTest : IClassFixture<WordPressFixture>
{
    private readonly WordPressFixture _fixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public const string? SkipTest = "not require every time";

    public WordPressPerformanceTest(WordPressFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
    }

    [IntegrationFact(Skip = SkipTest)]
    public async Task WordPress_ConnectionAndAuthorized_Success()
    {
        //Arrange
        var url = _fixture.WordPressUrl + "/wp-json/wp/v2/users/me";

        //Act
        var html = await _fixture.Client.Request(url).GetStringAsync();

        //Assert
        html.Should().Contain("admin");
    }

    [IntegrationFact(Skip = SkipTest)]
    public async Task TestResponseTimeFor10000RandomPostPages()
    {
        var random = new Random();

        var response = await _fixture.Client.Request("/wp-json/wp/v2/posts?_fields=id,title,slug").GetAsync();
        var totalPosts = response.Headers.FirstOrDefault("X-WP-Total");
        var totalPages = response.Headers.FirstOrDefault("X-WP-TotalPages");
        var posts = await response.GetJsonAsync<WordPressPost[]>();

        var startTime = DateTime.UtcNow;
        var requestsCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            var randomPost = posts[random.Next(posts.Length)];
            var postResponse = await _fixture.Client.Request($"/{randomPost.Slug}/").GetStringAsync();
            requestsCount++;
        }
        var endTime = DateTime.UtcNow;
        var totalDuration = endTime - startTime;
        var averageResponseTime = totalDuration.TotalMilliseconds / requestsCount;
        var rps = requestsCount / totalDuration.TotalSeconds;

        _testOutputHelper.WriteLine($"Total duration: {totalDuration.TotalSeconds:F2} seconds");
        _testOutputHelper.WriteLine($"Total requests: {requestsCount}");
        _testOutputHelper.WriteLine($"Average response time: {averageResponseTime:F2} ms");
        _testOutputHelper.WriteLine($"Requests per second (RPS): {rps:F2}");
    }

}

public class WordPressPost
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("link")]
    public string Link { get; set; } = default!;

    [JsonPropertyName("title")]
    public ContentInfo Title { get; set; } = default!;

    [JsonPropertyName("content")]
    public ContentInfo Content { get; set; } = default!;

    [JsonPropertyName("excerpt")]
    public ContentInfo Excerpt { get; set; } = default!;

    [JsonPropertyName("author")]
    public int AuthorId { get; set; }

    [JsonPropertyName("featured_media")]
    public int FeaturedMediaId { get; set; }

    [JsonPropertyName("categories")]
    public int[] Categories { get; set; } = default!;

    [JsonPropertyName("tags")]
    public int[] Tags { get; set; } = default!;
}

public class ContentInfo
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; } = default!;

    [JsonPropertyName("protected")]
    public bool IsProtected { get; set; }
}
