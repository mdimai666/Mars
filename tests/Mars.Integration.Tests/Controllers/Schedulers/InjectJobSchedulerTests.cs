using FluentAssertions;
using Flurl.Http;
using Mars.Controllers;
using Mars.Host.Shared.Scheduler;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Test.Common.FixtureCustomizes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Mars.Integration.Tests.Controllers.Schedulers;

public class InjectJobSchedulerTests : ApplicationTests
{
    const string _apiUrl = "/api/Scheduler";
    private readonly ISchedulerManager _scheduler;
    private readonly ITestDummyTriggerService _triggerService;

    public InjectJobSchedulerTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());
        _scheduler = AppFixture.ServiceProvider.GetRequiredService<ISchedulerManager>();
        //_triggerService = Substitute.For<ITestDummyTriggerService>();
        _triggerService = AppFixture.ServiceProvider.GetRequiredService<ITestDummyTriggerService>();
    }

    [IntegrationFact]
    public async Task InjectJob_Request_Unauthorized()
    {
        //Arrange
        _ = nameof(SchedulerController.InjectJob);
        _ = nameof(ISchedulerManager.InjectJob);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "InjectJob").AllowAnyHttpStatus().PostAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task InjectJob_Request_ShouldSuccess()
    {
        //Arrange
        _ = nameof(SchedulerController.InjectJob);
        _ = nameof(ISchedulerManager.InjectJob);
        var client = AppFixture.GetClient();

        var jobName = "dummyJob";
        await _scheduler.AddTestJob(jobName);

        //Act
        var result = await client.Request(_apiUrl, "InjectJob")
            .AppendQueryParam(new
            {
                jobName = jobName,
                jobGroup = "tests"
            })
            .PostJsonAsync(null);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        _triggerService.Received().Execute();
    }

    [IntegrationFact]
    public async Task InjectJob_InvalidRequest_Fail404()
    {
        //Arrange
        _ = nameof(SchedulerController.InjectJob);
        _ = nameof(ISchedulerManager.InjectJob);
        var client = AppFixture.GetClient();

        var jobName = "invalid-job";

        //Act
        var res = await client.Request(_apiUrl, "InjectJob")
            .AppendQueryParam(new
            {
                jobName = jobName,
                jobGroup = "tests"
            })
            .AllowAnyHttpStatus()
            .PostJsonAsync(null);

        //Assert
        res.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
