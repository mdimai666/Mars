using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

//public record ModelTypeInfo
//{
//    public required string Name { get; set; }
//    public required string Title { get; set; }
//    public required bool IsPlugin { get; set; } = true;

//    public IReadOnlyCollection<ModelSubTypeInfo>? SubTypes;

//    public PostTypeModelInfoResponse ToResponse()
//        => new()
//        {
//            Title = Title,
//            Name = Name,
//            IsPlugin = IsPlugin,
//            SubTypes = SubTypes?.Select(s => s.ToResponse()).ToList()
//        };
//}

//public class ModelSubTypeInfo
//{
//    public required string Name { get; set; }
//    public required string Title { get; set; }

//    public PostTypeModelSubTypeInfoResponse ToResponse()
//        => new()
//        {
//            Name = Name,
//            Title = Title
//        };

//}
