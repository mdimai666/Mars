using System.Data;
using Mars.Shared.Models;
using Mars.WebApp.Nodes.Models.AppEntityForms;

namespace Mars.WebApp.Nodes.Front.Models.AppEntityForms;

public class AppEntityCreateFormSchemaEditModel
{
    public string Title { get; set; }
    public SourceUri EntityUri { get; set; }
    public AppEntityCreateFormsBuilderDictionary BuilderDictionary { get; init; }

    public IReadOnlyDictionary<SourceUri, AppEntityCreateFormSchema> FormsSchemaDict { get; }

    public EntityPropertyFormFieldEditModel[] PropertyBindings { get; set; } = [];
    //public AppEntityCreateFormSchema Form => FormsSchemaDict[EntityUri];

    public AppEntityCreateFormSchemaEditModel(AppEntityCreateFormsBuilderDictionary builderDictionary, CreateAppEntityFromFormCommand currentValues)
    {
        BuilderDictionary = builderDictionary;
        FormsSchemaDict = builderDictionary.Forms.ToDictionary(f => f.EntityUri);
        EntityUri = currentValues.EntityUri;

        var form = FormsSchemaDict.GetValueOrDefault(EntityUri) ?? FormsSchemaDict.First().Value;
        Title = form.Title;

        PropertyBindings = ItemsToModel(currentValues.PropertyBindings, form);

        //TODO: on change EntityUri update PropertyBindings
    }

    public static EntityPropertyFormFieldEditModel[] ItemsToModel(IReadOnlyCollection<EntityPropertyBinding> propertySetters, AppEntityCreateFormSchema form)
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
