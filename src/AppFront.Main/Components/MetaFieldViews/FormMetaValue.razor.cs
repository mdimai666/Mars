using Mars.Core.Extensions;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

public partial class FormMetaValue
{
    [CascadingParameter] List<MetaValueEditModel> MetaValues { get; set; } = default!;

    [CascadingParameter] List<MetaFieldEditModel> MetaFields { get; set; } = default!;

    //public Dictionary<string, string> ModelsDict = default;

    //[Inject] public FormService FormService { get; set; } = default!;

    [Parameter] public bool Vertical { get; set; }
    [Parameter] public bool Client { get; set; }


    /*
    public static List<MetaValue> TestMetaValues() => new()
    {
        new MetaValue{
            MetaField = new MetaField { Title = "bool1", Description = "desc", Key = "key1", MaxValue = 10, Type = EMetaFieldType.Bool, IsNullable = false },
            Type = EMetaFieldType.Bool,
            Bool = true,
        },
        new MetaValue{
            MetaField = new MetaField { Title = "boolNullable", Description = "desc", Key = "key1", MaxValue = 10, Type = EMetaFieldType.Bool, IsNullable = true },
            Type = EMetaFieldType.Bool,
            NULL = true,
        },
        new MetaValue{
            MetaField = new MetaField { Title = "string1", Description = "desc", Key = "key2", MaxValue = 10, Type = EMetaFieldType.String },
            Type = EMetaFieldType.String,
            StringShort = "short string",
        },
        new MetaValue{
            MetaField = new MetaField { Title = "text1", Description = "", Key = "key3", MaxValue = 10, Type = EMetaFieldType.Text },
            Type = EMetaFieldType.Text,
            StringText = "<h3>long 2gb text</h3>"
        },
        new MetaValue{
            MetaField = new MetaField { Title = "int1", Description = "desc", Key = "key4", MaxValue = 10, Type = EMetaFieldType.Int },
            Type = EMetaFieldType.Int,
            Int = 5
        },
        new MetaValue{
            MetaField =
                new MetaField { Title = "select1", Description = "desc", Key = "key5",
                Type = EMetaFieldType.Select,
                Variants = new (){
                    new (){ Id=Guid.Parse("e2b4bf69-9396-4de5-a75c-30548b37c489"), Title = "val1", Tags=new[]{ "tag1", "tag2" }, Value=5 },
                    new (){ Id=Guid.Parse("85e395b9-3258-48e4-9380-2fbee33bc5da") ,Title = "val2" },
                } },
            Type = EMetaFieldType.Select,
            VariantId = Guid.Parse("85e395b9-3258-48e4-9380-2fbee33bc5da"),
        },
        new MetaValue{
            MetaField =
                new MetaField { Title = "selectMany2", Description = "desc", Key = "key5",
                Type = EMetaFieldType.SelectMany,
                Variants = new(){
                    new (){ Id=Guid.Parse("4bae7eae-0871-4375-b407-df32c12c4183"), Title = "checkbox1" },
                    new (){ Id=Guid.Parse("3fc7d309-d423-4ba0-8e80-4170ce82d348"), Title = "checkbox2" },
                    new (){ Id=Guid.Parse("6317b2cd-b4a7-406b-9bf0-55978e74659b"), Title = "checkbox3" },
                    new (){ Id=Guid.Parse("0b6a6bc9-475e-4a50-824e-42292e0d7b82"), Title = "checkbox4" },
                } },
            Type = EMetaFieldType.SelectMany,
            VariantsIds = new Guid[]{
                Guid.Parse("3fc7d309-d423-4ba0-8e80-4170ce82d348"),
                Guid.Parse("6317b2cd-b4a7-406b-9bf0-55978e74659b")
            }

        }
    };
    */


    public void AddNewField(MetaValueEditModel parent)
    {
        var listMeta = parent.MetaField;
        //int order = CountChilds(meta.Id) > 0 ? (Childs(meta.Id).Max(s => s.MetaField.Order) + 1) : 1;

        var childs = Childs(listMeta.Id);

        var items = new List<MetaValueEditModel>();

        //there known MetaField
        Console.WriteLine(childs.Select(s => s.MetaField.Title).JoinStr(","));

        foreach (var f in childs)
        {

            var meta = f.MetaField;

            var item = new MetaValueEditModel()
            {
                ParentId = listMeta.Id,
                NULL = meta.IsNullable,
                //Order = order,
                //MetaFieldId = meta.Id,
                MetaField = meta,
                //Type = meta.Type
            };
            items.Add(item);
        }
        MetaValues = MetaValues.Concat(items).ToList();

        //cascadeStateChanger?.StateChange();
        //StateHasChanged();
    }

