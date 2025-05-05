using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.MetaFields;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.Host.Services;

internal class MetaFieldMaterializerService : IMetaFieldMaterializerService
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly FileHostingInfo _hostingInfo;

    public MetaFieldMaterializerService(IMetaModelTypesLocator metaModelTypesLocator, IServiceProvider serviceProvider, IOptions<FileHostingInfo> hostingInfo)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _serviceProvider = serviceProvider;
        _hostingInfo = hostingInfo.Value;
    }

    IMetaRelationModelProviderHandler? ResolveProvider(string modelName)
    {
        return _serviceProvider.GetKeyedService<IMetaRelationModelProviderHandler>(modelName);
    }

    public async Task<Dictionary<Guid, MetaFieldRelatedFillDictValue>> GetModelByIds(MetaFieldMaterializerQuery query, CancellationToken cancellationToken)
    {
        string modelName = query.ModelName.Split('.', 2)[0];

        var provider = ResolveProvider(modelName)
            ?? throw new UserActionException($"{nameof(IMetaRelationModelProviderHandler)} for '{modelName}' not registered"); ;
        var items = await provider.ListHandle(query.Ids, query.ModelName, cancellationToken);

        return items.ToDictionary(s => s.Key, s => new MetaFieldRelatedFillDictValue
        {
            ModelId = s.Key,
            Type = query.Type,
            ModelName = query.ModelName,
            ModelDto = s.Value,
        });
    }

    public async Task<MetaFieldRelatedFillDict> GetFillContext(IEnumerable<MetaValueDto> metaValues, CancellationToken cancellationToken)
    {
        var dict = new MetaFieldRelatedFillDict(metaValues.Where(s => s.ModelId != null)
                            .Select(s => new { key = (s.Type, s.MetaField.ModelName, s.ModelId!.Value), value = s })
                            .DistinctBy(s => s.key)
                            .Select(s => new KeyValuePair<(MetaFieldType type, string? modelName, Guid ModelId), MetaFieldRelatedFillDictValue>(
                                s.key,
                                new MetaFieldRelatedFillDictValue
                                {
                                    Type = s.value.Type,
                                    ModelId = s.value.ModelId!.Value,
                                    ModelName = s.value.MetaField.ModelName,
                                }
                            )));

        // 1. append files
        {
            var files = dict.Values.Where(s => s.Type is MetaFieldType.File or MetaFieldType.Image);
            var filesIds = files.Select(s => s.ModelId).ToList();

            var fileRepository = _serviceProvider.GetRequiredService<IFileRepository>();

            var filesDict = (await fileRepository.ListAllDetail(new ListAllFileQuery { Ids = filesIds, IsImage = false }, _hostingInfo, cancellationToken))
                                    .ToDictionary(s => s.Id);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var item in files)
            {
                item.ModelDto = filesDict.GetValueOrDefault(item.ModelId);
            }
        }
        // 2. append Related model objects
        {
            var related = dict.Values.Where(s => s.Type is MetaFieldType.Relation);
            var grouped = related.GroupBy(s => s.ModelName);
            foreach (var group in grouped)
            {
                var modelRelType = group.Key!;
                var ids = group.Select(s => s.ModelId).ToList();

                var objectsDict = await GetModelByIds(
                    new MetaFieldMaterializerQuery { Ids = ids, Type = MetaFieldType.Relation, ModelName = modelRelType },
                    cancellationToken);

                foreach (var obj in group)
                {
                    obj.ModelDto = objectsDict.GetValueOrDefault(obj.ModelId)?.ModelDto;
                }
            }
        }

        return dict;
    }
}

