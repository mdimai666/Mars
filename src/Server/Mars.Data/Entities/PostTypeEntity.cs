using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Mars.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Data.Entities;

[DebuggerDisplay("{TypeName}/{Title}/{Id}")]
public class PostTypeEntity : IBasicEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }

    [Comment("Название")]
    [Required]
    public string Title { get; set; } = default!;

    [StringLength(100)]
    [Comment("Тип")]
    [Required]
    public string TypeName { get; set; } = default!;

    /// <summary>
    /// <b>[jsonb]</b>
    /// </summary>
    [Comment("Функции")]
    public List<string> EnabledFeatures { get; set; } = [];

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    [Comment("Видимость: публичный тип или встроенный компонент")]
    public EPostTypeVisibility Visibility { get; set; } = EPostTypeVisibility.Public;

    [StringLength(100)]
    [Comment("Ключ мета-поля — картинки типа (указатель превью)")]
    public string? ImageFieldKey { get; set; }

    //[Comment("Категория")]
    //public Guid CategoryId { get; set; }

    //icon, in_menu, search, public

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// <b>[jsonb]</b> Общие настройки типа (точка расширения) —
    /// см. <c>PostTypeOptionsCatalog</c>. Пер-постовые вещи — мета-поля, не опции.
    /// </summary>
    [Comment("Опции (точка расширения)")]
    public JsonNode? Options { get; set; }

    // Relations

    public virtual ICollection<PostStatusEntity>? Statuses { get; set; }

    public virtual ICollection<MetaFieldEntity>? MetaFields { get; set; }

    [NotMapped] //вспомогательный, для получения
    public virtual List<PostEntity>? Posts { get; set; }

    //[Comment("Форма списка")]
    //[Column(TypeName = "jsonb")]
    //public FormEditSettings FormList { get; set; }

    //[Comment("Форма редактирования")]
    //[Column(TypeName = "jsonb")]
    //public FormEditSettings FormEdit { get; set; }

    //[Comment("View settings")]
    //[Column(TypeName = "jsonb")]
    //public ModelViewSettingsEntity ViewSettings { get; set; } = new();

    public PostTypePresentationEntity? Presentation { get; set; }

    public virtual ICollection<PostCategoryEntity>? PostCategories { get; set; }
}
