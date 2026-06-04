using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class QueueNodeImpl : INodeImplement<QueueNode>, INodeImplement
{
    public QueueNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

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
        // Получаем или создаем состояние очереди
        var state = input.Get<QueueState>() ?? new QueueState();
        if (input.Get<QueueState>() == null)
        {
            input.Add(state);
        }

        lock (state)
        {
            state.Items.Add(input.Payload!);
            RED.Status(new NodeStatus($"In queue: {state.Items.Count}, active: {state.ActiveTasks}/{Node.MaxTask}"));
        }

        // Пытаемся немедленно отдать элемент на обработку, если есть свободные слоты
        return TryDispatchNext(state, input, callback, parameters);
    }

    /// <summary>
    /// Вход 1: Завершение предыдущего шага
    /// </summary>
    private Task ProcessStep(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var state = input.Get<QueueState>();
        if (state == null)
        {
            return Task.CompletedTask; // Нет состояния, нечего обрабатывать
        }

        if (state.ActiveTasks > 0)
        {
            lock (state)
            {
                state.ActiveTasks--;
                state.TotalProcessed++;
            }
        }

        // Пытаемся отдать следующий элемент
        return TryDispatchNext(state, input, callback, parameters);
    }

    /// <summary>
    /// Логика извлечения следующего элемента с учетом FIFO/LIFO и MaxTask
    /// </summary>
    private Task TryDispatchNext(QueueState state, NodeMsg originalInput, ExecuteAction callback, ExecutionParameters parameters)
    {
        lock (state)
        {
            if (state.IsStopped)
            {
                return Task.CompletedTask;
            }

            // Если очередь пуста и нет активных задач, сигнализируем о завершении (Выход 0)
            if (state.Items.Count == 0)
            {
                if (state.ActiveTasks == 0 && state.TotalProcessed > 0)
                {
                    RED.Status(new NodeStatus($"Complete. Total: {state.TotalProcessed}"));

                    // Создаем сообщение о завершении и сбрасываем счетчик для следующей потенциальной волны
                    NodeMsg completeMsg = originalInput.Copy(state.TotalProcessed);
                    state.TotalProcessed = 0;

                    callback(completeMsg, 0);
                }
                return Task.CompletedTask;
            }

            // Если достигнут лимит одновременных задач, ждем освобождения места (через вход 1)
            if (state.ActiveTasks >= Node.MaxTask)
            {
                return Task.CompletedTask;
            }

            // Извлекаем элемент в зависимости от режима
            object nextItem;
            if (Node.Mode == QueueNode.EQueueMode.FIFO)
            {
                nextItem = state.Items[0];
                state.Items.RemoveAt(0); // Удаляем первый элемент
            }
            else // LIFO
            {
                int lastIndex = state.Items.Count - 1;
                nextItem = state.Items[lastIndex];
                state.Items.RemoveAt(lastIndex); // Удаляем последний элемент
            }

            state.ActiveTasks++;

            // Готовим сообщение для следующего узла
            NodeMsg outMsg = originalInput.Copy(nextItem);
            outMsg.Set(state); // Обязательно передаем состояние дальше, чтобы оно вернулось на Вход 1

            RED.Status(new NodeStatus($"In queue: {state.Items.Count}, active: {state.ActiveTasks}/{Node.MaxTask}"));

            // Отправляем на Выход 1 (основной поток данных)
            callback(outMsg, 1);
        }

        return Task.CompletedTask;
    }
}
