using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;
using Mars.Forms.Front;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Регистрация редакторов значений метаполей в общем реестре формы (<see cref="IFormEditorLocator"/>):
/// ключ из <see cref="MetaFieldEditorCatalog"/> → компонент + совместимые типы полей + название для
/// UI выбора редактора. Отдельного реестра метаполей больше нет — ключи и компоненты живут в одном
/// месте со всеми редакторами формы.
/// Тяжёлые и модульные редакторы (блочный Editor.js) регистрирует потребитель: их компоненты живут
/// вне общей библиотеки.
/// </summary>
public static class MetaFieldEditors
{
    public static void RegisterAll()
    {
        Register(MetaFieldEditorCatalog.Color, typeof(Editors.MetaValueColorEditor), FormFieldType.String);
        Register(MetaFieldEditorCatalog.Url, typeof(Editors.MetaValueUrlEditor), FormFieldType.String);
        Register(MetaFieldEditorCatalog.Email, typeof(Editors.MetaValueEmailEditor), FormFieldType.String);
        Register(MetaFieldEditorCatalog.Date, typeof(Editors.MetaValueDateEditor), FormFieldType.DateTime);
        Register(MetaFieldEditorCatalog.Time, typeof(Editors.MetaValueTimeEditor), FormFieldType.DateTime);
        Register(MetaFieldEditorCatalog.DateTime, typeof(Editors.MetaValueDateTimeEditor), FormFieldType.DateTime);
        Register(MetaFieldEditorCatalog.Wysiwyg, typeof(Editors.MetaValueWysiwygEditor), FormFieldType.String, FormFieldType.Text);
        Register(MetaFieldEditorCatalog.Code, typeof(Editors.MetaValueCodeEditor), FormFieldType.String, FormFieldType.Text);
    }

    /// <summary>Ключ относится к редакторам метаполей: список редакторов в настройках поля не смешивается с чужими</summary>
    public static bool IsMetaEditor(string? editorKey)
        => !string.IsNullOrEmpty(editorKey) && MetaFieldEditorCatalog.All.Any(entry => entry.Key == editorKey);

    /// <summary>Название редактора из каталога (для регистрации в общем реестре)</summary>
    public static string Title(string editorKey)
        => MetaFieldEditorCatalog.All.FirstOrDefault(entry => entry.Key == editorKey).Title ?? editorKey;

    static void Register(string editorKey, Type component, params FormFieldType[] fieldTypes)
        => FormEditorLocator.Register(editorKey, component, false, Title(editorKey), fieldTypes);
}
