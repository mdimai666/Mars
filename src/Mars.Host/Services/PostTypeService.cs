using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Services;

internal class PostTypeService : IPostTypeService
{
    private readonly IPostTypeRepository _postTypeRepository;
    private readonly IEventManager _eventManager;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IValidatorFactory _validatorFactory;

    public PostTypeService(
        IPostTypeRepository postTypeRepository,
        IEventManager eventManager,
        IMetaModelTypesLocator metaModelTypesLocator,
        IServiceProvider serviceProvider,
        IValidatorFactory validatorFactory)
    {
        _postTypeRepository = postTypeRepository;
        _eventManager = eventManager;
        _metaModelTypesLocator = metaModelTypesLocator;
        _serviceProvider = serviceProvider;
        _validatorFactory = validatorFactory;
    }

    public Task<PostTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _postTypeRepository.Get(id, cancellationToken);

    public Task<PostTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _postTypeRepository.GetDetail(id, cancellationToken);

    public Task<ListDataResult<PostTypeSummary>> List(ListPostTypeQuery query, CancellationToken cancellationToken)
        => _postTypeRepository.List(query, cancellationToken);

    public Task<PagingResult<PostTypeSummary>> ListTable(ListPostTypeQuery query, CancellationToken cancellationToken)
        => _postTypeRepository.ListTable(query, cancellationToken);

    public async Task<PostTypeDetail> Create(CreatePostTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _postTypeRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostTypeAdd(created.TypeName), created.ToSummary());//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<PostTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var postType = await _postTypeRepository.GetDetail(id, cancellationToken) ?? throw new NotFoundException();

        return new PostTypeEditViewModel
        {
            PostType = postType.ToResponse(),
            MetaRelationModels = _metaModelTypesLocator.AllMetaRelationsStructure(_serviceProvider).ToResponse()
        };
    }

    public async Task<PostTypeDetail> Update(UpdatePostTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        await _postTypeRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostTypeUpdate(updated.TypeName), updated.ToSummary());
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<PostTypeSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync<Guid, DeletePostTypeQueryValidator>(id, cancellationToken);

        var postType = await Get(id, cancellationToken);

        await _postTypeRepository.Delete(id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostTypeDelete(postType.TypeName), postType);
        _eventManager.TriggerEvent(payload);
        return postType;
    }

    public async Task<IReadOnlyCollection<PostTypeSummary>> DeleteMany(DeleteManyPostTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var postTypes = await _postTypeRepository.ListAllIds(query.Ids, cancellationToken);

        await _postTypeRepository.DeleteMany(query, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        foreach (var postType in postTypes)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostTypeDelete(postType.TypeName), postType);
            _eventManager.TriggerEvent(payload);
        }
        return postTypes;
    }

    public Task<IReadOnlyCollection<MetaRelationModel>> AllMetaRelationsStructure()
    {
        return Task.FromResult(_metaModelTypesLocator.AllMetaRelationsStructure(_serviceProvider));
    }

    public Task<ListDataResult<MetaValueRelationModelSummary>> ListMetaValueRelationModels(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var rootModelName = query.ModelName.Split('.', 2)[0];
        //var models = _metaModelTypesLocator.ListMetaRelationModelProvider();
        ////var postType = _metaModelTypesLocator.GetPostTypeByName(postTypeName) ?? throw new NotFoundException($"post type '{postTypeName}' not found");
        ////var metaField = postType.MetaFields.FirstOrDefault(s=>s.Id == metaFieldId) ?? throw new NotFoundException($"metaFieldId with id '{metaFieldId}' not found");

        var dataProvider = _metaModelTypesLocator.GetMetaRelationModelProvider(rootModelName, _serviceProvider)
                                ?? throw new NotFoundException($"Provider for type '{query.ModelName}' not found"); ;

        return dataProvider.ListData(query, cancellationToken);
    }

    public Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetMetaValueRelationModels(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        var rootModelName = modelName.Split('.', 2)[0];
        var dataProvider = _metaModelTypesLocator.GetMetaRelationModelProvider(rootModelName, _serviceProvider)
                                ?? throw new NotFoundException($"Provider for type '{modelName}' not found"); ;

        return dataProvider.GetIds(modelName, ids, cancellationToken);
    }

    public async Task UpdatePresentation(UpdatePostTypePresentationQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        await _postTypeRepository.UpdatePresentation(query, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();
    }

    public PostTypePresentationEditViewModel? GetPresentationEditModel(Guid id, CancellationToken cancellationToken)
    {
        var postType = _metaModelTypesLocator.GetPostTypeById(id);
        if (postType == null) return null;

        return new()
        {
            PostType = postType.ToSummaryResponse(),
            Presentation = postType.Presentation.ToResponse(),
        };
    }

}
