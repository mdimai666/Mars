using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Data.Entities;

/// <summary>
/// Счётчик генератора «порядковый номер»: на пару (мета-поле, скоуп) — последний выданный номер.
/// Скоуп задаётся префиксом (в т.ч. из категории) и, при ежедневном сбросе, датой.
/// </summary>
public class MetaSequenceEntity
{
    [Key]
    [Comment("ИД")]
    public Guid Id { get; set; }

    [Comment("Мета-поле")]
    public Guid MetaFieldId { get; set; }
    public virtual MetaFieldEntity? MetaField { get; set; }

    [Comment("Скоуп счётчика (префикс, опционально + дата)")]
    public string ScopeKey { get; set; } = "";

    [Comment("Последний выданный номер")]
    public long LastValue { get; set; }

    [Comment("Создан")]
    public DateTimeOffset CreatedAt { get; set; }

    [Comment("Изменен")]
    public DateTimeOffset? ModifiedAt { get; set; }
}
