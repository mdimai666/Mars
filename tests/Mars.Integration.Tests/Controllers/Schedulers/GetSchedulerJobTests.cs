using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Scheduler;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Schedulers;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Schedulers;

public class GetSchedulerJobTests : ApplicationTests
{
    const string _apiUrl = "/api/Scheduler";
    private readonly ISchedulerManager _scheduler;

    public GetSchedulerJobTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _scheduler = AppFixture.ServiceProvider.GetRequiredService<ISchedulerManager>();
    }

    [IntegrationFact]
    public async Task ListSchedulerJob_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(SchedulerController.JobListTable);
        _ = nameof(ISchedulerManager.JobListPaging);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "JobListTable").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task ListSchedulerJob_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SchedulerController.JobListTable);
        _ = nameof(ISchedulerManager.JobListPaging);
        var client = AppFixture.GetClient();

        var jobName = "dummyJob";
        await _scheduler.AddTestJob(jobName);

        var request = new ListSchedulerJobQueryRequest();

        //Act
        var result = await client.Request(_apiUrl, "JobListTable")
            .AppendQueryParam(request)
            .GetJsonAsync<PagingResult<SchedulerJobResponse>>();

        //Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(s => s.Name == jobName);
    }
}
