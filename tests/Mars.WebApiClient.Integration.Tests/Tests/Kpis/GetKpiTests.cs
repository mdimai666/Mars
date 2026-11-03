using FluentAssertions;
using Mars.Cms.Contracts.Feedbacks;
using Mars.Cms.Contracts.PostCategories;
using Mars.Cms.Contracts.Posts;
using Mars.Identity.Contracts.Users;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Server.Controllers;

namespace Mars.WebApiClient.Integration.Tests.Tests.Kpis;

public class GetKpiTests : BaseWebApiClientTests
{
    public GetKpiTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task Get_RegisteredKeys_Succeeds()
    {
        //Arrange
        _ = nameof(KpiController.Get);
        var client = GetWebApiClient();

        //Act
        var result = await client.Kpi.Get([UserKpiKeys.Total, UserKpiKeys.NewThisMonth, FeedbackKpiKeys.Total, FeedbackKpiKeys.NewThisWeek]);

        //Assert
        result.Should().ContainKeys(UserKpiKeys.Total, UserKpiKeys.NewThisMonth, FeedbackKpiKeys.Total, FeedbackKpiKeys.NewThisWeek);
        result[UserKpiKeys.Total].Value.Should().BeGreaterThanOrEqualTo(0);
        result[UserKpiKeys.NewThisMonth].Value.Should().BeGreaterThanOrEqualTo(0);
        result[UserKpiKeys.NewThisMonth].Value.Should().BeLessThanOrEqualTo(result[UserKpiKeys.Total].Value);
        result[FeedbackKpiKeys.Total].Value.Should().BeGreaterThanOrEqualTo(0);
        result[FeedbackKpiKeys.NewThisWeek].Value.Should().BeGreaterThanOrEqualTo(0);
        result[FeedbackKpiKeys.NewThisWeek].Value.Should().BeLessThanOrEqualTo(result[FeedbackKpiKeys.Total].Value);
    }

    [IntegrationFact]
    public async Task Get_UnknownKey_Ignored()
    {
        //Arrange
        _ = nameof(KpiController.Get);
        var client = GetWebApiClient();

        //Act
        var result = await client.Kpi.Get(["unknown.kpi"]);

        //Assert
        result.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task Get_ParameterizedKeys_Succeeds()
    {
        //Arrange
        _ = nameof(KpiController.Get);
        var client = GetWebApiClient();

        //Act
        var result = await client.Kpi.Get([PostKpiKeys.Total, PostKpiKeys.TotalFor("post"), PostCategoryKpiKeys.TotalFor("post")]);

        //Assert
        result.Should().ContainKeys(PostKpiKeys.Total, PostKpiKeys.TotalFor("post"), PostCategoryKpiKeys.TotalFor("post"));
        result[PostKpiKeys.Total].Value.Should().BeGreaterThanOrEqualTo(0);
        result[PostKpiKeys.TotalFor("post")].Value.Should().BeGreaterThanOrEqualTo(0);
        result[PostKpiKeys.TotalFor("post")].Value.Should().BeLessThanOrEqualTo(result[PostKpiKeys.Total].Value);
        result[PostCategoryKpiKeys.TotalFor("post")].Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [IntegrationFact]
    public async Task Get_ParameterizedKey_UnknownPrefixIgnored_EmptyParameterZero()
    {
        //Arrange
        _ = nameof(KpiController.Get);
        var client = GetWebApiClient();

        //Act
        var result = await client.Kpi.Get(["unknown.kpi:x", PostKpiKeys.TotalFor("nonexistent-type")]);

        //Assert
        result.Should().NotContainKey("unknown.kpi:x");
        result[PostKpiKeys.TotalFor("nonexistent-type")].Value.Should().Be(0);
    }
}
