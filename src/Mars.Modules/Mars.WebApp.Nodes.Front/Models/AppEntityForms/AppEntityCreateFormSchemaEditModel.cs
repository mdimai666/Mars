using System.Data;
using Mars.Shared.Models;
using Mars.WebApp.Nodes.Models.AppEntityForms;

namespace Mars.WebApp.Nodes.Front.Models.AppEntityForms;

public class AppEntityCreateFormSchemaEditModel
{
    public string Title { get; set; } = "";
    SourceUri _entityUri = "";
    public SourceUri EntityUri { get => _entityUri; set => UpdateValue(value); }
    public AppEntityCreateFormsBuilderDictionary BuilderDictionary { get; }

    public IReadOnlyDictionary<SourceUri, AppEntityCreateFormSchema> FormsSchemaDict { get; }

    public EntityPropertyFormFieldEditModel[] PropertyBindings { get; set; } = [];

    public AppEntityCreateFormSchemaEditModel(AppEntityCreateFormsBuilderDictionary builderDictionary, CreateAppEntityFromFormCommand currentValues)
    {
        BuilderDictionary = builderDictionary;
        FormsSchemaDict = builderDictionary.Forms.ToDictionary(f => f.EntityUri);

        EntityChanged(currentValues.EntityUri, currentValues.PropertyBindings);
    }

    void EntityChanged(SourceUri entityUri, IReadOnlyCollection<EntityPropertyBinding> propertyBindings)
    {
        _entityUri = entityUri;

        var form = FormsSchemaDict.GetValueOrDefault(EntityUri) ?? FormsSchemaDict.First().Value;
        Title = form.Title;

        PropertyBindings = ItemsToModel(form, propertyBindings);
    }

    void UpdateValue(SourceUri entityUri)
    {
        var propertyBindings = PropertyBindings.Select(s => new EntityPropertyBinding
        {
            IsEvalExpression = s.IsEvalExpression,
            PropertyName = s.PropertyName,
            ValueOrExpression = s.ValueOrExpression,
        }).ToList();

        EntityChanged(entityUri, propertyBindings);
    }

    public static EntityPropertyFormFieldEditModel[] ItemsToModel(AppEntityCreateFormSchema form, IReadOnlyCollection<EntityPropertyBinding> propertySetters)
    {
        var valuesDict = propertySetters.ToDictionary(s => s.PropertyName);
        return form.Properties.Select(s => new EntityPropertyFormFieldEditModel
        {
            PropertyName = s.PropertyName,
            Placeholder = s.Placeholder,
            IsRequired = s.IsRequired,
            Title = s.Title,
            IsEvalExpression = valuesDict.GetValueOrDefault(s.PropertyName)?.IsEvalExpression ?? false,
            ValueOrExpression = valuesDict.GetValueOrDefault(s.PropertyName)?.ValueOrExpression ?? string.Empty,
        }).ToArray();
    }

    public CreateAppEntityFromFormCommand ToRequest()
        => new()
        {
            EntityUri = EntityUri,
            PropertyBindings = PropertyBindings.Select(s => s.ToRequest()).ToArray()
        };
}

public class EntityPropertyFormFieldEditModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Placeholder { get; init; } = string.Empty;
    public bool IsRequired { get; init; }

    public string ValueOrExpression { get; set; } = string.Empty;
    public bool IsEvalExpression { get; set; }

    public EntityPropertyBinding ToRequest()
        => new()
        {
            PropertyName = PropertyName,
            IsEvalExpression = IsEvalExpression,
            ValueOrExpression = ValueOrExpression,
        };
}
