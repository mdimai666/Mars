using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Models.Interfaces;
using System;

namespace Mars.Host.Shared.Templators;

[Obsolete]
public class MetaRelationObjectDict
{
    public Guid MetaValueId { get; set; }
    public Guid ModelId { get; set; }

    /// <summary>
    /// <seealso cref="MetaField.GetModelType(EMetaFieldType,string)"/>
    /// </summary>
    public string ModelName { get; set; }
    public MetaFieldType Type { get; set; }
    public IBasicEntity Entity { get; set; }

    public MetaRelationObjectDict()
    {

    }

    public MetaRelationObjectDict(MetaValueDto metaValue)
    {
        //MetaValueId = metaValue.Id;
        //ModelId = metaValue.ModelId;
        //ModelName = metaValue.MetaField.Type switch
        //{
        //    EMetaFieldType.Image => nameof(FileEntity),
        //    EMetaFieldType.File => nameof(FileEntity),
        //    _ => metaValue.MetaField.ModelName
        //};
        //Type = metaValue.MetaField.Type;
        throw new NotImplementedException();
    }
}
