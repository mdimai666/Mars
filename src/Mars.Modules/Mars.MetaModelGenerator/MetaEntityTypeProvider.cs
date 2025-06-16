using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace Mars.MetaModelGenerator;

internal class MetaEntityTypeProvider : IMetaEntityTypeProvider
{
    private readonly IMarsDbContextFactory _dbContextFactory;

    public MetaEntityTypeProvider(IMarsDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task<Dictionary<string, MetaTypeInfo>> PrepateData()
    {
        using var ef = _dbContextFactory.CreateInstance();

        var postTypeList = await ef.PostTypes
                                .Include(s => s.MetaFields)
                                .AsNoTracking()
                                .ToListAsync();

        var dict = new Dictionary<string, MetaTypeInfo>();

        foreach (var postType in postTypeList)
        {
            dict.Add(postType.TypeName,
                new MetaTypeInfo(
                    GenSourceCodeMasterHelper.GetNormalizedTypeName(postType.TypeName),
                    typeof(PostEntity),
                    postType.MetaFields.ToArray(),
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

    public async Task<Dictionary<string, Type>> GenerateMetaTypes(/*pass external types*/)
    {
        var dict = await PrepateData();
        var runtimeMetaTypeCompiler = new RuntimeMetaTypeCompiler();
        var metaModelTypesResolverDict = _getInternalTypes.Concat(dict.ToDictionary(s => $"Post.{s.Key}", s => new MetaModelResolveTypeInfo(true, s.Value.NewClassName, null))).ToDictionary();

        var compiledTypesDict = await runtimeMetaTypeCompiler.Compile(dict.Values.ToArray(), metaModelTypesResolverDict);

        return dict.ToDictionary(s => s.Key, s => compiledTypesDict[s.Value.NewClassName]);
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