    public IEnumerable<MetaValueEditModel> Childs(Guid id)
    {
        var childs = MetaValues.Where(s => s.ParentId == id);
        return childs;//.Any() ? childs : new List<MetaField>();
    }

    public int CountChilds(Guid id)
    {
        return MetaValues.Count(s => s.ParentId == id);
    }

    public void AddNewFieldGroupToIterator(MetaFieldEditModel metaList, int index)
    {
        //var existDict = MetaValues.ToDictionary(s => s.MetaFieldId);

        //var items = MetaField.FieldsBlank(default, default, MetaFields, meta.Id);
        var metaFields = MetaFields.Where(s => s.ParentId == metaList.Id).ToList();
        //Console.WriteLine("metaFields= " + metaFields.Select(s=>s.Title).JoinStr(";"));
        var items = MetaFieldEditModel.MetaListGetNewGroupChild(metaList, metaFields, index);
        //Console.WriteLine("items= " + items.Select(s=>s.MetaField.Title).JoinStr("; "));
        //_logger.LogWarning("items" + System.Text.Json.JsonSerializer.Serialize(items));

        //MetaValues = MetaValues.Concat(items).ToList();
        //foreach (var a in items)
        //{
        //    MetaValues.Add(a);
        //}
        MetaValues.AddRange(items);
        //cascadeStateChanger?.StateChange();

    }

    

    //public List<Guid> RemoveList { get; set; } = new();

    public void RemoveIteratorItem(MetaFieldEditModel metaList, int index)
    {
        var values = MetaValues.Where(s => s.MetaField.ParentId == metaList.Id && s.Index == index);
        ////Console.WriteLine($"{metaList.Key} {index} = count({values.Count()})");

        //MetaValues = MetaValues.Except(values).ToList();
        ////A1ntDesign.Form<Post>
        ////antForm.Validate();

        ////RemoveList.AddRange(values.Select(x => x.Id));

        ////MetaValues = MetaValues.ToList();

        //////foreach (var v in values)
        //////{
        //////    MetaValues.Remove(v);
        //////}

        //////var q = new ObservableCollection<MetaValue>();

        //////MetaValues = q;

        MetaValues = values.ToList();
        //values.ForEach(s => s.MarkForDelete = true);

    }

    public void ListMoveUp(MetaFieldEditModel metaList, int index)
    {
        var count = MetaValues.Count(s => s.MetaField.ParentId == metaList.Id);
        if (count == 1) return;

        var childs = MetaValues.Where(s => s.MetaField.ParentId == metaList.Id);

        int minIndex = childs.Min(s => s.Index);
        int maxIndex = childs.Max(s => s.Index);

        if (index == minIndex) return;

        //Тут индексы не нормализованы
        var indexList = childs.Select(s => s.Index).Distinct().OrderBy(s => s).ToList();

        //Console.WriteLine($"list = {indexList.Select(s=>s.ToString()).JoinStr(",")}");

        int currentIndex = indexList.IndexOf(index);

        int prevIndex = indexList[currentIndex - 1];

        var prev = childs.Where(s => s.Index == prevIndex).ToList();//тут без ToList не работает
        var current = childs.Where(s => s.Index == index).ToList();

        //Console.WriteLine($"prev={prevIndex} current={index}; pi={prev.First().Index} ci={current.First().Index}");


        foreach (var v in current)
        {
            v.Index = prevIndex;
        }
        foreach (var v in prev)
        {
            v.Index = index;
        }

    }

    public void ListMoveDown(MetaFieldEditModel metaList, int index)
    {
        var count = MetaValues.Count(s => s.MetaField.ParentId == metaList.Id);
        if (count == 1) return;

        var childs = MetaValues.Where(s => s.MetaField.ParentId == metaList.Id);

        int minIndex = childs.Min(s => s.Index);
        int maxIndex = childs.Max(s => s.Index);

        if (index == maxIndex) return;

        //Тут индексы не нормализованы
        var indexList = childs.Select(s => s.Index).Distinct().OrderBy(s => s).ToList();

        //Console.WriteLine($"list = {indexList.Select(s=>s.ToString()).JoinStr(",")}");

        int currentIndex = indexList.IndexOf(index);

        int nextIndex = indexList[currentIndex + 1];

        var next = childs.Where(s => s.Index == nextIndex).ToList();//тут без ToList не работает
        var current = childs.Where(s => s.Index == index).ToList();

        //Console.WriteLine($"prev={prevIndex} current={index}; pi={prev.First().Index} ci={current.First().Index}");


        foreach (var v in current)
        {
            v.Index = nextIndex;
        }
        foreach (var v in next)
        {
            v.Index = index;
        }
    }
}
