using System.Collections;
using System.Reflection;
using Mars.Nodes.Core.Nodes.Sequences;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes.Sequences;

public class SplitNodeImpl : INodeImplement<SplitNode>
{
    public SplitNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public SplitNodeImpl(SplitNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var payload = input.Payload;

        if (payload == null)
        {
            throw new ArgumentException("Payload is null");
        }

        if (payload is string str)
        {
            if (string.IsNullOrEmpty(Node.Delimiter))
            {
                foreach (char c in str)
                {
                    callback(input.Copy(c.ToString()));
                }
            }
            else
            {
                var parts = str.Split([Node.Delimiter], StringSplitOptions.None);
                foreach (var part in parts)
                {
                    callback(input.Copy(part), 0);
                }
            }
            return Task.CompletedTask;
        }

        if (payload is IEnumerable enumerable && payload is not string)
        {
            foreach (var item in enumerable)
            {
                callback(input.Copy(item), 0);
            }
            return Task.CompletedTask;
        }

        // 3. Если это сложный объект (не примитив и не коллекция)
        // Разбиваем на свойства объекта
        var type = payload.GetType();

        // Игнорируем примитивные типы (int, double, bool и т.д.), у них нет осмысленных свойств для разбиения
        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(DateTime))
        {
            throw new ArgumentException($"Нет свойств для разбиения на объект {type.Name}");
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(payload);

            // Формируем удобный объект для передачи дальше: имя свойства и его значение
            var propertyItem = new
            {
                PropertyName = prop.Name,
                Value = value
            };

            callback(input.Copy(propertyItem), 0);
        }

        return Task.CompletedTask;
    }
}
