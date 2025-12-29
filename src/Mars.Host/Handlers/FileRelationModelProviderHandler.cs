using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;
using Microsoft.Extensions.Options;

namespace Mars.Host.Handlers;

internal class FileRelationModelProviderHandler(IFileRepository fileRepository, IOptions<FileHostingInfo> hostingInfo, IFileService fileService) : IMetaRelationModelProviderHandler
{
    public async Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken)
    {
        return (await fileRepository.ListAllDetail(new ListAllFileQuery { Ids = ids }, hostingInfo.Value, cancellationToken)).ToDictionary(s => s.Id, s => (object)s);
    }

    public MetaRelationModel Structure()
    {
        return new MetaRelationModel
        {
            Key = "File",
            Title = "📦 " + AppRes.File,
            TitlePlural = AppRes.Files,
            SubTypes = []
        };
    }

    public async Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var data = await fileRepository.List(new() { Skip = query.Skip, Take = query.Take, Sort = query.Sort, Search = query.Search }, hostingInfo.Value, cancellationToken);
        return data.ToMap(ToModelSummary);
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        return (await fileRepository.ListAll(new() { Ids = ids }, hostingInfo.Value, cancellationToken))
                                    .ToDictionary(s => s.Id, ToModelSummary);
    }

    MetaValueRelationModelSummary ToModelSummary(FileSummary value)
        => new()
        {
            Id = value.Id,
            Title = value.Name,
            Description = value.Url,
            CreatedAt = value.CreatedAt,
        };

    public async Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids)
            await fileService.Delete(id, cancellationToken);

        return ids.Count;
    }
}
