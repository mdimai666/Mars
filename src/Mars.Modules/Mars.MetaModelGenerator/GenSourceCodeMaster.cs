using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Interfaces;

namespace Mars.MetaModelGenerator;

public class GenSourceCodeMaster
{
    public const string selectExpressionGetterName = "selectExpression";

    /// <summary>
    /// Создает исходный код для Модели с MetaFields делая поля часью модели. Для запросов через ef
    /// </summary>
    /// <param name="newClassName">Новое имя модели - e.t.c NewsMto</param>
    /// <param name="baseEntityType">Базовый тип для наследования</param>
    /// <param name="metaFields">Мета поля</param>
    /// <returns>Исходный код для компиляции</returns>
    public string Generate(string newClassName, Type baseEntityType, MetaFieldEntity[] metaFields,
                            DisplayAttribute? displayAttribute, IReadOnlyDictionary<string, MetaModelResolveTypeInfo>? metaModelTypesResolverDict)
    {
        //var newClassName = GenSourceCodeMasterHelper.GetNormalizedTypeName(baseEntityType.Name, "Mto");

        var fields = metaFields.Select(f => new MtFieldInfo(f, metaModelTypesResolverDict)).ToList();
        var fieldsStr = string.Join("\n\n", fields);

        var sb = new StringBuilder();

        var selectExpression = GenSourceCodeMasterHelper.AddTabsToLines(SelectExpressionString(baseEntityType, fields, newClassName));

        var code = $$"""
            /// <summary>
            /// {{displayAttribute?.Description}}
            /// </summary>
            [Display(Name = "{{displayAttribute?.Name}}", Description = "{{displayAttribute?.Description}}", GroupName = "{{displayAttribute?.GroupName}}")]
            public partial class {{newClassName}} : global::{{baseEntityType.Namespace}}.{{baseEntityType.Name}}, {{nameof(IMtoMarker)}}
            {
                {{fieldsStr}}

                // select expression
                {{selectExpression}}
            }
            """;

        return code;
    }

    List<string> TypeFieldsAsSourceCode(Type type)
    {
        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite).ToArray();

        List<string> result = new List<string>();

        foreach (var prop in props)
        {
            string typeName = GenSourceCodeMasterHelper.GetFriendlyTypeName(prop.PropertyType.Name);
            if (prop.PropertyType.IsGenericType)
            {
                typeName = GenSourceCodeMasterHelper.GetFormattedName(prop.PropertyType);
            }
            result.Add($"\tpublic {typeName} {prop.Name} {{ get; set; }}");
        }

        return result;
    }

    public string SelectExpressionString(Type baseEntityType, IReadOnlyCollection<MtFieldInfo> metaFields, string newClassName)
    {
        var props = baseEntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propsStrings = props.Where(p => p.CanRead && p.CanWrite)
                                .Where(s => s.Name != nameof(PostEntity.MetaValues))
                                .Select(p => $"\t{p.Name} = post.{p.Name},").JoinStr("\n");

        var metaFieldStrings = metaFields.Select(f => $"{f.SelectExpressionRow()},").JoinStr("\n");

        //propsStrings = GenSourceCodeMasterHelper.AddTabsToLines(propsStrings);
        metaFieldStrings = GenSourceCodeMasterHelper.AddTabsToLines(metaFieldStrings, 1);

        return $$"""
                public static readonly Expression<Func<{{baseEntityType.Name}}, {{newClassName}}>> {{selectExpressionGetterName}} = post => new {{newClassName}}
                {
                    // props
                    {{propsStrings}}

                    // meta fields
                    {{metaFieldStrings}}
                };
                """;
    }

}

// Концепт
public interface IGenExtraFieldFunction
{
    public string ExtraFieldCode(Type baseEntityType);
    public Type[] UsingTypes();
}

public class PostStatusFieldFunction : IGenExtraFieldFunction
{
    public string ExtraFieldCode(Type baseEntityType)
    {
        return """
                public string StatusDto
                """;
    }

    public Type[] UsingTypes() => [typeof(PostStatusDto)];
}
