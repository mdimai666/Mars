using System.ComponentModel.DataAnnotations;
using Mars.Shared.Models.Interfaces;

namespace Mars.Shared.Contracts.Common;

//public class BasicEntityResponse : IBasicEntityResponse
//{
//    public required Guid Id { get; init; }
//    public required DateTime CreatedAt { get; init; }
//    //public required DateTime ModifiedAt { get; init; }
//}

public interface IBasicEntityResponse : IHasId
{
    //[Display(Name = "ИД")]
    //Guid Id { get; }
    [Display(Name = "Создан")]
    DateTimeOffset CreatedAt { get; }
    //[Display(Name = "Изменен")]
    //DateTime ModifiedAt { get; }

}
