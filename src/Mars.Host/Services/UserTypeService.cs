using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Host.Shared.Mappings.UserTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.UserTypes;
using Mars.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Mars.Host.Services;

internal class UserTypeService : IUserTypeService
{
    private readonly IUserTypeRepository _userTypeRepository;
    private readonly IStringLocalizer<AppRes> _stringLocalizer;
    private readonly IEventManager _eventManager;
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public UserTypeService(
        IUserTypeRepository userTypeRepository,
        IStringLocalizer<AppRes> stringLocalizer,
        IEventManager eventManager,
        IMetaModelTypesLocator metaModelTypesLocator)
    {
        _userTypeRepository = userTypeRepository;
        _stringLocalizer = stringLocalizer;
        _eventManager = eventManager;
        _metaModelTypesLocator = metaModelTypesLocator;
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

        //if (created != null)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeAdd(created.TypeName), created.ToSummary());//TODO: сделать явный тип.
            _eventManager.TriggerEvent(payload);
        }

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

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userType = await Get(id, cancellationToken) ?? throw new NotFoundException();

        try
        {
            await _userTypeRepository.Delete(id, cancellationToken);

            _metaModelTypesLocator.InvalidateCompiledMetaMtoModels();

            //if (result.Ok)
            {
                var payload = new ManagerEventPayload(_eventManager.Defaults.UserTypeDelete(userType.TypeName), userType);
                _eventManager.TriggerEvent(payload);
            }
            return UserActionResult.Success();
        }
        catch (Exception ex)
        {
            return UserActionResult.Exception(ex);
        }
    }

    public Task<IReadOnlyCollection<MetaRelationModel>> AllMetaRelationsStructure()
    {
        return Task.FromResult(_metaModelTypesLocator.AllMetaRelationsStructure());
    }

    public Task<ListDataResult<MetaValueRelationModelSummary>> ListMetaValueRelationModels(MetaValueRelationModelsListQuery query, CancellationToken cancellationToken)
    {
        var rootModelName = query.ModelName.Split('.', 2)[0];
        //var models = _metaModelTypesLocator.ListMetaRelationModelProvider();
        ////var userType = _metaModelTypesLocator.GetUserTypeByName(userTypeName) ?? throw new NotFoundException($"post type '{userTypeName}' not found");
        ////var metaField = userType.MetaFields.FirstOrDefault(s=>s.Id == metaFieldId) ?? throw new NotFoundException($"metaFieldId with id '{metaFieldId}' not found");

        var dataProvider = _metaModelTypesLocator.GetMetaRelationModelProvider(rootModelName) ?? throw new NotFoundException($"Provider for type '{query.ModelName}' not found"); ;

        return dataProvider.ListData(query, cancellationToken);
    }

    public Task<IReadOnlyDictionary<Guid, MetaValueRelationModelSummary>> GetMetaValueRelationModels(string modelName, Guid[] ids, CancellationToken cancellationToken)
    {
        var rootModelName = modelName.Split('.', 2)[0];
        var dataProvider = _metaModelTypesLocator.GetMetaRelationModelProvider(rootModelName) ?? throw new NotFoundException($"Provider for type '{modelName}' not found"); ;

        return dataProvider.GetIds(modelName, ids, cancellationToken);
    }
}
