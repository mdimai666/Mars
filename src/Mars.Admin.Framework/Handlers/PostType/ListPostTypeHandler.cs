using Mars.Contracts.PostTypes;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Framework.Handlers.PostType;

public class ListPostTypeHandler(IMarsWebApiClient client) : IListModelHandler<PostTypeListItemResponse, TablePostTypeQueryRequest>
{
    public Task<PagingResult<PostTypeListItemResponse>> Handle(TablePostTypeQueryRequest query)
        => client.PostType.ListTable(query);
}
