using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Хост группы метаполей: держит контроллер определений (<see cref="MetaFieldDefinitions"/>) и
/// передаёт общему редактору состав полей, защиту feature-полей и доменные компоненты метаполей.
/// </summary>
public partial class MetaFieldsGroup
{
    /// <summary>Поля владельца — хранилище; определения строятся из него</summary>
    [Parameter, EditorRequired] public List<MetaFieldEditModel> Fields { get; set; } = default!;

    /// <summary>Доступные цели связей (пикеры Relation и вычислимого поля)</summary>
    [Parameter] public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = [];

    /// <summary>Ключ поля, на который указывает фича типа (картинка поста): защищено от удаления и смены типа</summary>
    [Parameter] public string? FeatureFieldKey { get; set; }

    /// <summary>Фича «Контент» включена: поле с фиксированным ключом защищено и не переименовывается</summary>
    [Parameter] public bool ContentFeatureEnabled { get; set; }

    /// <summary>Поле переименовано (старый ключ, новый ключ) — владелец двигает указатель фичи</summary>
    [Parameter] public Action<string, string>? OnFieldKeyRenamed { get; set; }

    [Inject] IFormEditorLocator EditorLocator { get; set; } = default!;

    MetaFieldDefinitions Definitions { get; set; } = default!;

    List<MetaFieldEditModel>? _source;

    protected override void OnParametersSet()
    {
        if (Definitions is null || !ReferenceEquals(_source, Fields))
        {
            _source = Fields;
            Definitions = new MetaFieldDefinitions(Fields, EditorLocator);
        }

        Definitions.FeatureFieldKey = FeatureFieldKey;
        Definitions.ContentFeatureEnabled = ContentFeatureEnabled;
        Definitions.OnFieldKeyRenamed = OnFieldKeyRenamed;

        // состав строк и защита полей зависят от фич типа; пересборка сохраняет экземпляры определений,
        // поэтому состояние строк (раскрыта/свёрнута) не сбрасывается
        Definitions.Rebuild();
    }
}
