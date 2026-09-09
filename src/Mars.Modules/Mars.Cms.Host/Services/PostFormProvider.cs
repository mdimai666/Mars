using Mars.Cms.Abstractions.Forms;
using Mars.Cms.Abstractions.Services;
using Mars.Core.Exceptions;
using Mars.Forms.Abstractions;
using Mars.Forms.Contracts;

namespace Mars.Cms.Host.Services;

/// <summary>
/// Провайдер формы поста в общем слое форм: отдаёт определение дерева (системные слоты
/// <c>SystemFieldsCatalog</c> + метаполя типа), нормализованное сохранённой раскладкой типа.
/// Значения не читает и не принимает — пост сохраняется существующим типизированным API
/// (см. <see cref="PostFormBuilder.Manifest"/>).
/// </summary>
internal class PostFormProvider(IMetaModelTypesLocator metaModelTypesLocator,
                                IFormDefinitionNormalizer formNormalizer) : IFormDataProvider
{
    public string OwnerModel => PostFormBuilder.OwnerModelWildcard;

    public Task<FormDefinition> GetFormAsync(FormContext context, CancellationToken cancellationToken)
    {
        var typeName = PostFormBuilder.PostTypeName(context.OwnerModel);
        var postType = metaModelTypesLocator.GetPostTypeByName(typeName) ?? throw new NotFoundException();

        return Task.FromResult(PostFormBuilder.Build(postType, formNormalizer, context.Client));
    }
}
