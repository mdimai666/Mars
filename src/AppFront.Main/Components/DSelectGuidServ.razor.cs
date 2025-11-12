using Mars.Shared.Models.Interfaces;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components;

public partial class DSelectGuidServ<TModel, TListQueryRequest>
    where TModel : IHasId
    where TListQueryRequest : IBasicListRequest
{

    Guid _value;
    [Parameter]
    public Guid Value
    {
        get => _value;
        set
        {
            if (value == _value) return;
            _value = value;
            ValueChanged.InvokeAsync(_value).Wait();

            var f = VarianList.FirstOrDefault(s => s.Id == _value);
            OnValueChange.InvokeAsync(f).Wait();
        }
    }
    [Parameter] public EventCallback<Guid> ValueChanged { get; set; }
    [Parameter] public EventCallback<TModel> OnValueChange { get; set; }

    [Parameter] public TListQueryRequest Query { get; set; } = default!;
    [Parameter] public Func<TListQueryRequest, Task<ListDataResult<TModel>>> ListAction { get; set; } = default!;

    [Parameter] public Func<TModel, string>? LabelExpression { get; set; }
    [Parameter] public string Placeholder { get; set; } = "-не выбрано-";

    IEnumerable<TModel> VarianList = new List<TModel>();

    string? errorMessage = null;
    //bool Busy = true;

    public string SetterStringId
    {
        get => _value.ToString();
        set
        {
            Guid id = Guid.Parse(value);
            Value = id;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Q.IsPrerenderProcess)
        {
            _ = Load();
        }
    }

    public Task Load()
    {
        Console.WriteLine("DEPRECATED");
        return Task.CompletedTask;
        //Busy = true;

        ////var query = new BasicListQuery(1, 100);
        //var query = Query;

        ////if (QueryExpression is not null)
        ////{
        ////    query.AddQuery<TModel>(QueryExpression);
        ////}

        //var result = await ListAction(query);
        //Busy = false;
        ////if (result.Ok)
        //{
        //    VarianList = result.Items;
        //    SelectNodes = new List<CascaderNode>{
        //        new CascaderNode {
        //            Value = Guid.Empty.ToString(),
        //            Label = Placeholder
        //        }
        //    };

        //    var list2 = VarianList.Select(s =>
        //        new CascaderNode
        //        {
        //            Value = s.Id.ToString(),
        //            Label = LabelExpression is not null ? LabelExpression(s) : s.Id.ToString()
        //        }).ToList();
        //    list2 = list2.OrderBy(s => s.Label).ToList();

        //    SelectNodes = SelectNodes.Concat(list2);
        //}
        ////else
        ////{
        ////    errorMessage = result.Message ?? "error";
        ////    SelectNodes = new List<CascaderNode>();
        ////}
        //StateHasChanged();
    }

}
