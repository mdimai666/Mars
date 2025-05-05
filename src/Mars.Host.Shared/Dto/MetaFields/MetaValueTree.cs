using System.Text.Json.Nodes;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

public class MetaValueTree
{
    public string Key { get; set; }
    public MetaFieldType Type { get; set; }
    public object? Value { get; set; }
    public bool IsList { get; set; }
    //public int Index { get; set; }
    public IEnumerable<MetaValueTree> Childs { get; set; }

    public MetaValueTree(MetaValueDto value)
    {
        Key = value.MetaField.Key;
        Type = value.Type;
        Value = value.Type == MetaFieldType.List ? null : value.Value;
        IsList = value.Type == MetaFieldType.List;
    }

    public static IEnumerable<MetaValueTree> AsTree(IEnumerable<MetaValueDto> values, Guid parentId = new(), int index = 0)
    {
        throw new NotImplementedException();
        /*
        var metaFields = values.Select(s => s.MetaField).ToList();

        var root = metaFields.Where(s => s.ParentId == parentId && !s.Disabled);
        List<MetaValueTree> list = new();

        foreach (var meta in root)
        {
            if (meta.Type == MetaFieldType.List)
            {
                var x = new MetaValueTree(meta);
                var childs = metaFields.Where(s => s.ParentId == meta.Id);


                var xChilds = new List<MetaValueTree>();

                if (true)
                {
                    var values2 = values.Where(s => s.MetaField.ParentId == meta.Id);

                    if (values2.Count() > 0)
                    {
                        var groups = values2.GroupBy(s => s.Index).OrderBy(s => s.Key);

                        foreach (var group in groups)
                        {

                            var gg = new MetaValueTree() { Key = group.Key.ToString() };
                            var ggChilds = new List<MetaValueTree>();

                            foreach (var f in childs)
                            {
                                var mKey = meta.Key;
                                var fKeyName = f.Key;


                                //TODO: NOT tested 
                                if (f.TypeParentable)
                                {
                                    //var mChilds = AsTree(values.Except(group.ToList()), metaFields.Except(root), f.Id, group.Key);
                                    var mChilds = AsTree(values.Except(group.ToList()), f.Id, group.Key);
                                    MetaValueTree xx = new(f);
                                    xx.Childs = mChilds;
                                    ggChilds.Add(xx);
                                }
                                else
                                {
                                    var v = values2.FirstOrDefault(s => s.Index == group.Key && s.MetaField.Id == f.Id);
                                    if (v is not null)
                                    {
                                        MetaValueTree xx = new(v);
                                        ggChilds.Add(xx);
                                    }
                                }

                            }

                            gg.Childs = ggChilds;

                            xChilds.Add(gg);
                        }
                    }

                }

                x.Childs = xChilds;
                list.Add(x);
            }
            else
            {
                var v = values.FirstOrDefault(s => s.MetaField.Id == meta.Id && s.Index == index);
                MetaValueTree x = v is not null ? new(v) : new(meta);

                if (meta.Type == MetaFieldType.Group)
                {
                    x.Childs = AsTree(values.Except(v), metaFields.Except(root), meta.Id, index);
                }

                list.Add(x);
            }
        }

        return list;
        */
    }

    static void AsJsonValue(ref JsonObject json, MetaValueTree f)
    {
        if (f.Type == MetaFieldType.List)
        {
            var list = new JsonArray();

            foreach (var v in f.Childs)
            {
                var item = new JsonObject();
                foreach (var a in v.Childs)
                {
                    AsJsonValue(ref item, a);
                }
                list.Add(item);
            }
            json.Add(f.Key, list);
        }
        else if (f.Type == MetaFieldType.Group)
        {
            var group = new JsonObject();

            foreach (var v in f.Childs)
            {
                AsJsonValue(ref group, v);
            }
            json.Add(f.Key, group);
        }
        else
        {
            json.Add(f.Key, JsonValue.Create(f.Value));
        }
    }
}
