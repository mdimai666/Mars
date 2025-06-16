using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.MetaFields;

namespace Mars.MetaModelGenerator;

public class MtFieldInfo
{
    string keyName;
    List<string> attributes = new();
    string comment = "";
    Type type;
    public bool TypeRelation => metaField.TypeRelation;
    MetaFieldEntity metaField;

    protected bool IsSystemType => type.Namespace == "System";

    public string friendlyTypeName;
    public string? relationModelTypeFullName;

    public MtFieldInfo(MetaFieldEntity metaField, IReadOnlyDictionary<string, MetaModelResolveTypeInfo>? metaModelTypesResolverDict)
    {

        this.metaField = metaField;
        keyName = metaField.Key;

        var mt = MetaFieldEntity.MetaFieldTypeToType(metaField.Type);
        type = mt;

        friendlyTypeName = IsSystemType ? GenSourceCodeMasterHelper.GetFriendlyTypeName(type.Name) : type.FullName!;

        if (type.IsGenericType)
        {
            friendlyTypeName = GenSourceCodeMasterHelper.GetFormattedName(type);
        }

        if (metaField.TypeRelation)
        {
            friendlyTypeName = GenSourceCodeMasterHelper.GetFormattedName(type);

            if (metaField.Type is EMetaFieldType.File or EMetaFieldType.Image)
            {
                relationModelTypeFullName = typeof(FileEntity).FullName!;
            }
            else
            {
                var resolvedTypeInfo = metaModelTypesResolverDict?.GetValueOrDefault(metaField.ModelName!)
                                                ?? throw new Exception($"metaField.TypeRelation: type '{metaField.ModelName} not found in metaModelTypesResolverDict'");

                relationModelTypeFullName = resolvedTypeInfo.IsMetaModel
                                                ? resolvedTypeInfo.MetaModelClassName
                                                : (resolvedTypeInfo.ExternalType.FullName ?? throw new ArgumentNullException("ExternalType cannot be null"));
            }
        }

        attributes.Add($"[Display(Name=\"{metaField.Title}\")]");

        if (!string.IsNullOrEmpty(metaField.Description))
        {
            comment += ($"\t/// {metaField.Description}");
        }

        if (metaField.Hidden)
        {
            comment += ($"\t/// <i>hidden</i>");
        }

        if (!string.IsNullOrEmpty(comment))
        {
            comment = ($"\t/// <summary>\r\n{comment} \r\n\t/// </summary>");
        }
    }

    public override string ToString()
    {
        string fieldRow;

        if (TypeRelation)
        {
            fieldRow = $$"""
                    public {{friendlyTypeName}} {{keyName}}Id { get; set; }
                    public {{relationModelTypeFullName}}? {{keyName}} { get; set; }
                """;
        }
        else if (metaField.Type == EMetaFieldType.Select)
        {
            fieldRow = $"\tpublic {nameof(MetaFieldVariant)}? {keyName} {{ get; set; }}";
        }
        else if (metaField.Type == EMetaFieldType.SelectMany)
        {
            fieldRow = $"\tpublic {nameof(MetaFieldVariant)}[]? {keyName} {{ get; set; }}";
        }
        else if (metaField.TypeParentable)
        {
            throw new NotImplementedException();
        }
        else
        {
            fieldRow = $"\tpublic {friendlyTypeName} {keyName} {{ get; set; }}";
        }

        return //Primitive field
            (string.IsNullOrEmpty(comment) ? "" : $"{comment}\n")
            + $"\t{attributes.JoinStr("\n\t")}\n"
            + fieldRow;
    }

    public string SelectExpressionRow()
    {
        // see class: MyPostTypeEntity

        if (TypeRelation)
        {
            return $"""
                {keyName}Id = post.MetaValues!
                    .Where(f => f.MetaField.Key == "{keyName}" && f.MetaField.ParentId == Guid.Empty)
                    .Select(f => f.{MetaValueEntity.GetColName(metaField.Type)})
                    .FirstOrDefault()!
                """;
        }
        else if (metaField.Type == EMetaFieldType.Select)
        {
            return $"""
                {keyName} = post.MetaValues!
                    .Where(f => f.MetaField.Key == "{keyName}" && f.MetaField.ParentId == Guid.Empty)
                    .Select(f => f.MetaField.Variants.FirstOrDefault(v => v.Id == f.VariantId))
                    .FirstOrDefault()!
                """;
        }
        else if (metaField.Type == EMetaFieldType.SelectMany)
        {
            return $"""
                {keyName} = post.MetaValues!
                    .Where(f => f.MetaField.Key == "{keyName}" && f.MetaField.ParentId == Guid.Empty)
                    .SelectMany(f => f.MetaField.Variants.Where(v => f.VariantsIds.Contains(v.Id)))
                    .ToArray()
                """;
        }

        return $"""
                {keyName} = post.MetaValues!
                    .Where(f => f.MetaField.Key == "{keyName}" && f.MetaField.ParentId == Guid.Empty)
                    .Select(f => f.{MetaValueEntity.GetColName(metaField.Type)})
                    .FirstOrDefault()!
                """;
    }
}

public record MetaModelResolveTypeInfo(bool IsMetaModel, string? MetaModelClassName, Type? ExternalType);
