using Microsoft.AspNetCore.Components;

namespace Mars.Datasource.Front.Workspaces.Shared;

/// <summary>Подсказка на пустом результате: как писать запрос к источнику этого типа.</summary>
public partial class ObjectsHint : ComponentBase
{
    [Parameter] public bool IsRest { get; set; }
}
