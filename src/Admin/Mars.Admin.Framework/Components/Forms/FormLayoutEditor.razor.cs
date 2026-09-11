using Mars.Contracts.Resources;
using Mars.Forms.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Дизайнер раскладки формы: зоны, контейнеры-табы и сетка ряды/колонки, собранная
/// перетаскиванием. Все операции ведёт <see cref="FormLayoutDraft"/>, а наружу уходит только
/// раскладка (<see cref="FormLayoutSettings"/>): дескрипторы не хранятся, их каждый раз отдаёт
/// провайдер. Параметры самих полей (правила, редактор) здесь не редактируются — они живут
/// в настройках владельца формы.
/// </summary>
public partial class FormLayoutEditor
{
    [Inject] IStringLocalizer<AppRes> L { get; set; } = default!;

    /// <summary>Действующее определение провайдера: уже с применённой сохранённой раскладкой</summary>
    [Parameter] public FormDefinition? Definition { get; set; }

    /// <summary>Раскладка после каждого изменения; null — сброс к раскладке провайдера</summary>
    [Parameter] public EventCallback<FormLayoutSettings?> ValueChanged { get; set; }

    /// <summary>
    /// Просьба сбросить раскладку: хост подменяет <see cref="Definition"/> на раскладку по умолчанию
    /// (дизайнер не знает порядка провайдера — определение собирает сервер).
    /// </summary>
    [Parameter] public EventCallback OnResetRequested { get; set; }

    /// <summary>Черновик раскладки — состояние дизайнера (переживает ре-рендеры)</summary>
    public FormLayoutDraft Draft => _draft ??= new FormLayoutDraft([], []);

    FormLayoutDraft? _draft;
    FormDefinition? _source;

    /// <summary>Варианты ширины колонки в долях из 12 — в дизайнере показывается число</summary>
    public static readonly IReadOnlyList<(string Key, string Title)> Widths =
    [
        (FormItemWidths.Full, "12"),
        (FormItemWidths.Half, "6"),
        (FormItemWidths.Third, "4"),
        (FormItemWidths.Quarter, "3"),
    ];

    /// <summary>Зона приёма: пунктирная рамка и минимальная высота, чтобы пустое место было видно</summary>
    public static string DropAreaStyle
        => "border:1px dashed var(--neutral-stroke-rest); border-radius:4px; padding:6px; min-height:44px";

    /// <summary>Ряд — сплошная акцентная рамка: отличается от рамки ячейки внутри него</summary>
    public static string RowStyle
        => "border:2px solid var(--accent-fill-rest); border-radius:6px; padding:4px; min-width:120px";

    /// <summary>Ячейка (колонка) — пунктирная нейтральная рамка другого цвета, чем у ряда</summary>
    public static string ColumnStyle
        => "border:2px dashed var(--neutral-stroke-rest); border-radius:6px; padding:4px; min-height:56px";

    /// <summary>Листовой элемент — прямоугольник с названием</summary>
    public static string ElementStyle
        => "background:var(--neutral-layer-2); border:1px solid var(--neutral-stroke-rest); border-radius:4px; padding:2px 4px";

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_source, Definition)) return;

        _source = Definition;
        _draft = Definition is null ? null : new FormLayoutDraft(Definition.Items, ZonesOf(Definition));
    }

    static IReadOnlyList<FormZoneDescriptor> ZonesOf(FormDefinition definition)
        => definition.Zones.Count > 0
            ? definition.Zones.ToList()
            : definition.Items.Select(item => item.Zone ?? "")
                              .Distinct()
                              .Select(zone => new FormZoneDescriptor { Key = zone, Title = zone })
                              .ToList();

    /// <summary>Дерево зоны для дизайнера — тот же проектор, что и у рендера формы</summary>
    public IReadOnlyList<FormLayoutNode> TreeOf(string zone) => FormLayoutTree.Build(Draft.Items, zone);

    /// <summary>Цель броска в корень зоны (вне контейнеров)</summary>
    public static LayoutDropTarget RootTarget(string zone) => new(zone, null, null);

    /// <summary>Заголовок узла: переопределение раскладки, затем ключ ресурса, затем заголовок поля</summary>
    public string DisplayTitle(FormItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Title)) return item.Title;
        if (item.Field is not { } descriptor) return item.Key;

        return string.IsNullOrEmpty(descriptor.TitleKey) ? descriptor.Title : L[descriptor.TitleKey];
    }

    /// <summary>Имя таба контейнера</summary>
    public static string TabTitle(FormItem container)
        => string.IsNullOrWhiteSpace(container.Title) ? "Контейнер" : container.Title;

    /// <summary>Доля ширины ячейки в ряду — дизайнер показывает пропорции как на форме</summary>
    public static string FlexStyle(FormItem item) => item.Width switch
    {
        FormItemWidths.Half => "flex:0 0 50%; max-width:50%",
        FormItemWidths.Third => "flex:0 0 33%; max-width:33%",
        FormItemWidths.Quarter => "flex:0 0 25%; max-width:25%",
        _ => "",
    };

    //=====================================
    // изменения

    public Task EmitAsync() => ValueChanged.InvokeAsync(Draft.ToSettings());

    public async Task AddContainerAsync(string zone)
    {
        if (Draft.AddContainer(zone, "Контейнер") is not null) await EmitAsync();
    }

    public async Task ResetAsync()
    {
        await ValueChanged.InvokeAsync(null);
        await OnResetRequested.InvokeAsync();
    }

    /// <summary>Обработчик броска: у <c>FluentDragContainer.OnDropEnd</c> void-делегат</summary>
    public void OnDropEnd(FluentDragEventArgs<FormItem> args) => _ = DropAsync(args);

    /// <summary>Перенос узла: зона, родитель и место приезжают с целью броска</summary>
    public async Task DropAsync(FluentDragEventArgs<FormItem> args)
    {
        if (args.Source.Item is not { } dragged) return;
        if (args.Target.Data is not LayoutDropTarget target) return;

        if (Draft.Move(dragged.Key, target.ParentKey, target.BeforeKey, target.Zone)) await EmitAsync();
    }

    /// <summary>Куда бросают узел: зона, ключ родителя и узел, перед которым вставляют (null — в конец)</summary>
    public sealed record LayoutDropTarget(string Zone, string? ParentKey, string? BeforeKey);
}
