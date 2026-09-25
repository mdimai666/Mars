using Docker.DotNet.Models;
using Mars.Contracts.Common;
using Mars.Contracts.Extensions;
using Mars.Docker.Contracts;

namespace Mars.Docker.Host.Mappings;

public static class DockerImageMappings
{
    public static ImageSummaryResponse1 ToResponse(this ImagesListResponse request)
        => new()
        {
            ID = request.ID,
            ParentID = request.ParentID,
            RepoTags = request.RepoTags ?? [],
            RepoDigests = request.RepoDigests ?? [],
            Created = request.Created,
            Size = request.Size,
            SharedSize = request.SharedSize,
            VirtualSize = request.VirtualSize,
            Labels = request.Labels?.ToDictionary() ?? new Dictionary<string, string>(),
            Containers = request.Containers,
        };

    public static ListDataResult<ImageSummaryResponse1> ToResponse(this ListDataResult<ImagesListResponse> request)
        => request.ToMap(ToResponse);

    public static PagingResult<ImageSummaryResponse1> ToResponse(this PagingResult<ImagesListResponse> request)
        => request.ToMap(ToResponse);
}
