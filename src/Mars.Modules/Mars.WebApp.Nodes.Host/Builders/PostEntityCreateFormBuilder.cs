using System.Data;
using AngleSharp.Common;
using Mars.Core.Extensions;
using Mars.Core.Utils;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Templators;
using Mars.Host.Shared.Utils;
using Mars.Nodes.Core;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Models;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Mars.WebApp.Nodes.Front.Models.AppEntityForms;
using Mars.WebApp.Nodes.Models.AppEntityForms;

namespace Mars.WebApp.Nodes.Host.Builders;

public class PostEntityCreateFormBuilder : IAppEntityCreateFormBuilder
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IPostService _postService;
    private readonly IRequestContext _requestContext;
    private readonly IUserRepository _userRepository;

    public PostEntityCreateFormBuilder(IMetaModelTypesLocator metaModelTypesLocator,
                                        IPostService postService,
                                        IRequestContext requestContext,
                                        IUserRepository userRepository)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _postService = postService;
        _requestContext = requestContext;
        _userRepository = userRepository;
    }

    public AppEntityCreateFormSchema CreateForm(SourceUri entityUri)
    {
        var subType = entityUri.SegmentsCount > 1 ? entityUri[1] : "post";

        EntityPropertyFormField create(string propertyName, string? title = null, bool isRequired = false, string placeholder = "")
            => new()
            { Title = title ?? propertyName, PropertyName = propertyName, IsRequired = isRequired, Placeholder = placeholder };

        var rootItems = new List<EntityPropertyFormField>()
        {
            create(nameof(CreatePostJsonQuery.Id), placeholder: "auto" ),
            create(nameof(CreatePostJsonQuery.Title), title: AppRes.Title, isRequired: true, placeholder: "auto title-<guid>"),
            create(nameof(CreatePostJsonQuery.Type), title: AppRes.TypeName, placeholder: subType ),
            create(nameof(CreatePostJsonQuery.Slug), title: AppRes.Slug, placeholder: "auto slug-<guid>" ),
            create(nameof(CreatePostJsonQuery.Tags), title: AppRes.Tags, placeholder: "tag1,tag2..." ),
            create(nameof(CreatePostJsonQuery.Content), title: AppRes.Content),
            create(nameof(CreatePostJsonQuery.Status), title: AppRes.Status, placeholder: "auto default" ),
            create(nameof(CreatePostJsonQuery.Excerpt), title: AppRes.Excerpt),
            create(nameof(CreatePostJsonQuery.LangCode), title: AppRes.Language),
        };

        var postType = _metaModelTypesLocator.GetPostTypeByName(subType);

        var metaItems = postType.MetaFields.Where(s => !s.Disabled)
                                            .Select(f => create("meta." + f.Key, title: f.Title))
                                            .ToList();

        return new()
        {
            EntityUri = entityUri,
            Title = postType.Title,
            Properties = rootItems.Concat(metaItems).ToList(),
        };
    }

    public IReadOnlyCollection<AppEntityCreateFormSchema> AllForms()
    {
        var postTypes = _metaModelTypesLocator.PostTypesDict();

        return postTypes.Values.Select(postType => CreateForm(new SourceUri($"/Post/{postType.TypeName}"))).ToList();
    }

    public async Task<IBasicEntity> Save(CreateAppEntityFromFormCommand form, NodeMsg input, CancellationToken cancellationToken)
    {
        var subType = form.EntityUri.SegmentsCount > 1 ? form.EntityUri[1] : "post";
        //var dict = new Dictionary<string,string>(form.PropertySetters.Count);
        var f = form.PropertyBindings.ToDictionary(s => s.PropertyName);

        var ppt = new XInterpreter(null, input.AsFullDict());

        T? value<T>(string propertyName, Func<string, T> literallyValue, bool asNullIfEmpty = false)
        {
            var prop = f[propertyName];
            if (asNullIfEmpty && string.IsNullOrEmpty(prop.ValueOrExpression)) return default;
            return prop.IsEvalExpression
                ? ppt.Get.Eval<T>(prop.ValueOrExpression)
                : literallyValue(prop.ValueOrExpression);
        }

        string valueString(string propertyName)
        {
            var prop = f[propertyName];
            if (string.IsNullOrEmpty(prop.ValueOrExpression)) return prop.ValueOrExpression;
            return prop.IsEvalExpression
                ? ppt.Get.Eval<string>(prop.ValueOrExpression)
                : prop.ValueOrExpression;
        }

        var postType = _metaModelTypesLocator.GetPostTypeByName(subType)
                            ?? throw new InvalidOperationException($"postType '{subType}' not found");

        var postTypeMetaFieldsDict = postType.MetaFields.ToDictionary(s => s.Key);

        var isStatusSupport = postType.EnabledFeatures.Contains(PostTypeConstants.Features.Status);
        var status = (isStatusSupport ? postType.PostStatusList.FirstOrDefault()?.Slug : null) ?? "";

        async Task<Guid> defaultUserId() => _requestContext.User?.Id
                                        ?? (await _userRepository.List(new() { Roles = ["Admin"] }, cancellationToken)).Items.First().Id;

        var metaDict = form.PropertyBindings.Where(s => s.PropertyName.StartsWith("meta."))
                                            .ToDictionary(s => s.PropertyName.Split("meta.", 2)[1],
                                                                s =>
                                                                {
                                                                    var metaFieldKey = s.PropertyName.Split("meta.", 2)[1];
                                                                    var mft = postTypeMetaFieldsDict[metaFieldKey].Type;
                                                                    var valueType = MetaFieldUtils.MetaFieldTypeToType(mft);

                                                                    if (s.IsEvalExpression)
                                                                        return ppt.Get.Eval(s.ValueOrExpression, valueType);
                                                                    else
                                                                        return MetaFieldUtils.ConvertStringValueToMetaTypeObject(mft, s.ValueOrExpression);
                                                                });
        var guid = Guid.NewGuid();

        var query = new CreatePostQuery
        {
            Id = value<Guid?>(nameof(CreatePostJsonQuery.Id), v => new Guid(v), asNullIfEmpty: true) ?? guid,
            Title = valueString(nameof(CreatePostJsonQuery.Title)).AsNullIfEmpty() ?? $"title-{guid}",
            Slug = valueString(nameof(CreatePostJsonQuery.Slug)).AsNullIfEmpty() ?? $"slug-{guid}",
            Type = valueString(nameof(CreatePostJsonQuery.Type)).AsNullIfEmpty() ?? subType,
            Tags = value<string[]>(nameof(CreatePostJsonQuery.Tags), v => v.Split(',', StringSplitOptions.TrimEntries).ToArray(), asNullIfEmpty: true) ?? [],
            Content = valueString(nameof(CreatePostJsonQuery.Content)),
            Excerpt = valueString(nameof(CreatePostJsonQuery.Excerpt)).AsNullIfEmpty(),
            LangCode = valueString(nameof(CreatePostJsonQuery.LangCode)),
            Status = valueString(nameof(CreatePostJsonQuery.Status)).AsNullIfEmpty() ?? status,
            //UserId = value<Guid?>(nameof(CreatePostJsonQuery.UserId), v => new Guid(v), asNullIfEmpty: true) ?? await defaultUserId(),
            UserId = await defaultUserId(),
            MetaValues = CreateStringMetaValuesToModifyDto(metaDict, postType.MetaFields, postType.TypeName),
        };

        var post = await _postService.Create(query, cancellationToken);

        return post;
    }

    internal static IReadOnlyCollection<ModifyMetaValueDetailQuery> CreateStringMetaValuesToModifyDto(IReadOnlyDictionary<string, object>? meta,
                                                                                                IReadOnlyCollection<MetaFieldDto> metaFields,
                                                                                                string postTypeName)
    {
        if (meta is null) return [];

        var mfDict = metaFields.ToDictionary(s => s.Key);

        var diff = DiffList.FindDifferences(mfDict.Keys.ToList(), meta.Keys.ToList());

        if (diff.ToAdd.Any())
        {
            throw new InvalidOperationException($"fields '{diff.ToAdd.JoinStr(",")}' not exist for '{postTypeName}'");
        }

        var intersectKeysOnly = meta.Keys.Intersect(mfDict.Keys);
        var appendValues = new List<ModifyMetaValueDetailQuery>();

        foreach (var key in intersectKeysOnly)
        {
            var jsonVal = meta[key];
            var blank = ModifyMetaValueDetailQuery.GetBlank(mfDict[key]);
            var modified = MetaFieldUtils.MetaValueFromObject(blank, jsonVal);
            appendValues.Add(modified);
        }

        return appendValues;
    }

}
