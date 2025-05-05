using Mars.Host.Data.Entities;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Shared.Contracts.Users;
using Mars.Test.Common.FixtureCustomizes;
using Mars.WebApiClient.Integration.Tests.GeneralTestAbstractions;
using FluentAssertions;

namespace Mars.WebApiClient.Integration.Tests.Tests.Users;

public class GetUserTests : BaseWebApiClientTests
{
    GeneralGetTests<UserEntity, ListUserQueryRequest, TableUserQueryRequest, UserDetailResponse, UserListItemResponse> _getTest;

    public GetUserTests(ApplicationFixture appFixture) : base(appFixture)
    {
        _fixture.Customize(new FixtureCustomize());

        _getTest = new(
            this,
            (client, id) => client.User.Get(id),
            (client, query) => client.User.List(query),
            (client, query) => client.User.ListTable(query)
            );

    }

    [IntegrationFact]
    public async void GetUser_Request_Unauthorized()
    {
        await _getTest.GetDetail_Request_Unauthorized();
    }

    [IntegrationFact]
    public async void GetUser_ValidRequest_ShouldSuccess()
    {
        await _getTest.GetDetail_ValidRequest_ShouldSuccess();
    }

    [IntegrationFact]
    public void GetUser_NotExistEntity_Fail404ShouldReturnNullInsteadException()
    {
        _getTest.GetDetail_NotExistEntity_Fail404ShouldReturnNullInsteadException();
    }


    [IntegrationFact]
    public async void ListUser_Request_Unauthorized()
    {
        await _getTest.List_Request_Unauthorized(new());
    }

    [IntegrationFact]
    public async void ListUser_ValidRequest_ShouldSuccess()
    {
        await _getTest.List_ValidRequest_ShouldSuccess(new(), new());
    }

    [IntegrationFact]
    public async void ListDetailUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.User.ListDetail(new());

        //Assert
        list.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [IntegrationFact]
    public async void TableListDetailUser_ValidRequest_ShouldSuccess()
    {
        //Arrange
        var client = GetWebApiClient();

        //Act
        var list = await client.User.ListTableDetail(new());

        //Assert
        list.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }
}
