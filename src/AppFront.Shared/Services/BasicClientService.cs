using Mars.Core.Features;
using Mars.Shared.Common;

namespace AppFront.Shared.Services;

public class BasicClientService<TEntity>
{
    protected readonly StandartControllerClient<TEntity> controller;
    protected readonly QServer _client;
    protected readonly IServiceProvider serviceProvider;

    protected string _controllerName { get; set; }
    protected string _basePath { get; set; }
    protected Interfaces.IMessageService messageService { get; }

    public BasicClientService(IServiceProvider serviceProvider)
    {
        _basePath = "/api/";
        _controllerName = typeof(TEntity).Name;

        this.serviceProvider = serviceProvider;
        _client = serviceProvider.GetRequiredService<QServer>();
        controller = new StandartControllerClient<TEntity>(_client);
        messageService = serviceProvider.GetRequiredService<Interfaces.IMessageService>();
    }


    public virtual Task<TEntity?> Get(Guid id)
    {
        return controller.Get(id);
    }

    public virtual Task<PagingResult<TEntity>> ListTable(IBasicListQuery filter, string extraQuery = "")
    {
        return controller.ListTable(filter, extraQuery);
    }

    //public virtual async Task<TotalResponse<TEntity>> ListTable<TQueryModel>(QueryModel<TQueryModel> queryModel)
    //{
    //    return await controller.ListTable(queryModel.AsQueryFilter());
    //}

    public virtual async Task<UserActionResult<TEntity?>> Add(TEntity entity)
    {
        return await controller.Add(entity);
    }

    public virtual async Task<UserActionResult<TEntity>> Update(TEntity entity)
    {
        return await controller.Update(entity);

    }

    public virtual async Task<TEntity?> SmartSave(bool addNew, TEntity entity)
    {
        UserActionResult<TEntity?> result;

        if (addNew)
            result = await controller.Add(entity);
        else
            result = (await controller.Update(entity))!;

        if (result.Ok)
        {
            _ = messageService.Success(result.Message);
            return result.Data;
        }
        else
        {
            _ = messageService.Error(result.Message);
            return default;
        }
    }

    public virtual Task<UserActionResult> Delete(Guid id)
    {
        return controller.Delete(id);
    }

    //
    //public async Task<T> GetViewModel<T>()
    //{
    //    string name = typeof(T).Name;
    //    return await _client.GET<T>($"/vm/ViewModels/Get/{name}");

    //}

    public string QueryFilterAsQuery(BasicListQuery filter)
    {
        return controller.QueryFilterAsQuery(filter);
    }
}
