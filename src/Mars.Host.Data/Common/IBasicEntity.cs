using System.ComponentModel.DataAnnotations;
using Mars.Host.Data.Entities;

namespace Mars.Host.Data.Common;

public interface IBasicEntity
{
    [Key]
    Guid Id { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset? ModifiedAt { get; set; }

}

public interface IBasicUserEntity : IBasicEntity
{
    Guid UserId { get; set; }
    UserEntity? User { get; set; }
}
