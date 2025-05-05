using Mars.Shared.Contracts.MetaFields;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class FormMetaField
{
    //ICollection<MetaField> _model;
    //[Parameter]
    //public ICollection<MetaField> Model
    //{
    //    get => _model;
    //    set
    //    {
    //        if (_model == value) return;
    //        _model = value;
    //        ModelChanged.InvokeAsync(_model);
    //    }
    //}

    //[Parameter] public EventCallback<ICollection<MetaField>> ModelChanged { get; set; }

    [CascadingParameter]
    public ICollection<MetaFieldEditModel> Model { get; set; } = default!;

    [CascadingParameter]
    public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = default!;


    [Parameter]
    public Guid ParentId { get; set; } = Guid.Empty;

    //[CascadingParameter]
    //CascadeStateChanger cascadeStateChanger { get; set; } = default!;

    //static Dictionary<string, string> modelsDict = new();
    //static List<ModelTypeInfo> modelsDict2 = new();

    [Inject] IMarsWebApiClient client { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        //modelsDict = MetaField.MetaModelsDictionary(Q.Site.PostTypes);
        //modelsDict = Q.Site.PostTypes.ToDictionary(s => s.TypeName, x => x.Title);

        Load();
    }

    void Load()
    {
        //if (modelsDict2.Count == 0)
        //{
        //    List<ModelTypeInfo> res = await client.PostType.RegisteredPostTypes();
        //    modelsDict2 = res;
        //    var postSubtypes = modelsDict2.First(s => s.Name == nameof(Post)).SubTypes!.ToDictionary(s => s.Name, s => s.Title)!;
        //    modelsDict = postSubtypes.Concat(modelsDict2.ToDictionary(s => s.Name, s => s.Title)).ToDictionary(s => s.Key, s => s.Value);
        //}
    }

    public static List<MetaFieldEditModel> TestMetaFieldsTemplates() => new()
    {
        new MetaFieldEditModel { Title = "bool1", Description = "desc", Key = "key1", MaxValue = 10, Type = MetaFieldType.Bool },
        new MetaFieldEditModel { Title = "string1", Description = "desc", Key = "key2", MaxValue = 10, Type = MetaFieldType.String },
        new MetaFieldEditModel { Title = "text1", Description = "", Key = "key3", MaxValue = 10, Type = MetaFieldType.Text },
        new MetaFieldEditModel { Title = "int1", Description = "desc", Key = "key4", MaxValue = 10, Type = MetaFieldType.Int },
        new MetaFieldEditModel { Title = "select1", Description = "desc", Key = "key5",
        Type = MetaFieldType.Select,
        Variants = new (){
            new (){ Title = "val1", Tags=[ "tag1", "tag2" ], Value=5 },
            new (){ Title = "val2" },
        } },
        new MetaFieldEditModel { Title = "selectMany2", Description = "desc", Key = "key5",
        Type = MetaFieldType.SelectMany,
        Variants = new(){
            new (){ Title = "checkbox1" },
            new (){ Title = "checkbox2" },
            new (){ Title = "checkbox3" },
        } },
    };

    void OnFinish()
    {

    }

    public void UpdateState()
    {
        StateHasChanged();
    }

    void OnDelete(MetaFieldEditModel field)
    {
        Model.Remove(field);
        //_model = Model.ExceptBy(s=>s.id == field.Id).ToList();
        //ModelChanged.InvokeAsync(_model);
        Console.WriteLine("del2");
    }

    void AddNewField(Guid parentId)
    {
        int order = CountChilds(parentId) > 0 ? (Childs(parentId).Max(s => s.Order) + 1) : 1;
        Model.Add(NewField(order, parentId));
        //cascadeStateChanger?.StateChange();
        //StateHasChanged();
    }

    public static MetaFieldEditModel NewField(int order, Guid parentId)
    {
        return new MetaFieldEditModel
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Order = order,
        };
    }

    IEnumerable<MetaFieldEditModel> Childs(Guid id)
    {
        var childs = Model.Where(s => s.ParentId == id);
        return childs;//.Any() ? childs : new List<MetaField>();
    }

    int CountChilds(Guid id)
    {
        return Model.Count(s => s.ParentId == id);
    }
}
