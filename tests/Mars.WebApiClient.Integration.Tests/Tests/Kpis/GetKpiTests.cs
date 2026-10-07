using FluentAssertions;
using Mars.Cms.Contracts.Feedbacks;
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
}
