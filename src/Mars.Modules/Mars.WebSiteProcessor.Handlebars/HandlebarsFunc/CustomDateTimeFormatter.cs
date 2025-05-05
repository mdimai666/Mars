using HandlebarsDotNet;
using HandlebarsDotNet.IO;

namespace Mars.Host.Templators.HandlebarsFunc;

public sealed class CustomDateTimeFormatter : IFormatter, IFormatterProvider
{
    private readonly string _format;

    public CustomDateTimeFormatter(string format) => _format = format;

    public void Format<T>(T value, in EncodedTextWriter writer)
    {
        if (!(value is DateTime dateTime))
            throw new ArgumentException("supposed to be DateTime");

        writer.Write($"{dateTime.ToString(_format)}");
    }

    public bool TryCreateFormatter(Type type, out IFormatter formatter)
    {
        if (type != typeof(DateTime))
        {
            formatter = null;
            return false;
        }

        formatter = this;
        return true;
    }
}


