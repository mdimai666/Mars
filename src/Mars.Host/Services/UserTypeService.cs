using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.UserTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;
using Mars.Shared.Contracts.UserTypes;

namespace Mars.Host.Services;

internal class UserTypeService : IUserTypeService
{
    private readonly IUserTypeRepository _userTypeRepository;
    private readonly IEventManager _eventManager;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IValidatorFabric _validatorFabric;

    public UserTypeService(
        IUserTypeRepository userTypeRepository,
        IEventManager eventManager,
        IMetaModelTypesLocator metaModelTypesLocator,
        IValidatorFabric validatorFabric)
    {
        _userTypeRepository = userTypeRepository;
        _eventManager = eventManager;
        _metaModelTypesLocator = metaModelTypesLocator;
        _validatorFabric = validatorFabric;
    }

    public Task<UserTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _userTypeRepository.Get(id, cancellationToken);

    public Task<UserTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _userTypeRepository.GetDetail(id, cancellationToken);

    public Task<ListDataResult<UserTypeSummary>> List(ListUserTypeQuery query, CancellationToken cancellationToken)
        => _userTypeRepository.List(query, cancellationToken);

    public Task<PagingResult<UserTypeSummary>> ListTable(ListUserTypeQuery query, CancellationToken cancellationToken)
        => _userTypeRepository.ListTable(query, cancellationToken);

    public async Task<UserTypeDetail> Create(CreateUserTypeQuery query, CancellationToken cancellationToken)
    {
        var id = await _userTypeRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeAdd(created.TypeName), created.ToSummary());//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);

        return created;
    }

    public async Task<UserTypeEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken)
    {
        var userType = await _userTypeRepository.GetDetail(id, cancellationToken) ?? throw new NotFoundException();

        return new UserTypeEditViewModel
        {
            UserType = userType.ToResponse(),
            MetaRelationModels = _metaModelTypesLocator.AllMetaRelationsStructure().ToResponse()
        };
    }

    public async Task<UserTypeDetail> Update(UpdateUserTypeQuery query, CancellationToken cancellationToken)
    {
        await _userTypeRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeUpdate(updated.TypeName), updated.ToSummary());
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<UserTypeSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync<Guid, DeleteUserTypeQueryValidator>(id, cancellationToken);

        var userType = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _userTypeRepository.Delete(id, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeDelete(userType.TypeName), userType);
        _eventManager.TriggerEvent(payload);

        return userType;
    }

    public async Task<IReadOnlyCollection<UserTypeSummary>> DeleteMany(DeleteManyUserTypeQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var userTypes = await _userTypeRepository.ListAll(new() { Ids = query.Ids }, cancellationToken);

        await _userTypeRepository.DeleteMany(query, cancellationToken);

        _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

        foreach (var userType in userTypes)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeDelete(userType.TypeName), userType);
            _eventManager.TriggerEvent(payload);
        }
        return userTypes;
    }

}
