namespace AppFront.Shared.Handlers;

public interface IListModelHandler<TModel, TQuery>
    where TQuery : IBasicTableRequest
{
    Task<PagingResult<TModel>> Handle(TQuery query);
}
