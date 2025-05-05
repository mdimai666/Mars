using Mars.Controllers;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Scheduler;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Controllers.Schedulers;
using Mars.Test.Common.FixtureCustomizes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApiClient.Integration.Tests.Tests.Schedulers;

public class InjectSchedulerTests : BaseWebApiClientTests
{
    public InjectSchedulerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        //reset scheduler jobs
    }

    [IntegrationFact]
    public async Task InjectJob_ValidRequest_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SchedulerController.InjectJob);
        _ = nameof(ISchedulerManager.InjectJob);
        var client = GetWebApiClient();

        var jobName = "dummyJob";
        var scheduler = AppFixture.ServiceProvider.GetRequiredService<ISchedulerManager>();
        await scheduler.AddTestJob(jobName);

        //Act
        var action = () => client.Scheduler.InjectJob(jobName, "tests");

        //Assert
        await action.Should().NotThrowAsync();
    }

    [IntegrationFact]
    public async void InjectJob_InvalidRequest_Fail404Exception()
    {
        //Arrange
        _ = nameof(SchedulerController.InjectJob);
        _ = nameof(ISchedulerManager.InjectJob);
        var client = GetWebApiClient();
        var invalidJobName = "invalid-jobName";

        //Act
        var action = () => client.Scheduler.InjectJob(invalidJobName, "tests");

        //Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
