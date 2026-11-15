namespace Mars.Identity.Contracts.Users.UserProfiles;

/// <summary>
/// Результат загрузки аватара: полный (256×256) и миниатюра (64×64), webp.
/// URL содержит ?v= для сброса браузерного кэша при стабильном имени файла.
/// </summary>
public record AvatarUploadResponse(string AvatarUrl, string ThumbUrl);
