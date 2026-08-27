namespace Mars.Datasource.Abstractions.Exceptions;

public class DatasourceOperationException : Exception
{
    public DatasourceOperationException(string? message) : base(message)
    {
    }

    public DatasourceOperationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
