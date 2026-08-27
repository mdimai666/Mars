namespace Mars.Contracts.PostTypes;

public class PostTypePresentationEditViewModel
{
    public required PostTypeSummaryResponse PostType { get; init; }
    public required PostTypePresentationResponse Presentation { get; init; }

}
