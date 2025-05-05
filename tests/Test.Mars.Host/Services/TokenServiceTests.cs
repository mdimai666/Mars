using System.Text.Json;
using FluentAssertions;
using Mars.Host.Models;
using Mars.Host.Services;
using Mars.Host.Services.MarsSSOClient;
using Microsoft.Extensions.Options;

namespace Test.Mars.Host.Services;

public class TokenServiceTests
{
    [Fact]
    public void CheckTokenExpireConvert()
    {
        long jwtExpiryInMinutes = 43200;
        var expires = DateTime.Now.AddMinutes(Convert.ToDouble(jwtExpiryInMinutes));

        var result = global::Mars.Host.Services.TokenService.JwtExpireDateTimeToUnixSeconds(expires);

        Assert.Equal(new DateTimeOffset(expires).ToUnixTimeSeconds(), result);
    }

    string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU2ZDQ1NGM2LTZiOGQtNGYyNS04OTFmLTBmZjRjMGJlMGVlMCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ0ZXN0IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvZW1haWxhZGRyZXNzIjoidGVzdEBtYWlsLnJ1IiwiQXNwTmV0LklkZW50aXR5LlNlY3VyaXR5U3RhbXAiOiJLVVpDM0FXUjU0MjVaN0ZUUlpFUkVaV1RITVlWTzNRNiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2dpdmVubmFtZSI6IlRlc3QiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9zdXJuYW1lIjoiVGVzdGVyIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiVmlld2VyIiwiZXhwIjoxNzE1MzE2NzYxLCJpc3MiOiJibGFzdElzc3VlckFQSSIsImF1ZCI6Imh0dHBzOi8vbG9jYWxob3N0OjUwMDMifQ.iS5VKbqvmZAW_HyjhEJY9YvU3rUG_Bt0kusq6GqswjE";

    string json = @"{
          ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"": ""56d454c6-6b8d-4f25-891f-0ff4c0be0ee0"",
          ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"": ""test"",
          ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"": ""test@mail.ru"",
          ""AspNet.Identity.SecurityStamp"": ""KUZC3AWR5425Z7FTRZEREZWTHMYVO3Q6"",
          ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"": ""Test"",
          ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"": ""Tester"",
          ""http://schemas.microsoft.com/ws/2008/06/identity/claims/role"": ""Viewer"",
          ""exp"": 1715316761,
          ""iss"": ""blastIssuerAPI"",
          ""aud"": ""https://localhost:5003""
        }";

    [Fact]
    public void JwtEncode()
    {
        // Arrange
        var tokenService = TokenService();

        // Act
        var token = tokenService.JwtEncode(new Dictionary<string, object> { ["name"] = "User" });

        // Assert
        token.Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public void JwtDecode()
    {
        // Arrange
        var tokenService = TokenService();
        var expectUserInfo = JsonSerializer.Deserialize<MarsJwtTokenUserUnfo>(json);

        // Act
        var userInfo = tokenService.JwtDecode<MarsJwtTokenUserUnfo>(token, verify: false);

        // Assert
        userInfo.Should().BeEquivalentTo(expectUserInfo);
    }

    TokenService TokenService()
    {
        var jwtSettings = new JwtSettings()
        {
            ExpiryInMinutes = 30,
            SecurityKey = "MarsSuperSecretKey256greaterThan32",
            ValidAudience = "https://localhost:5003",
            ValidIssuer = "MarsIssuerAPI"
        };

        return new TokenService(Options.Create(jwtSettings));
    }

}
