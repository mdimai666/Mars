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
/// ключом дескриптора. Вызывается до сборки приложения — реестры статические.
/// </summary>
internal static class StartupFormEditors
{
    internal static void RegisterFormEditors(this WebAssemblyHostBuilder builder)
    {
        // редакторы значений — общие для всех полей: текстовые (в т.ч. тяжёлые WYSIWYG/код/блочный),
        // строковые и даты
        FormEditorLocator.Register(FormEditorCatalog.Wysiwyg, typeof(FormWysiwygEditor), false, "WYSIWYG (Quill)",
            FormFieldType.String, FormFieldType.Text);
        FormEditorLocator.Register(FormEditorCatalog.Code, typeof(FormCodeEditor), false, "Код (Monaco)",
            FormFieldType.String, FormFieldType.Text);
        FormEditorLocator.Register(FormEditorCatalog.BlockEditor, typeof(FormBlockEditor), false, "Блочный (Editor.js)",
            FormFieldType.String, FormFieldType.Text);
        FormEditorLocator.Register(FormEditorCatalog.Color, typeof(FormColorEditor), false, "Цвет", FormFieldType.String);
        FormEditorLocator.Register(FormEditorCatalog.Url, typeof(FormUrlEditor), false, "URL-адрес", FormFieldType.String);
        FormEditorLocator.Register(FormEditorCatalog.Email, typeof(FormEmailEditor), false, "Email", FormFieldType.String);
        FormEditorLocator.Register(FormEditorCatalog.Time, typeof(FormTimeEditor), false, "Время", FormFieldType.DateTime);
        FormEditorLocator.Register(FormEditorCatalog.DateTime, typeof(FormDateTimeEditor), false, "Дата и время",
            FormFieldType.DateTime);

        // доменный редактор системного слота формы поста (общий слой Mars.Forms): пикер категорий
        // привязан к типу поста, поэтому остаётся редактором провайдера
        FormEditorLocator.Register(PostFormEditors.Categories, typeof(PostCategoriesEditor), true, FormFieldType.Relation);

        // общие редакторы слотов: теги и отображение значения строкой годятся любому провайдеру
        FormEditorLocator.Register(FormEditorCatalog.Tags, typeof(FormTagsEditor), true, "Теги", FormFieldType.String);
        FormEditorLocator.Register(FormEditorCatalog.TextDisplay, typeof(FormTextDisplayEditor), false,
            "Текст только для чтения", FormFieldType.String, FormFieldType.Relation);

        // доменные редакторы метаполей: у связей и медиа нет типизированного CLR-значения, поэтому
        // их строки правят собственные компоненты (остальные значения метаполей — общие редакторы выше)
        FormEditorLocator.Register(MetaFormEditors.Relation, typeof(MetaValueRelationEditor), false, FormFieldType.Relation);
        FormEditorLocator.Register(MetaFormEditors.RelationMulti, typeof(MetaValueRelationEditor), true, FormFieldType.Relation);
        FormEditorLocator.Register(MetaFormEditors.File, typeof(MetaValueFileEditor), false, FormFieldType.File, FormFieldType.Image);
        FormEditorLocator.Register(MetaFormEditors.FileMulti, typeof(MetaValueFileEditor), true, FormFieldType.File, FormFieldType.Image);

        // доменные панели настроек полей в общем редакторе определений: метаполя (скоуп meta)
        // и системные слоты типа поста (язык кода выбранного редактора)
        FormFieldTypeSettingsLocator.Register(MetaFieldSettingsPanel.Scope, typeof(MetaFieldSettingsPanel));
        FormFieldTypeSettingsLocator.Register(SystemFieldSettingsPanel.Scope, typeof(SystemFieldSettingsPanel));
    }
}
