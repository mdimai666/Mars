using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using AntDesign;
using AppShared.Models;
using System.Text.Json;
using AntDesign.TableModels;
using AppFront.Shared.Models;

namespace AppAdmin.Pages.ActionHistoryViews
{
    public partial class ActionHistoryPage
    {
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        public List<ActionHistory>? Items = null;
        [Inject] public ActionHistoryService service { get; set; } = default!;
        [Inject] public MessageService _message { get; set; } = default!;


        ActionHistory _model = new ActionHistory();
        Form<ActionHistory> form = new Form<ActionHistory>();

        ITable? table;
        int _pageIndex = 1;
        int _pageSize = 10;
        int _total = 0;

        bool _visible = false;


        void OnChange2()
        {
            _ = OnChange((table.GetQueryModel() as QueryModel<ActionHistory>)!);
        }


        public async Task OnChange(QueryModel<ActionHistory> queryModel)
        {

            Console.WriteLine(JsonSerializer.Serialize(queryModel));

            var data = await service.ListTable(queryModel.AsQueryFilter());

            if (data.Ok)
            {
                Items = data.Records.ToList();
                _total = data.TotalCount;

                Console.WriteLine(Items.Count);

                StateHasChanged();
            }
        }

        public async Task Delete(Guid id)
        {
            var result = await service.Delete(id);
            if (result.Ok)
            {
                _ = _message.Success(result.Message);
            }
            else
            {
                _ = _message.Error(result.Message);
            }
            OnChange2();
        }


        public void EditClick(ActionHistory contest)
        {
            _model = contest;
            _visible = true;
        }


        private void HandleCancel()
        {
            //Console.WriteLine(e);
            _visible = false;
        }

    }
}
