using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;

namespace Mars.Host.Handlers;

internal class FeedbackRelationModelProviderHandler(IFeedbackRepository feedbackRepository) : IMetaRelationModelProviderHandler
{
    public async Task<Dictionary<Guid, object>> ListHandle(IReadOnlyCollection<Guid> ids, string modelName, CancellationToken cancellationToken)
    {
        return (await feedbackRepository.ListAllDetail(new() { Ids = ids }, cancellationToken)).ToDictionary(s => s.Id, s => (object)s);
    }

    public MetaRelationModel Structure()
    {
        return new MetaRelationModel
        {
            Key = "Feedback",
            Title = "✉️ " + AppRes.Feedback,
            TitlePlural = AppRes.Feedbacks,
            SubTypes = []
        };
    }

    public async Task<ListDataResult<MetaValueRelationModelSummary>> ListData(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var data = await feedbackRepository.List(new() { Skip = query.Skip, Take = query.Take, Sort = query.Sort, Search = query.Search }, cancellationToken);
        return data.ToMap(ToModelSummary);
    }

    public async Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetIds(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        return (await feedbackRepository.ListAll(new() { Ids = ids }, cancellationToken))
                                    .ToDictionary(s => s.Id, ToModelSummary);
    }

    MetaValueRelationModelSummary ToModelSummary(FeedbackSummary value)
        => new()
        {
            Id = value.Id,
            Title = value.Title,
            Description = value.Type,
            CreatedAt = value.CreatedAt,
        };

    public Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return feedbackRepository.DeleteMany(ids, cancellationToken);
    }
}
