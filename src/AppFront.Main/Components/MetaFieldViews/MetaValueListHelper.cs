using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>Общая логика полей-списков объектов (мульти-значения Relation)</summary>
public static class MetaValueListHelper
{
    /// <summary>Имя целевого типа поста из ModelName («Post.photo» → «photo»); null, если цель — не тип поста</summary>
    public static string? GetTargetPostTypeName(string modelName)
        => modelName.StartsWith("Post.") ? modelName["Post.".Length..] : null;

    /// <summary>
    /// Действующий режим удаления значения: переопределение поля
    /// или дефолт по видимости типа-цели (компонентные типы удаляются с подтверждением).
    /// </summary>
    public static string ResolveRemoveMode(MetaFieldEditModel meta)
    {
        if (!string.IsNullOrEmpty(meta.RemoveMode)) return meta.RemoveMode;

        if (GetTargetPostTypeName(meta.ModelName) is string typeName
            && Q.Site?.PostTypes.FirstOrDefault(s => s.TypeName == typeName)?.Visibility == PostTypeVisibility.Component)
        {
            return MetaFieldKindCatalog.RemoveModes.DeleteConfirm;
        }

        return MetaFieldKindCatalog.RemoveModes.Unlink;
    }
}
