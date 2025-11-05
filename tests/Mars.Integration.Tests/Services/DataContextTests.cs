using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentAssertions;
using Flurl.Http;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Interfaces;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.TestControllers;
using Mars.Test.Common.Constants;

namespace Mars.Integration.Tests.Services;

public sealed class DataContextTests : ApplicationTests
{
    const string _apiUrl = "/api-test/TestApi1/";

    public DataContextTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task DataContext_AuthorizedUserDataResponse_ShouldResponseDataContext()
    {
        //Arrange
        _ = nameof(TestApi1Controller.CheckRequestContext);
        var client = AppFixture.GetClient();

        //Act
        var result = await client.Request(_apiUrl, "CheckRequestContext").GetJsonAsync<RequestContextImpl>();

        //Assert
        result.Should().NotBeNull();
        result.Jwt.Should().Be(ApplicationFixture.BearerToken);
        result.UserName.Should().Be(result.User.UserName);
        result.IsAuthenticated.Should().BeTrue();
        var testUser = UserConstants.TestUser;
        result.UserName.Should().Be(testUser.UserName);
        result.User.Id.Should().Be(testUser.Id);
        result.User.FirstName.Should().Be(testUser.FirstName);
        result.User.LastName.Should().Be(testUser.LastName);
        result.User.Email.Should().Be(testUser.Email);
        result.Claims2.First(s => s.Key == "AspNet.Identity.SecurityStamp").Value.Should().Be(testUser.SecurityStamp);
    }

    [IntegrationFact]
    public async Task DataContext_NonAuthorizedUser_ShouldEmpty()
    {
        //Arrange
        _ = nameof(TestApi1Controller.CheckRequestContext);
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "CheckRequestContext").GetJsonAsync<RequestContextImpl>();

        //Assert
        result.Should().NotBeNull();
        result.Jwt.Should().BeNullOrEmpty();
        result.UserName.Should().BeNullOrEmpty();
        result.IsAuthenticated.Should().BeFalse();
        result.User.Should().BeNull();
    }

    private class RequestContextImpl : IRequestContext
    {
        [JsonIgnore]
        [JsonPropertyName("ClaimsOld")]
        public ClaimsPrincipal Claims { get; init; } = default!;

        [JsonPropertyName("Claims")]
        public IReadOnlyCollection<KeyValuePair<string, string>> Claims2 { get; init; } = default!;

        public string Jwt { get; init; } = default!;
        public string UserName { get; init; } = default!;
        public bool IsAuthenticated { get; init; } = default!;
        public HashSet<string>? Roles { get; init; } = default!;
        public RequestContextUser? User { get; init; } = default!;
    }
}
