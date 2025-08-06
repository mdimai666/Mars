using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Plugins;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;

namespace Mars.Host.Shared.Mappings.Plugins;

public static class PluginMapping
{
    public static PluginInfoResponse ToResponse(this PluginInfoDto entity)
        => new()
        {
            PackageId = entity.PackageId,
            Title = entity.Title,
            AssemblyName = entity.AssemblyName,
            Version = entity.Version,
            Description = entity.Description,
            Enabled = entity.Enabled,
            InstalledAt = entity.InstalledAt,
            FrontManifest = entity.FrontManifest,
            PackageTags = entity.PackageTags,
            RepositoryUrl = entity.RepositoryUrl,
            PackageIconUrl = entity.PackageIconUrl,
        };

    public static PluginManifestInfoResponse ToResponse(this PluginManifestInfoDto entity)
        => new()
        {
            Name = entity.Name,
            Uri = entity.Uri,
        };

    public static IReadOnlyCollection<PluginInfoResponse> ToResponse(this IReadOnlyCollection<PluginInfoDto> list)
        => list.Select(ToResponse).ToList();

    public static ListDataResult<PluginInfoResponse> ToResponse(this ListDataResult<PluginInfoDto> list)
        => list.ToMap(ToResponse);

    public static PagingResult<PluginInfoResponse> ToResponse(this PagingResult<PluginInfoDto> list)
        => list.ToMap(ToResponse);

    public static IReadOnlyCollection<PluginManifestInfoResponse> ToResponse(this IEnumerable<PluginManifestInfoDto> list)
        => list.Select(ToResponse).ToList();

    public static PluginsUploadOperationResultResponse ToResponse(this PluginsUploadOperationResultDto entity)
        => new()
        {
            Items = entity.Items.ToResponse(),
            UploadStart = entity.UploadStart,
            UploadEnd = entity.UploadEnd,
        };

    public static PluginsUploadOperationItemResponse ToResponse(this PluginsUploadOperationItemDto entity)
        => new()
        {
            ErrorMessage = entity.ErrorMessage,
            FileName = entity.FileName,
            FileSize = entity.FileSize,
        };

    public static IReadOnlyCollection<PluginsUploadOperationItemResponse> ToResponse(this IEnumerable<PluginsUploadOperationItemDto> list)
        => list.Select(ToResponse).ToArray();
}
