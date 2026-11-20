using FluentAssertions;
using Mars.Core.Exceptions;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;

namespace Mars.WebApiClient.Integration.Tests.Tests.Accounts;

/// <seealso cref="AccountController.UploadAvatar"/>
public class UploadAvatarTests : BaseWebApiClientTests
{
    // 1x1 PNG (67 байт) — заведомо валидный, лёгкий.
    private static readonly byte[] Png1X1 = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public UploadAvatarTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task UploadAvatar_ValidPng_ReturnsFullAndThumbUrls()
    {
        //Arrange
        _ = nameof(AccountController.UploadAvatar);
        var client = GetWebApiClient(isAnonymous: false);
        using var stream = new MemoryStream(Png1X1);

        //Act
        var result = await client.Account.UploadAvatar(stream, "avatar.png");

        //Assert — полный 256×256 и миниатюра 64×64 в upload/Users/{id}/, с cache-bust версией
        result.Should().NotBeNull();
        result.AvatarUrl.Should().StartWith("/upload/Users/").And.Contain("avatar.webp?v=");
        result.ThumbUrl.Should().StartWith("/upload/Users/").And.Contain("avatar-64.webp?v=");
    }

    [IntegrationFact]
    public async Task UploadAvatar_UnsupportedFormat_Fails()
    {
        //Arrange
        _ = nameof(AccountController.UploadAvatar);
        var client = GetWebApiClient(isAnonymous: false);
        using var stream = new MemoryStream("not an image"u8.ToArray());

        //Act
        var act = () => client.Account.UploadAvatar(stream, "avatar.txt");

        //Assert — 466 UserActionException транслируется клиентом в доменное исключение
        await act.Should().ThrowAsync<UserActionException>()
            .WithMessage("*Неподдерживаемый формат*");
    }

    [IntegrationFact]
    public async Task UploadAvatar_Anonymous_Unauthorized()
    {
        //Arrange
        _ = nameof(AccountController.UploadAvatar);
        var client = GetWebApiClient(isAnonymous: true);
        using var stream = new MemoryStream(Png1X1);

        //Act
        var act = () => client.Account.UploadAvatar(stream, "avatar.png");

        //Assert — 401 транслируется клиентом в UnauthorizedException
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
