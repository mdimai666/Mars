using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.PostCategoryTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Host.Services;

internal class PostCategoryTypeService : IPostCategoryTypeService
{
    private readonly IPostCategoryTypeRepository _postCategoryTypeRepository;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IEventManager _eventManager;
    private readonly IPostCategoryMetaLocator _postCategoryMetaLocator;
    private readonly IValidatorFabric _validatorFabric;

    public PostCategoryTypeService(
        IPostCategoryTypeRepository postCategoryTypeRepository,
        IMetaModelTypesLocator metaModelTypesLocator,
        IEventManager eventManager,
        IPostCategoryMetaLocator postCategoryMetaLocator,
        IValidatorFabric validatorFabric)
    {
        _postCategoryTypeRepository = postCategoryTypeRepository;
        _metaModelTypesLocator = metaModelTypesLocator;
        _eventManager = eventManager;
        _postCategoryMetaLocator = postCategoryMetaLocator;
        _validatorFabric = validatorFabric;
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
        var id = await _postCategoryTypeRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCompiledMetaMtoModels();

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
            MetaRelationModels = _metaModelTypesLocator.AllMetaRelationsStructure().ToResponse()
        };
    }

    public async Task<PostCategoryTypeDetail> Update(UpdatePostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        await _postCategoryTypeRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeUpdate(updated.TypeName), updated.ToSummary());
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<PostCategoryTypeSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync<Guid, DeletePostCategoryTypeQueryValidator>(id, cancellationToken);

        var postCategoryType = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _postCategoryTypeRepository.Delete(id, cancellationToken);

        _postCategoryMetaLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeDelete(postCategoryType.TypeName), postCategoryType);
        _eventManager.TriggerEvent(payload);

        return postCategoryType;
    }

    public async Task<IReadOnlyCollection<PostCategoryTypeSummary>> DeleteMany(DeleteManyPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var postCategoryTypes = await _postCategoryTypeRepository.ListAll(new() { Ids = query.Ids }, cancellationToken);

        await _postCategoryTypeRepository.DeleteMany(query, cancellationToken);

        _postCategoryMetaLocator.InvalidateCompiledMetaMtoModels();

        foreach (var postCategoryType in postCategoryTypes)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.PostCategoryTypeDelete(postCategoryType.TypeName), postCategoryType);
            _eventManager.TriggerEvent(payload);
        }
        return postCategoryTypes;
    }

}
