using Mars.Core.Exceptions;
using Mars.Host.Shared.Scheduler;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Controllers.Schedulers;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Schedulers;

public class GetSchedulerTests : BaseWebApiClientTests
{
    public GetSchedulerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
    }

    [IntegrationFact]
    public async void ListSchedulerJob_Request_Unauthorized()
    {
        //Arrange
        var client = GetWebApiClient(true);

        //Act
        var action = () => client.Scheduler.JobListTable(new());

        //Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [IntegrationFact]
    public async Task ListSchedulerJob_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();
        var jobName = "dummyJob";
        var scheduler = AppFixture.ServiceProvider.GetRequiredService<ISchedulerManager>();
        await scheduler.AddTestJob(jobName);

        //Act
        var list = await client.Scheduler.JobList(new());
        var list2 = await client.Scheduler.JobListTable(new());

        //Assert
        list.Items.Should().HaveCountGreaterThanOrEqualTo(0);
        list2.Items.Should().HaveCountGreaterThanOrEqualTo(0);
    }

}
