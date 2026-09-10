using Mars.Admin.Components;
using Mars.Admin.Framework.Components.Forms;
using Mars.Admin.Framework.Components.Forms.Editors;
using Mars.Admin.Pages.PostsViews.Forms;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.PostTypes;
using Mars.Forms.Contracts;
using Mars.Forms.Front;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Mars.Admin.Startups;

/// <summary>
/// Регистрации общего слоя форм: редакторы значений (ключ → компонент + совместимые типы полей)
/// и доменные панели настроек полей. Название делает редактор предлагаемым в выборе редактора
/// поля; безымянные регистрации (обёртки провайдеров, встроенные дефолты) доступны только явным
/// ключом дескриптора. Вызывается до сборки контейнера — реестры строятся из этих регистраций.
/// </summary>
internal static class StartupFormEditors
{
    internal static void RegisterFormEditors(this WebAssemblyHostBuilder builder)
    {
        var services = builder.Services;

        // редакторы значений — общие для всех полей: текстовые (в т.ч. тяжёлые WYSIWYG/код/блочный),
        // строковые и даты
        services.AddFormEditor(FormEditorCatalog.Wysiwyg, typeof(FormWysiwygEditor), false, "WYSIWYG (Quill)",
            FormFieldType.String, FormFieldType.Text);
        services.AddFormEditor(FormEditorCatalog.Code, typeof(FormCodeEditor), false, "Код (Monaco)",
            FormFieldType.String, FormFieldType.Text);
        services.AddFormEditor(FormEditorCatalog.BlockEditor, typeof(FormBlockEditor), false, "Блочный (Editor.js)",
            FormFieldType.String, FormFieldType.Text);
        services.AddFormEditor(FormEditorCatalog.Color, typeof(FormColorEditor), false, "Цвет", FormFieldType.String);
        services.AddFormEditor(FormEditorCatalog.Url, typeof(FormUrlEditor), false, "URL-адрес", FormFieldType.String);
        services.AddFormEditor(FormEditorCatalog.Email, typeof(FormEmailEditor), false, "Email", FormFieldType.String);
        services.AddFormEditor(FormEditorCatalog.Time, typeof(FormTimeEditor), false, "Время", FormFieldType.DateTime);
        services.AddFormEditor(FormEditorCatalog.DateTime, typeof(FormDateTimeEditor), false, "Дата и время",
            FormFieldType.DateTime);

        // доменный редактор системного слота формы поста (общий слой Mars.Forms): пикер категорий
        // привязан к типу поста, поэтому остаётся редактором провайдера
        services.AddFormEditor(PostFormEditors.Categories, typeof(PostCategoriesEditor), true, null,
            FormFieldType.Relation);

        // общие редакторы слотов: теги и отображение значения строкой годятся любому провайдеру
        services.AddFormEditor(FormEditorCatalog.Tags, typeof(FormTagsEditor), true, "Теги", FormFieldType.String);
        services.AddFormEditor(FormEditorCatalog.TextDisplay, typeof(FormTextDisplayEditor), false,
            "Текст только для чтения", FormFieldType.String, FormFieldType.Relation);

        // доменные редакторы метаполей: у связей и медиа нет типизированного CLR-значения, поэтому
        // их строки правят собственные компоненты (остальные значения метаполей — общие редакторы выше)
        services.AddFormEditor(MetaFormEditors.Relation, typeof(MetaValueRelationEditor), false, null,
            FormFieldType.Relation);
        services.AddFormEditor(MetaFormEditors.RelationMulti, typeof(MetaValueRelationEditor), true, null,
            FormFieldType.Relation);
        services.AddFormEditor(MetaFormEditors.File, typeof(MetaValueFileEditor), false, null,
            FormFieldType.File, FormFieldType.Image);
        services.AddFormEditor(MetaFormEditors.FileMulti, typeof(MetaValueFileEditor), true, null,
            FormFieldType.File, FormFieldType.Image);

        // доменные панели настроек полей в общем редакторе определений: метаполя (скоуп meta)
        // и системные слоты типа поста (язык кода выбранного редактора)
        services.AddFormFieldSettingsPanel(MetaFieldSettingsPanel.Scope, typeof(MetaFieldSettingsPanel));
        services.AddFormFieldSettingsPanel(SystemFieldSettingsPanel.Scope, typeof(SystemFieldSettingsPanel));
    }
}
