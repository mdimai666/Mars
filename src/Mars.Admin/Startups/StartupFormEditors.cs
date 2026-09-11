using Mars.Admin.Components;
using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Components.Forms.Editors;
using Mars.Admin.Pages.PostsViews.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Admin.Startups;

/// <summary>
/// Регистрации общего слоя форм: редакторы значений (ключ → компонент + совместимые типы полей)
/// и доменные панели настроек полей. Название делает редактор предлагаемым в выборе редактора
/// поля; безымянные регистрации (обёртки провайдеров, встроенные дефолты) доступны только явным
/// ключом дескриптора. Вызывается после сборки контейнера (как <c>UseNodeWorkspace</c>):
/// реестры — синглтоны из DI, регистрировать в них можно и позже (загрузка плагинов).
/// </summary>
internal static class StartupFormEditors
{
    internal static IServiceProvider RegisterFormEditors(this IServiceProvider services)
    {
        var editors = services.GetRequiredService<IFormEditorLocator>();
        var panels = services.GetRequiredService<IFormFieldTypeSettingsLocator>();

        // редакторы значений — общие для всех полей: текстовые (в т.ч. тяжёлые WYSIWYG/код/блочный),
        // строковые и даты
        editors.Register(FormEditorCatalog.Wysiwyg, typeof(FormWysiwygEditor), false, "WYSIWYG (Quill)",
            FormFieldType.String, FormFieldType.Text);
        editors.Register(FormEditorCatalog.Code, typeof(FormCodeEditor), false, "Код (Monaco)",
            FormFieldType.String, FormFieldType.Text);
        editors.Register(FormEditorCatalog.BlockEditor, typeof(FormBlockEditor), false, "Блочный (Editor.js)",
            FormFieldType.String, FormFieldType.Text);
        editors.Register(FormEditorCatalog.Color, typeof(FormColorEditor), false, "Цвет", FormFieldType.String);
        editors.Register(FormEditorCatalog.Url, typeof(FormUrlEditor), false, "URL-адрес", FormFieldType.String);
        editors.Register(FormEditorCatalog.Email, typeof(FormEmailEditor), false, "Email", FormFieldType.String);
        editors.Register(FormEditorCatalog.Time, typeof(FormTimeEditor), false, "Время", FormFieldType.DateTime);
        editors.Register(FormEditorCatalog.DateTime, typeof(FormDateTimeEditor), false, "Дата и время",
            FormFieldType.DateTime);

        // доменные редакторы системных слотов формы поста (общий слой Mars.Forms): заголовок —
        // крупное поле на всю ширину, пикер категорий привязан к типу поста
        editors.Register(PostFormEditors.Title, typeof(PostTitleEditor), false, "Крупное поле",
            FormFieldType.String);
        editors.Register(PostFormEditors.Categories, typeof(PostCategoriesEditor), true, null, FormFieldType.Relation);

        // общие редакторы слотов: теги и отображение значения строкой годятся любому провайдеру
        editors.Register(FormEditorCatalog.Tags, typeof(FormTagsEditor), true, "Теги", FormFieldType.String);
        editors.Register(FormEditorCatalog.TextDisplay, typeof(FormTextDisplayEditor), false,
            "Текст только для чтения", FormFieldType.String, FormFieldType.Relation);

        // доменные редакторы метаполей: у связей и медиа нет типизированного CLR-значения, поэтому
        // их строки правят собственные компоненты (остальные значения метаполей — общие редакторы выше)
        editors.Register(MetaFormEditors.Relation, typeof(MetaValueRelationEditor), false, null,
            FormFieldType.Relation);
        editors.Register(MetaFormEditors.RelationMulti, typeof(MetaValueRelationEditor), true, null,
            FormFieldType.Relation);
        editors.Register(MetaFormEditors.File, typeof(MetaValueFileEditor), false, null,
            FormFieldType.File, FormFieldType.Image);
        editors.Register(MetaFormEditors.FileMulti, typeof(MetaValueFileEditor), true, null,
            FormFieldType.File, FormFieldType.Image);

        // доменные панели настроек полей в общем редакторе определений: метаполя (скоуп meta)
        // и системные слоты типа поста (язык кода выбранного редактора)
        panels.Register(MetaFieldSettingsPanel.Scope, typeof(MetaFieldSettingsPanel));
        panels.Register(SystemFieldSettingsPanel.Scope, typeof(SystemFieldSettingsPanel));

        return services;
    }
}
