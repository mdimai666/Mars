namespace Mars.Host.Data.Common;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; }
}
