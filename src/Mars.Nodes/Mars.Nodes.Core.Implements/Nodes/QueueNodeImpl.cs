using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class QueueNodeImpl : INodeImplement<QueueNode>, INodeImplement
{
    public QueueNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    private readonly QueueState _state = new();

    public QueueNodeImpl(QueueNode node, IRED red)
    {
        Node = node;
        RED = red;
    }

    public class QueueState
    {
        public List<object> Items { get; set; } = [];
        public int ActiveTasks { get; set; } = 0;
        public int TotalProcessed { get; set; } = 0;
        public bool IsStopped { get; set; } = false;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        return parameters.InputPort switch
        {
            0 => EnqueueItem(input, callback, parameters),
            1 => ProcessStep(input, callback, parameters),
            _ => throw new NotImplementedException($"Input port {parameters.InputPort} is not implemented")
        };
    }

    /// <summary>
    /// Вход 0: Добавление нового элемента в очередь
    /// </summary>
    private Task EnqueueItem(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        lock (_state)
        {
            _state.Items.Add(input.Payload!);
            RED.Status(new NodeStatus($"In queue: {_state.Items.Count}, active: {_state.ActiveTasks}/{Node.MaxTask}"));
        }

        // Пытаемся немедленно отдать элемент на обработку, если есть свободные слоты
        return TryDispatchNext(input, callback, parameters);
    }

    /// <summary>
    /// Вход 1: Завершение предыдущего шага
    /// </summary>
    private Task ProcessStep(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        lock (_state)
        {
            if (_state.ActiveTasks > 0)
            {
                _state.ActiveTasks--;
                _state.TotalProcessed++;
            }
        }

        // Пытаемся отдать следующий элемент
        return TryDispatchNext(input, callback, parameters);
    }

    /// <summary>
    /// Логика извлечения следующего элемента с учетом FIFO/LIFO и MaxTask
    /// </summary>
    private Task TryDispatchNext(NodeMsg originalInput, ExecuteAction callback, ExecutionParameters parameters)
    {
        lock (_state)
        {
            if (_state.IsStopped)
            {
                return Task.CompletedTask;
            }

            // Если очередь пуста и нет активных задач, сигнализируем о завершении (Выход 0)
            if (_state.Items.Count == 0)
            {
                if (_state.ActiveTasks == 0 && _state.TotalProcessed > 0)
                {
                    RED.Status(new NodeStatus($"Complete. Total: {_state.TotalProcessed}"));

                    // Создаем сообщение о завершении и сбрасываем счетчик для следующей волны
                    NodeMsg completeMsg = originalInput.Copy(_state.TotalProcessed);
                    _state.TotalProcessed = 0;

                    callback(completeMsg, 0);
                }
                return Task.CompletedTask;
            }

            // Если достигнут лимит одновременных задач, ждем освобождения места (через вход 1)
            if (_state.ActiveTasks >= Node.MaxTask)
            {
                return Task.CompletedTask;
            }

            // Извлекаем элемент в зависимости от режима
            object nextItem;
            if (Node.Mode == QueueNode.EQueueMode.FIFO)
            {
                nextItem = _state.Items[0];
                _state.Items.RemoveAt(0);
            }
            else // LIFO
            {
                int lastIndex = _state.Items.Count - 1;
                nextItem = _state.Items[lastIndex];
                _state.Items.RemoveAt(lastIndex);
            }

            _state.ActiveTasks++;

            NodeMsg outMsg = originalInput.Copy(nextItem);

            RED.Status(new NodeStatus($"In queue: {_state.Items.Count}, active: {_state.ActiveTasks}/{Node.MaxTask}"));

            // Отправляем на Выход 1 (основной поток данных)
            callback(outMsg, 1);
        }

        return Task.CompletedTask;
    }
}
