namespace Mars.Core.Extensions;

public static class WaitHelper
{
    /// <summary>
    /// Ожидает, пока функция не вернет не-null значение, или пока не истечет таймаут.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="valueProvider">Функция, возвращающая значение.</param>
    /// <param name="timeoutMs">Таймаут в миллисекундах.</param>
    /// <returns>Первое не-null значение или default(T), если таймаут истек.</returns>
    public static T? WaitForNotNull<T>(Func<T?> valueProvider, int timeoutMs, CancellationToken cancellationToken = default)
        where T : class
    {
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        var spinWait = new SpinWait();
        var startTime = Environment.TickCount;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = valueProvider();
            if (result != null)
                return result;

            if (Environment.TickCount - startTime >= timeoutMs)
                return default;

            spinWait.SpinOnce(); // Эффективное ожидание без блокировки потока
        }

        cancellationToken.ThrowIfCancellationRequested();
        return default;
    }
}
