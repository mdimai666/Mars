using Mars.Admin.Framework.Extensions;
using Mars.Core.Exceptions;
using Mars.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MediaViews;

/// <summary>
/// Общая зона загрузки файлов (перетаскивание или выбор).
/// Авто-режим (<see cref="AutoUpload"/>) — сразу грузит файлы в медиа
/// (папка <see cref="FolderId"/> или <see cref="FolderPath"/>, пусто = папка года)
/// и сообщает об успешных загрузках через <see cref="OnFilesUploaded"/>.
/// Ручной режим — только передаёт выбранные файлы родителю через <see cref="OnFilesSelected"/>.
/// </summary>
public partial class MediaUploadZone
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    [Parameter] public string Label { get; set; } = "Загрузка файла";
    [Parameter] public string Accept { get; set; } = "*";
    [Parameter] public bool Multiple { get; set; } = true;
    [Parameter] public int MaximumFileCount { get; set; } = 50;
    [Parameter] public long MaximumFileSize { get; set; } = 1000L * 1024 * 1024;
    [Parameter] public string ZoneStyle { get; set; } = "min-height: 100px; max-width: 300px;";

    /// <summary>Авто-режим: файлы грузятся в медиа сразу при выборе</summary>
    [Parameter] public bool AutoUpload { get; set; } = true;

    /// <summary>Папка загрузки по ИД (авто-режим; приоритет над <see cref="FolderPath"/>)</summary>
    [Parameter] public Guid? FolderId { get; set; }

    /// <summary>Папка загрузки по пути (авто-режим; пусто = папка года)</summary>
    [Parameter] public string? FolderPath { get; set; }

    /// <summary>Авто-режим: успешно загруженные файлы (после завершения всей пачки)</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<FileDetailResponse>> OnFilesUploaded { get; set; }

    /// <summary>Ручной режим: выбранные файлы (загрузка — на родителе)</summary>
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnFilesSelected { get; set; }

    /// <summary>Показывать список загруженного после завершения пачки</summary>
    [Parameter] public bool ShowResults { get; set; } = true;

    /// <summary>Содержимое зоны вместо дефолтной подписи (например, плитка значения — дроп поверх неё)</summary>
    [Parameter] public RenderFragment? ZoneContent { get; set; }

    /// <summary>Клик по содержимому зоны открывает нативный диалог выбора файлов</summary>
    [Parameter] public bool ZoneClickable { get; set; }

    readonly string _inputFileId = "media-upload-zone_" + Guid.NewGuid().ToString("N");
    readonly List<FileUploadResult> _results = [];
    readonly List<FileDetailResponse> _uploaded = [];
    int _progressPercent;

    async Task OnFileUploadedHandler(FluentInputFileEventArgs file)
    {
        if (!AutoUpload) return;

        try
        {
            var uploaded = await client.Media.Upload(file.Stream!, file.Name, FolderId, FolderPath);
            _uploaded.Add(uploaded);
            _results.Add(new FileUploadResult(uploaded.Name, uploaded.Size, null));
        }
        catch (MarsValidationException ex)
        {
            _results.Add(new FileUploadResult(file.Name, (ulong)file.Size, string.Join("; ", ex.Errors.Values.SelectMany(x => x))));
        }
        catch (Exception ex)
        {
            _results.Add(new FileUploadResult(file.Name, (ulong)file.Size, ex.Message));
        }
    }

    async Task OnCompletedHandler(IEnumerable<FluentInputFileEventArgs> files)
    {
        if (!AutoUpload) return;

        if (_uploaded.Count > 0)
        {
            var uploaded = _uploaded.ToArray();
            _uploaded.Clear();
            await OnFilesUploaded.InvokeAsync(uploaded);
        }

        StateHasChanged();
    }

    class FileUploadResult
    {
        public string Name { get; init; }
        public long Size { get; init; }
        public string? ErrorMessage { get; init; }

        public FileUploadResult(string name, ulong size, string? errorMessage)
        {
            Name = name;
            Size = (long)size;
            ErrorMessage = errorMessage;
        }
    }
}
