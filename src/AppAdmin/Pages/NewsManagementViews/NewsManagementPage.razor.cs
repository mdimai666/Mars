using AntDesign;
using AntDesign.TableModels;
using AppFront.Shared.Models;
using AppShared.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppAdmin.Pages.NewsManagementViews
{
    public partial class NewsManagementPage
    {

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PostService service { get; set; } = default!;
        [Parameter] public List<Post>? Items { get; set; } = null;

        ITable? table;
        int _pageIndex = 1;
        int _pageSize = 10;
        int _total = 0;


        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        void Delete(Guid id)
        {

        }

        public async Task OnChange(QueryModel<Post> queryModel)
        {
            Console.WriteLine(JsonSerializer.Serialize(queryModel));


            var data = await service.ListTable(queryModel.AsQueryFilter());
            if (data.Ok)
            {
                Items = data.Records.ToList();
                _total = data.TotalCount;

                Console.WriteLine(Items.Count);
            }
        }

    }
}
