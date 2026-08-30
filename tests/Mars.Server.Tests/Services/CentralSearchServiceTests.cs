using FluentAssertions;
using Mars.Cms.Abstractions.Dto.Search;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.Search;
using Mars.Cms.Host.Services;

namespace Mars.Server.Tests.Services;

public class CentralSearchServiceTests
{
    private sealed class FakeProvider : ICentralSearchProvider
    {
        public required int Order { get; init; }
        public required SearchFoundElement[] Items { get; init; }

        public Task<IReadOnlyCollection<SearchFoundElement>> SearchAsync(string query, int maxCount, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<SearchFoundElement>>(Items);
    }

    static SearchFoundElement El(string title, float relevant)
        => new() { Title = title, Key = title, Relevant = relevant, Type = FoundElementType.Url };

    [Fact]
    public async Task ActionBarSearch_OrdersProvidersByOrder_AndItemsByRelevant()
    {
        var service = new CentralSearchService(new ICentralSearchProvider[]
        {
            new FakeProvider { Order = 20, Items = [El("post-low", 2), El("post-high", 5)] },
            new FakeProvider { Order = 10, Items = [El("type-low", 1), El("type-high", 9)] },
        });

        var result = await service.ActionBarSearch("q", 10, CancellationToken.None);

        result.Select(s => s.Title).Should().Equal("type-high", "type-low", "post-high", "post-low");
    }

    [Fact]
    public async Task ActionBarSearch_TakesMaxCountAcrossProviders()
    {
        var service = new CentralSearchService(new ICentralSearchProvider[]
        {
            new FakeProvider { Order = 10, Items = [El("a", 1), El("b", 1), El("c", 1)] },
            new FakeProvider { Order = 20, Items = [El("d", 1), El("e", 1)] },
        });

        var result = await service.ActionBarSearch("q", 4, CancellationToken.None);

        result.Should().HaveCount(4);
        result.Select(s => s.Title).Should().Equal("a", "b", "c", "d");
    }

    [Fact]
    public async Task ActionBarSearch_NoProviders_ReturnsEmpty()
    {
        var service = new CentralSearchService(Array.Empty<ICentralSearchProvider>());

        var result = await service.ActionBarSearch("q", 10, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
