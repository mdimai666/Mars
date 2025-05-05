using System.Net.Http.Headers;
using System.Text.Json;
using AppFront.Shared.Services.GallerySpace;
using AppShared.Dto.Gallery;
using AppShared.Models;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews.SpecialViews;

public partial class GalleryFluentFileUploader1
{
    FluentInputFile? fileinput;

    // FluentInputFileEventArgs[] Files = Array.Empty<FluentInputFileEventArgs>();
    List<FileEntity> Files = new();

    //[Inject] public MediaService service { get; set; } = default!;

    [Parameter]
    public long MaximumFileSize { get; set; } = 5000000;

    [Parameter]
    public int MaximumFileCount { get; set; } = 1;

    [Parameter]
    public bool ReadOnly { get; set; }

    
    [Parameter]
    public Guid GalleryId { get; set; }

    [Parameter]
    public EventCallback<List<FileEntity>> OnAfterUpload { get; set; }


    [Parameter]
    public RenderFragment<FileEntity>? ItemActionBottom { get; set; } = null;

    [Inject]
    ILogger<GalleryFluentFileUploader1> _logger { get; set; } = default!;

    [Inject] HttpClient httpClient { get; set; } = default!;

    [Inject] QServer _client { get; set; } = default!;

    [Inject] GalleryService galleryService { get; set; } = default!;

    private bool shouldRender;
    protected override bool ShouldRender() => shouldRender;

    async void OnInputFileChange(InputFileChangeEventArgs e)
    {
        // https://learn.microsoft.com/ru-ru/aspnet/core/blazor/file-uploads?view=aspnetcore-7.0
        // var file = e.File;
        // foreach (var file in e.)
        // {
        //Console.WriteLine($"file: {e.File.Name}");
        // }

        var maxAllowedFiles = MaximumFileCount;


        shouldRender = false;
        long maxFileSize = MaximumFileSize;
        var upload = false;

        using var content = new MultipartFormDataContent();

        foreach (var file in e.GetMultipleFiles(maxAllowedFiles))
        {
            if (Files.SingleOrDefault(
                f => f.FileName == file.Name) is null)
            {
                try
                {
                    // files.Add(new() { Name = file.Name });

                    var fileContent =
                        new StreamContent(file.OpenReadStream(maxFileSize));

                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue(file.ContentType);

                    content.Add(
                        content: fileContent,
                        // name: "\"files\"",
                        name: "\"files\"",
                        fileName: file.Name);

                    upload = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        "{FileName} not uploaded (Err: 6): {Message}",
                        file.Name, ex.Message);

                    // uploadResults.Add(
                    //     new()
                    //         {
                    //             FileName = file.Name,
                    //             ErrorCode = 6,
                    //             Uploaded = false
                    //         });
                }
            }
        }

        if (upload)
        {
            // var client = ClientFactory.CreateClient();
            var client = httpClient;

            var postUrl = Q.ServerUrlJoin($"api/Gallery/PostPhotos/{GalleryId}/?");

            // var response = await client.PostAsync(Q.ServerUrlJoin("/api/Post/Upload"), content);
            var response = await client.PostAsync(postUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                using var responseStream =
                    await response.Content.ReadAsStreamAsync();

                // var newUploadResults = await JsonSerializer
                //     .DeserializeAsync<IList<UploadResult>>(responseStream, options);
                var newUploadResults = await JsonSerializer
                    .DeserializeAsync<UserActionResult<List<FileEntity>>>(responseStream, options);

                if (newUploadResults is not null)
                {
                    // uploadResults = uploadResults.Concat(newUploadResults).ToList();
                    Files.AddRange(newUploadResults.Data);

                    if (newUploadResults.Ok)
                    {
                        _ = OnAfterUpload.InvokeAsync(newUploadResults.Data);
                    }
                }
            }
        }

        shouldRender = true;
        StateHasChanged();
    }

    void OnCompleted(IEnumerable<FluentInputFileEventArgs> files)
    {
        // Files = files.ToArray();

        // // For the demo, delete these files.
        foreach (var file in Files)
        {
            Console.WriteLine($"file: {file.FileName}");
            // file.Stream.
            //     file.LocalFile?.Delete();
        }
    }

    void OnError(FluentInputFileEventArgs file)
    {
        // DemoLogger.WriteLine($"Error: {file.ErrorMessage}");
    }

    string FileHintTitle(FileEntity file)
    {
        return $"{file.FileName}\nsize: {file.FileSize.ToHumanizedSize()}\n{file.Meta?.ImageInfo?.Width ?? 0}x{file.Meta?.ImageInfo?.Height ?? 0}";
    }

    async void ItemRemoveClick(FileEntity file)
    {
        var id = file.Id;
        //Files.Remove(file);
        var del = await DeleteFileEntity(id);

        if (del.Ok)
        {
            var item = Files.First(s => s.Id == id);
            Files.Remove(item);
        }
        StateHasChanged();
    }

    public async Task<UserActionResult> DeleteFileEntity(Guid id)
    {
        throw new NotImplementedException();

        //string _basePath = "/api/";
        //string _controllerName = "Media";
        //var result = await _client.DELETE<UserActionResult>($"{_basePath}{_controllerName}/DeleteFileEntity/{id}");

        //return result!;
    }

}
