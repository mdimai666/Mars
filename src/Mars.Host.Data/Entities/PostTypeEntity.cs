using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Mars.Host.Data.Common;
using Mars.Host.Data.Configurations;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

[DebuggerDisplay("{TypeName}/{Title}/{Id}")]
[EntityTypeConfiguration(typeof(PostTypeEntityConfiguration))]
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
    [Comment("Статусы")]
    [Column(TypeName = "jsonb")] // see configuration: used .ToJson()
    public List<PostStatusEntity> PostStatusList { get; set; } = new();

    /// <summary>
    /// <b>[jsonb]</b>
    /// </summary>
    [Comment("Функции")]
    [Column(TypeName = "jsonb")]
    public List<string> EnabledFeatures { get; set; } = new();

    [Comment("Отключен")]
    public bool Disabled { get; set; }

    //[Comment("Категория")]
    //public Guid CategoryId { get; set; }

    //icon, in_menu, search, public

    /// <summary>
    /// <b>[jsonb]</b>
    /// </summary>
    [Comment("Настройки контента")]
    [Column(TypeName = "jsonb")] // see configuration: used .ToJson()
    public PostContentSettings PostContentType { get; set; } = new();

    [Comment("Теги")]
    public List<string> Tags { get; set; } = [];

    // Relations

    public virtual ICollection<PostTypeMetaFieldEntity>? PostTypeMetaFields { get; set; }
    [NotMapped]
    public virtual List<MetaFieldEntity>? MetaFields { get; set; }

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

}

/// <summary>
/// [jsonb]
/// </summary>
public class ModelViewSettingsEntity
{
    public string? ListViewTemplateSourceUri { get; set; }

    //public SourceUri ListViewTemplateSourceUri { get; set; } = new(null);

    //[NotMapped]
    //[JsonIgnore]
    //[ValidateSourceUri]
    //public string SourceUriSetter
    //{
    //    get => SourceUri;
    //    set
    //    {
    //        try
    //        {
    //            SourceUri = new SourceUri(value);
    //        }
    //        catch (Exception)
    //        {
    //            SourceUri = null;
    //        }
    //    }
    //}

}
