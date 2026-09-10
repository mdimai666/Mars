namespace Mars.Data.Common;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; }
}
