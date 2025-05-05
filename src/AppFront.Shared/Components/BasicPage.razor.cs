using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components
{
    public partial class BasicPage<TModel>
    {

        public bool Busy { get; set; } = false;

        [Parameter]
        public RenderFragment WhenLoading { get; set; } = default!;

        [Parameter]
        public RenderFragment<Exception> ErrorMessageTemplate { get; set; } = default!;

        [Parameter]
        public RenderFragment<TModel> ChildContent { get; set; } = default!;

        public Exception Error { get; set; } = default!;

        //public TModel model { get; set; }


        [Parameter]
        public Func<Task<TModel>> LoadFunc { get; set; } = default!;

        public bool Errored
        {
            get
            {
                return Error != null;
            }
        }

        //[Inject]
        //public DriverService service { get; set; }

        //[Parameter]
        //public Guid ID { get; set; }

        [Parameter]
        public TModel model { get; set; } = default!;

        [Parameter]
        public EventCallback<TModel> modelChanged { get; set; }

        protected override void OnInitialized()
        {
            if (Q.IsPrerender == false)
            {
                _ = StartLoad();
            }
        }

        //protected override void OnAfterRender(bool firstRender)
        //{
        //    base.OnAfterRender(firstRender);

        //    _ = StartLoad();
        //}


        public async virtual Task StartLoad()
        {
            if (Busy) return;
            Busy = true;
            StateHasChanged();

            try
            {
                //while (Q.Profile.Id == "0") {
                //    await Task.Delay(10);
                //}

                //Console.WriteLine("=1=");
                var task = LoadFunc();
                //Console.WriteLine("=2+"+task);
                model = await task;
                _ = modelChanged.InvokeAsync(model);

                //ChildContent(model);

                //if(model is Driver d)
                //{
                //    Console.WriteLine("=dr" + d?.FullName);

                //}
                //Console.WriteLine("=3=" + model);
                //model = await LoadFunc();

            }
            catch (Exception ex)
            {
                Error = ex;
                //throw;
            }
            finally
            {
                Busy = false;
            }

            StateHasChanged();

        }

        //public async virtual Task<TModel> LoadFunc()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
