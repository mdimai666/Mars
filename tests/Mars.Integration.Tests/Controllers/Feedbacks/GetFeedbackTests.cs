using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Data.Entities;
using Mars.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Feedbacks;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;

namespace Mars.Integration.Tests.Controllers.Feedbacks;

public class GetFeedbackTests : ApplicationTests
{
    const string _apiUrl = "/api/Feedback";

    public GetFeedbackTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async Task GetFeedback_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.Get);
        _ = nameof(FeedbackService.Get);
        var client = AppFixture.GetClient();

        var createdFeedback = _fixture.Create<FeedbackEntity>();

        var ef = AppFixture.MarsDbContext();
        await ef.Feedbacks.AddAsync(createdFeedback);
        await ef.SaveChangesAsync();

        var postTypeId = createdFeedback.Id;

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(postTypeId).GetJsonAsync<FeedbackDetailResponse>();

        //Assert
        result.Should().NotBeNull();
    }

    [IntegrationFact]
    public async Task GetFeedback_InvalidModelRequest_Fail404()
    {
        //Arrange
        _ = nameof(FeedbackController.Get);
        _ = nameof(FeedbackService.Get);
        var client = AppFixture.GetClient();
        var invalidFeedbackId = Guid.NewGuid();

        //Act
        var result = await client.Request(_apiUrl).AppendPathSegment(invalidFeedbackId)
                        .AllowAnyHttpStatus().SendAsync(HttpMethod.Get);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [IntegrationFact]
    public async Task ListFeedback_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.List);
        _ = nameof(FeedbackService.List);
        var client = AppFixture.GetClient();

        var createdFeedbacks = _fixture.CreateMany<FeedbackEntity>(13);
        var expectCount = 10;

        var ef = AppFixture.MarsDbContext();
        await ef.Feedbacks.AddRangeAsync(createdFeedbacks);
        await ef.SaveChangesAsync();

        var request = new ListFeedbackQueryRequest() { Skip = 0, Take = expectCount };
        var totalCount = ef.Feedbacks.Count();

        //Act
        var result = await client.Request(_apiUrl, "list/offset")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<FeedbackSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(expectCount);
        result.TotalCount.Should().Be(totalCount);
    }

    [IntegrationFact]
    public async Task ListFeedback_SearchRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(FeedbackController.List);
        _ = nameof(FeedbackService.List);
        var client = AppFixture.GetClient();

        var searchString = $"{Guid.NewGuid()}";
        var searchTitleString = $"nonUsePart_{searchString}";

        var createdFeedbacks = _fixture.CreateMany<FeedbackEntity>(3);
        createdFeedbacks.ElementAt(0).Title = searchTitleString;

        var ef = AppFixture.MarsDbContext();
        await ef.Feedbacks.AddRangeAsync(createdFeedbacks);
        await ef.SaveChangesAsync();

        var expectFeedbackId = createdFeedbacks.ElementAt(0).Id;

        var request = new ListFeedbackQueryRequest() { Search = searchString };

        //Act
        var result = await client.Request(_apiUrl, "list/offset")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<ListDataResult<FeedbackSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(1);
        result.Items.ElementAt(0).Id.Should().Be(expectFeedbackId);
    }

    [IntegrationFact]
    public async Task ListFeedbackPagination_Request_ShouldValidTotalCount()
    {
        //Arrange
        _ = nameof(FeedbackController.List);
        _ = nameof(FeedbackService.List);
        var client = AppFixture.GetClient();

        var createdFeedbacks = _fixture.CreateMany<FeedbackEntity>(15);

        var ef = AppFixture.MarsDbContext();
        await ef.Feedbacks.AddRangeAsync(createdFeedbacks);
        await ef.SaveChangesAsync();

        var request = new TableFeedbackQueryRequest() { Page = 1, PageSize = 10 };

        //Act
        var result = await client.Request(_apiUrl, "list/page")
                                    .AppendQueryParam(request)
                                    .GetJsonAsync<PagingResult<FeedbackSummaryResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Count().Should().Be(10);
        result.TotalPages.Should().Be(2);
        result.TotalCount.Should().Be(15);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
        result.HasMoreData.Should().BeTrue();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

}
