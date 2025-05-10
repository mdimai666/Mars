using Mars.Nodes.Core.Nodes;
using Mars.Nodes.EditorApi.Interfaces;
using Microsoft.AspNetCore.Components;

namespace NodeFormEditor.EditForms.Components;

public partial class InputConfigField<TConfig>
    where TConfig : ConfigNode
{
    InputConfig<TConfig> _value;

    [Parameter]
    public InputConfig<TConfig> Value
    {
        get => _value;
        set
        {
            if (_value.Equals(value) || _value.Id == value.Id) return;
            _value = value;
            ValueChanged.InvokeAsync(_value);
        }
    }

    [Parameter]
    public EventCallback<InputConfig<TConfig>> ValueChanged { get; set; }

    [CascadingParameter]
    NodeEditContainer1 _nodeEditContainer1 { get; set; } = default!;

    [CascadingParameter]
    INodeEditorApi _nodeEditorApi { get; set; } = default!;


    Dictionary<string, KeyValuePair<string, string>> _items = [];

    KeyValuePair<string, string> _valueSetter
    {
        get => _items[Value.Id];
        set => Value = new InputConfig<TConfig> { Id = value.Key, Value = GetById(value.Key) };
    }

    readonly KeyValuePair<string, string> Empty = new(string.Empty, "--");

    protected override void OnParametersSet()
    {
        _items = ((KeyValuePair<string, string>[])[Empty,
                    .. _nodeEditorApi.Nodes
                                .Where(s => s.GetType() == typeof(TConfig))
                                .Select(node => new KeyValuePair<string,string>(node.Id, node.Label))
                                ]).ToDictionary(s => s.Key);
    }

    TConfig? GetById(string id) => (TConfig?)_nodeEditorApi.Nodes.FirstOrDefault(s => s.Id == id);

    void ClickEdit()
    {
        _nodeEditContainer1.OnClickEditConfigNode.InvokeAsync(Value.Id);
    }

    void ClickNew()
    {
        _nodeEditContainer1.OnClickNewConfigNode.InvokeAsync(typeof(TConfig));
    }
}
