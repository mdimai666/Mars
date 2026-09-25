using System.ComponentModel.DataAnnotations;
using Mars.Cms.Abstractions.Services;
using Mars.Data.Contexts;
using Mars.Data.Entities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mars.MetaModelGenerator;

internal class MetaEntityTypeProvider : IMetaEntityTypeProvider
{
    private readonly IMarsDbContextFactory _dbContextFactory;
    private readonly ILogger<MetaEntityTypeProvider> _logger;

    public MetaEntityTypeProvider(IMarsDbContextFactory dbContextFactory, ILogger<MetaEntityTypeProvider> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    private async Task<Dictionary<string, MetaTypeInfo>> PrepateData()
    {
        using var ef = _dbContextFactory.CreateInstance();

        var postTypeList = await ef.PostTypes
                                .Include(s => s.MetaFields)
                                .AsNoTracking()
                                .Where(s => !s.Disabled)
                                .ToListAsync();

        var dict = new Dictionary<string, MetaTypeInfo>();

        foreach (var postType in postTypeList)
        {
            // Query-поля вычислимые и не имеют хранимой колонки — в Mto-модель не попадают.
            // Ключи-невалидные C#-идентификаторы (цифра/пробел/кириллица) не скомпилировать —
            // поле пропускается, иначе падает компиляция ВСЕХ Mto-моделей.
            var fields = postType.MetaFields!
                                .Where(f => f.Type != EMetaFieldType.Query)
                                .Where(f => SyntaxFacts.IsValidIdentifier(f.Key))
                                .ToArray();

            foreach (var skipped in postType.MetaFields!.Where(f => f.Type != EMetaFieldType.Query && !SyntaxFacts.IsValidIdentifier(f.Key)))
            {
                _logger.LogWarning("MetaField '{Key}' of post type '{TypeName}' is not a valid C# identifier — skipped in Mto model", skipped.Key, postType.TypeName);
            }

            dict.Add(postType.TypeName,
                new MetaTypeInfo(
                    GenSourceCodeMasterHelper.GetNormalizedTypeName(postType.TypeName),
                    typeof(PostEntity),
                    fields,
                    new DisplayAttribute() { Name = postType.Title, Description = "" })
                );
        }

        return dict;
    }

    public async Task<string> GenerateMetaTypesSourceCode()
    {
        var dict = await PrepateData();
        var runtimeMetaTypeCompiler = new RuntimeMetaTypeCompiler();
        var metaModelTypesResolverDict = _getInternalTypes.Concat(dict.ToDictionary(s => $"Post.{s.Key}", s => new MetaModelResolveTypeInfo(true, s.Value.NewClassName, null))).ToDictionary();

        return runtimeMetaTypeCompiler.GenerateFullSourceCode(dict.Values.ToArray(), metaModelTypesResolverDict, true);
    }

    public async Task<IReadOnlyCollection<MtoModelInfo>> GenerateMetaTypes(/*pass external types*/)
    {
        var dict = await PrepateData();
        var runtimeMetaTypeCompiler = new RuntimeMetaTypeCompiler();
        var metaModelTypesResolverDict = _getInternalTypes.Concat(dict.ToDictionary(s => $"Post.{s.Key}", s => new MetaModelResolveTypeInfo(true, s.Value.NewClassName, null))).ToDictionary();

        var compiledTypesDict = await runtimeMetaTypeCompiler.Compile(dict.Values.ToArray(), metaModelTypesResolverDict);

        return dict.Select(s => new MtoModelInfo
        {
            CreatedType = compiledTypesDict[s.Value.NewClassName],
            KeyName = s.Key,
            BaseEntityType = s.Value.BaseEntityType
        }).ToList();
    }

    /// <summary>
    /// посмотреть - можно ли совместить с
    /// <see cref="IMetaRelationModelProviderHandler"/>
    /// </summary>
    // IMetaRelationModelProviderHandler
    static readonly Dictionary<string, MetaModelResolveTypeInfo> _getInternalTypes =
        new()
        {
            ["Post"] = new(false, null, typeof(PostEntity)),
            ["File"] = new(false, null, typeof(FileEntity)),
            ["Feedback"] = new(false, null, typeof(FeedbackEntity)),
            ["NavMenu"] = new(false, null, typeof(NavMenuEntity)),
            ["User"] = new(false, null, typeof(UserEntity)),
        };

}
