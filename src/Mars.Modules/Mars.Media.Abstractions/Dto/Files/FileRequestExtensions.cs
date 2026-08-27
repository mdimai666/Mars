using Mars.Server.Abstractions.Extensions;
using Mars.Contracts.Files;

namespace Mars.Media.Abstractions.Dto.Files;

public static class FileRequestExtensions
{
    //public static CreateFileQuery ToQuery(this CreateFileRequest request, FileInfo fileInfo)
    //    => new()
    //    {
    //        Name = request.Name,
    //        Size = fileInfo.
    //    };

    //public static UpdateFileQuery ToQuery(this UpdateFileRequest request)
    //    => new()
    //    {
    //        Id = request.Id,
    //        Name = request.Name,
    //    };

    public static ListFileQuery ToQuery(this ListFileQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
            FolderId = request.FolderId,
        };

    public static ListFileQuery ToQuery(this TableFileQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
            FolderId = request.FolderId,
        };
}
