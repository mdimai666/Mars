using Mars.Cms.Abstractions.Dto.PostCategoryTypes;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Cms.Abstractions.Mappings.PostCategoryTypes;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Contracts.PostCategoryTypes;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Mars.Server.Abstractions.Validators;

namespace Mars.Cms.Host.Services;

internal class PostCategoryTypeService : IPostCategoryTypeService
{
    private readonly IPostCategoryTypeRepository _postCategoryTypeRepository;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IPostCategoryMetaLocator _postCategoryMetaLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IValidatorFactory _validatorFactory;

    public PostCategoryTypeService(
        IPostCategoryTypeRepository postCategoryTypeRepository,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IPostCategoryMetaLocator postCategoryMetaLocator,
        IServiceProvider serviceProvider,
        IValidatorFactory validatorFactory)
    {
        _postCategoryTypeRepository = postCategoryTypeRepository;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _postCategoryMetaLocator = postCategoryMetaLocator;
        _serviceProvider = serviceProvider;
        _validatorFactory = validatorFactory;
    }

    public Task<PostCategoryTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.Get(id, cancellationToken);

    public Task<PostCategoryTypeSummary?> GetByName(string typeName, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.GetByName(typeName, cancellationToken);

    public Task<PostCategoryTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.GetDetail(id, cancellationToken);

    public Task<PostCategoryTypeDetail?> GetDetailByName(string typeName, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.GetDetailByName(typeName, cancellationToken);

    public Task<ListDataResult<PostCategoryTypeSummary>> List(ListPostCategoryTypeQuery query, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.List(query, cancellationToken);

    public Task<PagingResult<PostCategoryTypeSummary>> ListTable(ListPostCategoryTypeQuery query, CancellationToken cancellationToken)
        => _postCategoryTypeRepository.ListTable(query, cancellationToken);

    public async Task<PostCategoryTypeDetail> Create(CreatePostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _postCategoryTypeRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCache();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeAdd(created.TypeName), created.ToSummary());//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<PostCategoryTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var postCategoryType = await _postCategoryTypeRepository.GetDetail(id, cancellationToken) ?? throw new NotFoundException();

        return new PostCategoryTypeEditViewModel
        {
            PostCategoryType = postCategoryType.ToResponse(),
            MetaRelationModels = _metaModelTypesLocator.AllMetaRelationsStructure(_serviceProvider).ToResponse()
        };
    }

    public async Task<PostCategoryTypeDetail> Update(UpdatePostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        await _postCategoryTypeRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCache();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeUpdate(updated.TypeName), updated.ToSummary());
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<PostCategoryTypeSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync<Guid, DeletePostCategoryTypeQueryValidator>(id, cancellationToken);

        var postCategoryType = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _postCategoryTypeRepository.Delete(id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCache();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeDelete(postCategoryType.TypeName), postCategoryType);
        _eventManager.TriggerEvent(payload);

        return postCategoryType;
    }

    public async Task<IReadOnlyCollection<PostCategoryTypeSummary>> DeleteMany(DeleteManyPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFactory.ValidateAndThrowAsync(query, cancellationToken);

        var postCategoryTypes = await _postCategoryTypeRepository.ListAll(new() { Ids = query.Ids }, cancellationToken);

        await _postCategoryTypeRepository.DeleteMany(query, cancellationToken);

        _postCategoryMetaLocator.InvalidateCache();

        foreach (var postCategoryType in postCategoryTypes)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeDelete(postCategoryType.TypeName), postCategoryType);
            _eventManager.TriggerEvent(payload);
        }
        return postCategoryTypes;
    }

}
