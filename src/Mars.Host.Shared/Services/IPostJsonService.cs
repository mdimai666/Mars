using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Common;
using Newtonsoft.Json.Linq;

namespace Mars.Host.Shared.Services;

public interface IPostJsonService
{
    Task<PostJsonDto?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostJsonDto?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken);

    //public Task<PagingResult<JToken>> ListTableJson(QueryFilter filter, string type, Expression<Func<Post, bool>>? predicate = null);

    //public Task<JToken?> GetAsJson(Expression<Func<Post, bool>> predicate);

    public JObject AsJson22(object pctx);
}
