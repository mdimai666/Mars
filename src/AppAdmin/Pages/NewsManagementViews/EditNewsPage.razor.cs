using AntDesign;

using AppShared.Models;
using Blazored.TextEditor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppAdmin.Pages.NewsManagementViews
{
    public partial class EditNewsPage
    {
        [Parameter]
        public Guid ID { get; set; }

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PostService service { get; set; } = default!;
        [Inject] public MessageService _message { get; set; } = default!;
        [Inject] public HttpClient _client { get; set; } = default!;

        Form<Post> form = new Form<Post>();

        public Post _model = new Post();

        //public void OnFinish(EditContext editContext)

        bool _addNewItem = true;

        Dictionary<string, string> headers = new Dictionary<string, string>();
        //string UploadAction;
        List<UploadFileItem> fileList = new List<UploadFileItem>();

        //InputFile inputFile;

        public string Accept = ".jpg,.png,.jpeg,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.pdf";

        WysiwygEditor? QuillHtml;

        string edit_html = "";

        //public ICollection<FileEntity> Files {get;set; } = new List<FileEntity>();


        protected override void OnInitialized()
        {
            //headers.Add("Authorization", "Bearer " + Q.AuthToken);
            //UploadAction = _client.BaseAddress.ToString() + "api/Document/Upload";
            _model = new Post()
            {
                //File = new FileEntity()
                Slug = Guid.NewGuid().ToString(),
            };
            form = new Form<Post>();
            base.OnInitialized();

            _ = Load();
        }

        public async Task Load()
        {
            if (ID != Guid.Empty)
            {
                var app = await service.Get(ID);
                //var f = app.File.FileName;
                //var ext = string.IsNullOrEmpty(app.File.FileExt) ? "txt" : app.File.FileExt;
                //Console.WriteLine($"f={f}");
                //fileList.Add(new UploadFileItem
                //{
                //    Id = app.File.Id.ToString(),
                //    FileName = f.Contains(ext) ? f : $"{f}.{ext}",
                //    //Percent = 100,//2
                //    //Ext = "." + app.File.FileExt,//2
                //    //Type = "image/" + app.File.FileExt,//2
                //    //Size = (long)app.File.FileSize,//2
                //    State = UploadState.Success,
                //    Url = app.File.FileUrl,
                //    //ObjectURL = app.File.FileUrl,//2
                //    //Response = JsonConvert.SerializeObject(new ResponseUploadFile(app.File))//2,
                //});
                _model = app;
                edit_html = _model.Content;
                _addNewItem = false;
                StateHasChanged();
            }
        }

        public async Task OnFinish(EditContext editContext)
        {
            Post a;
            _model.Content = await QuillHtml.GetHTML();

            a = await service.SmartSave(_addNewItem, _model);


            if (a is not null && _addNewItem)
            {
                //_ = _message.Success("Сохранено");
                NavigationManager.NavigateTo($"/dev/EditNews/{a.Id}", replace: true);
                _addNewItem = false;
                _ = Load();
            }
            StateHasChanged();
        }

        public void OnChangeFile(IReadOnlyList<IBrowserFile> files)
        {
            Console.WriteLine("12");
        }

        //void OnSingleCompleted(UploadInfo fileinfo)
        //{
        //    if (fileinfo.File.State == UploadState.Success)
        //    {
        //        var result = fileinfo.File.GetResponse<ResponseUploadFile>();
        //        fileinfo.File.Url = result.url;
        //        _model.FileId = result.file_id;
        //    }

        //}

        //void HandleChange(UploadInfo fileinfo)
        //{
        //    if (fileList.Count > 1)
        //    {
        //        fileList.RemoveRange(0, fileList.Count - 1);
        //        //fileList.Clear();
        //    }
        //    fileList.Where(file => file.State == UploadState.Success && !string.IsNullOrWhiteSpace(file.Response)).ForEach(file =>
        //    {
        //        var result = file.GetResponse<ResponseUploadFile>();
        //        file.Url = result.url;
        //    });
        //}

        public async void OnDeleteClick()
        {
            var result = await service.Delete(_model.Id);

            if (result.Ok == true)
            {
                _ = _message.Success(result.Message);
                NavigationManager.NavigateTo("/NewsManagement");
            }
            else
            {
                _ = _message.Error(result.Message);
            }
        }


    }
}
