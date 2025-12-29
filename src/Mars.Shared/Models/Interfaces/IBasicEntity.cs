using Mars.Core.Interfaces;

namespace Mars.Shared.Models.Interfaces;

public interface IBasicEntity : IHasId
{
    //Guid Id { get;  }

    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? ModifiedAt { get; }

}

//public interface IBasicUserEntity : IBasicEntity
//{
//    Guid UserId { get; set; }
//    UserSummaryResponse? User { get; set; }
//}
