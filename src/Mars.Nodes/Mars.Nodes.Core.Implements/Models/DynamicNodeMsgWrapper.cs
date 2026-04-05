using System.Dynamic;
using System.Reflection;

namespace Mars.Nodes.Core.Implements.Models;

/// <summary>
/// Работает как обертка для NodeMsg с правильными типами.
/// </summary>
// из-за того что у NodeMsg.Payload тип object многие reflection работают неправильно.
public class DynamicNodeMsgWrapper : DynamicObject
{
    private readonly NodeMsg _nodeMsg;
    private readonly object? _payload;
    private readonly Dictionary<string, PropertyInfo> _allProperties;

    public DynamicNodeMsgWrapper(NodeMsg nodeMsg)
    {
        _nodeMsg = nodeMsg;
        _payload = nodeMsg.Payload;
        _allProperties = [];

        // Добавляем свойства NodeMsg
        foreach (var prop in typeof(NodeMsg).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            _allProperties[prop.Name] = prop;
        }

        // Добавляем свойства Payload
        if (_payload != null)
        {
            foreach (var prop in _payload.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                _allProperties[prop.Name] = prop;
            }
        }
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_allProperties.TryGetValue(binder.Name, out var property))
        {
            // Определяем, с какого объекта читать свойство
            var target = property.DeclaringType == typeof(NodeMsg)
                ? _nodeMsg
                : _payload;

            result = property.GetValue(target);
            return true;
        }

        result = null;
        return false;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (_allProperties.TryGetValue(binder.Name, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg)
                ? _nodeMsg
                : _payload;

            property.SetValue(target, value);
            return true;
        }

        return false;
    }
}
