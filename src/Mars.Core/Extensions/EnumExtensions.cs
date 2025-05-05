using System.ComponentModel;

namespace Mars.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Получить описание значения <see cref="DescriptionAttribute" />
    /// <para />
    /// При отсутсвии атрибута строковое значение enum.
    /// </summary>
    /// <param name="enumValue"></param>
    /// <returns>Значение Description или .ToString()</returns>
    public static string GetDescription(this Enum enumValue)
    {
        var description = enumValue.GetAttributeOfType<DescriptionAttribute>();
        return description != null
            ? description.Description
            : enumValue.ToString();
    }

    public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();

        var isEnumValue = Enum.IsDefined(type, enumVal);
        if (!isEnumValue)
            throw new InvalidOperationException(
                $"Значение {enumVal} не является элементом {type.Name}");

        var memberInfo = type.GetMember(enumVal.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0
            ? (T)attributes[0]
            : null!;
    }
}
