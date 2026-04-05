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
    private readonly Dictionary<string, PropertyInfo> _payloadProperties;

    public DynamicNodeMsgWrapper(NodeMsg nodeMsg)
    {
        _nodeMsg = nodeMsg ?? throw new ArgumentNullException(nameof(nodeMsg));
        _payload = nodeMsg.Payload;
        _allProperties = [];
        _payloadProperties = [];

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
                _payloadProperties[prop.Name] = prop;
            }
        }
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        // 1. Сначала проверяем свойства NodeMsg и Payload
        if (_allProperties.TryGetValue(binder.Name, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg) ? _nodeMsg : _payload;
            result = property.GetValue(target);
            return true;
        }

        // 2. Затем проверяем Context в NodeMsg
        var contextValue = _nodeMsg.Get(binder.Name);
        if (contextValue != null)
        {
            result = contextValue;
            return true;
        }

        result = null;
        return false;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        // 1. Сначала проверяем свойства NodeMsg и Payload
        if (_allProperties.TryGetValue(binder.Name, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg) ? _nodeMsg : _payload;

            // Проверяем совместимость типов
            if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(target, value);
                return true;
            }
            else if (value == null && (!property.PropertyType.IsValueType ||
                     Nullable.GetUnderlyingType(property.PropertyType) != null))
            {
                property.SetValue(target, value);
                return true;
            }
            return false;
        }

        // 2. Если свойство не найдено, сохраняем в Context
        if (value != null)
        {
            _nodeMsg.Set(binder.Name, value);
            return true;
        }

        return false;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        // Пытаемся вызвать метод на Payload
        if (_payload != null)
        {
            var method = _payload.GetType().GetMethod(
                binder.Name,
                BindingFlags.Public | BindingFlags.Instance,
                args?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? Type.EmptyTypes
            );

            if (method != null)
            {
                result = method.Invoke(_payload, args);
                return true;
            }
        }

        result = null;
        return false;
    }

    // Метод для конвертации обратно в NodeMsg
    public NodeMsg ToNodeMsg()
    {
        // Создаем копию NodeMsg с текущим Payload
        var newNodeMsg = _nodeMsg.Copy(_payload);

        // Если были изменения в свойствах Payload, нужно обновить их
        if (_payload != null && _payloadProperties.Any())
        {
            foreach (var prop in _payloadProperties)
            {
                var currentValue = prop.Value.GetValue(_payload);
                prop.Value.SetValue(newNodeMsg.Payload, currentValue);
            }
        }

        return newNodeMsg;
    }

    // Получить оригинальный NodeMsg (без применения изменений из обертки)
    public NodeMsg GetOriginalNodeMsg() => _nodeMsg;

    // Получить тип Payload
    public Type? GetPayloadType() => _payload?.GetType();

    // Проверить существование свойства (включая Context)
    public bool HasProperty(string propertyName)
    {
        return _allProperties.ContainsKey(propertyName) ||
               _nodeMsg.Get(propertyName) != null;
    }

    // Проверить, является ли свойство свойством Payload
    public bool IsPayloadProperty(string propertyName) =>
        _payloadProperties.ContainsKey(propertyName);

    // Получить значение из Context
    public object? GetFromContext(string name) => _nodeMsg.Get(name);

    // Добавить или обновить значение в Context
    public void SetInContext(string name, object value) => _nodeMsg.Set(name, value);

    // Получить весь Context
    public Dictionary<string, object> GetContext() => _nodeMsg.AsFullDict();

    // Простой метод для установки значения
    public void SetValue(string propertyName, object? value)
    {
        // Прямая логика установки (без вызова TrySetMember)
        if (_allProperties.TryGetValue(propertyName, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg) ? _nodeMsg : _payload;
            property.SetValue(target, value);
        }
        else if (value != null)
        {
            _nodeMsg.Set(propertyName, value);
        }
    }

    // Получение значения
    public object? GetValue(string propertyName)
    {
        if (_allProperties.TryGetValue(propertyName, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg) ? _nodeMsg : _payload;
            return property.GetValue(target);
        }

        return _nodeMsg.Get(propertyName);
    }

    // Индексатор для удобства
    public object? this[string propertyName]
    {
        get => GetValue(propertyName);
        set => SetValue(propertyName, value);
    }

    // SetValue с поддержкой property path
    public bool SetValueByPath(string propertyPath, object? value)
    {
        if (string.IsNullOrEmpty(propertyPath))
            return false;

        var parts = propertyPath.Split('.');

        // Если путь состоит из одной части, используем обычную логику
        if (parts.Length == 1)
        {
            var binder = new SimpleSetMemberBinder(parts[0], false);
            return TrySetMember(binder, value);
        }

        // Для вложенных свойств
        return SetNestedValue(propertyPath, value);
    }

    // GetValue с поддержкой property path
    public object? GetValueByPath(string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
            return null;

        var parts = propertyPath.Split('.');

        // Если путь состоит из одной части, используем обычную логику
        if (parts.Length == 1)
        {
            var binder = new SimpleGetMemberBinder(parts[0], false);
            TryGetMember(binder, out var result);
            return result;
        }

        // Для вложенных свойств
        return GetNestedValue(propertyPath);
    }

    // Реализация для вложенных свойств
    private bool SetNestedValue(string propertyPath, object? value)
    {
        var parts = propertyPath.Split('.');
        object? currentObject = GetRootObject(parts[0]);

        if (currentObject == null)
            return false;

        // Идем по всем частям пути, кроме последней
        for (int i = 1; i < parts.Length - 1; i++)
        {
            currentObject = GetPropertyValue(currentObject, parts[i]);
            if (currentObject == null)
                return false;
        }

        // Устанавливаем значение последнего свойства
        var lastProperty = parts[^1];
        return SetPropertyValue(currentObject, lastProperty, value);
    }

    private object? GetNestedValue(string propertyPath)
    {
        var parts = propertyPath.Split('.');
        object? currentObject = GetRootObject(parts[0]);

        if (currentObject == null)
            return null;

        // Идем по всем частям пути
        for (int i = 1; i < parts.Length; i++)
        {
            currentObject = GetPropertyValue(currentObject, parts[i]);
            if (currentObject == null)
                return null;
        }

        return currentObject;
    }

    // Получение корневого объекта (NodeMsg, Payload или Context)
    private object? GetRootObject(string rootName)
    {
        // Проверяем свойства NodeMsg
        if (_allProperties.TryGetValue(rootName, out var property))
        {
            var target = property.DeclaringType == typeof(NodeMsg) ? _nodeMsg : _payload;
            return property.GetValue(target);
        }

        // Проверяем Context
        var contextValue = _nodeMsg.Get(rootName);
        if (contextValue != null)
            return contextValue;

        return null;
    }

    // Получение значения свойства объекта
    private object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj == null)
            return null;

        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        if (property != null)
            return property.GetValue(obj);

        // Пробуем получить через индексатор или поле
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
            return field.GetValue(obj);

        return null;
    }

    // Установка значения свойства объекта
    private bool SetPropertyValue(object obj, string propertyName, object? value)
    {
        if (obj == null)
            return false;

        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        if (property != null && property.CanWrite)
        {
            // Проверяем совместимость типов
            if (value != null && property.PropertyType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(obj, value);
                return true;
            }
            else if (value == null && (!property.PropertyType.IsValueType ||
                     Nullable.GetUnderlyingType(property.PropertyType) != null))
            {
                property.SetValue(obj, value);
                return true;
            }
        }

        // Пробуем установить через поле
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
            return true;
        }

        return false;
    }

    private class SimpleSetMemberBinder : SetMemberBinder
    {
        public SimpleSetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase) { }
        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject? errorSuggestion)
            => throw new NotImplementedException();
    }

    private class SimpleGetMemberBinder : GetMemberBinder
    {
        public SimpleGetMemberBinder(string name, bool ignoreCase) : base(name, ignoreCase) { }
        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
            => throw new NotImplementedException();
    }
}
