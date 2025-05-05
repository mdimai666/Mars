using AppShared.Models;
using Mars.Host.Shared.Services;
using Mars.Core.Extensions;

namespace Mars.GenSourceCode;

public class DtFieldInfo : MtFieldInfo
{

    public DtFieldInfo(MetaField metaField, IMetaModelTypesLocator locator) : base(metaField, locator)
    {

    }

    public override string ToString()
    {
        if (metaField.TypeRelation)
        {

            return
                (string.IsNullOrEmpty(comment) ? "" : $"{comment}\n") +
                $"\t{attributes.JoinStr("\n\t")}\n" +
                $"\tpublic {relationModelTypeFullName}? {name} {{get; set; }}" +
                "\n" +
                $"\tpublic {friendlyTypeName} _{name} {{get; set; }}";

        }

        return base.ToString();
    }
}
