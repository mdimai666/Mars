using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Mars.Media.Abstractions.Dto.Files;
using Mars.Media.Abstractions.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Tools;

/// <summary>
/// Инструменты агента: медиатека Mars (файлы в /upload/Media, миниатюры в /upload/MediaThumbs).
/// Экземпляр создаётся на каждый запуск с userId владельца чата — он становится
/// владельцем добавленных файлов.
/// </summary>
public class MarsMediaTools
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };
    private const int MaxDownloadBytes = 32 * 1024 * 1024;
    private const int DefaultReadChars = 8_000;
    private const int MaxReadChars = 30_000;

    private readonly IFileService _fileService;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MarsMediaTools> _logger;
    private readonly Guid _userId;

    public MarsMediaTools(
        IFileService fileService,
        IFileStorage fileStorage,
        IHttpClientFactory httpClientFactory,
        ILogger<MarsMediaTools> logger,
        Guid userId)
    {
        _fileService = fileService;
        _fileStorage = fileStorage;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _userId = userId;
    }

    [Description("Показать список файлов медиатеки (новые сверху). Возвращает id, имя, URL, размер и признак картинки.")]
    public async Task<string> ListMedia(
        [Description("Поиск по подстроке в имени файла, опционально")] string? search = null,
        [Description("Максимум записей (1-50, по умолчанию 20)")] int take = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = new ListFileQuery
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Take = Math.Clamp(take, 1, 50),
            };
            var result = await _fileService.List(query, ct);

            var files = result.Items.Select(f => new
            {
                id = f.Id,
                name = f.Name,
                url = f.UrlRelative,
                size = f.Size,
                image = f.IsImage,
                created = f.CreatedAt.ToString("yyyy-MM-dd"),
            });

            return JsonSerializer.Serialize(new { ok = true, total = result.TotalCount, hasMore = result.HasMoreData, files }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: ListMedia failed");
            return Error(ex.GetBaseException().Message);
        }
    }

    [Description("Получить детали медиафайла по id: URL, размер, расширение; для картинок — размеры и миниатюры.")]
    public async Task<string> GetMedia(
        [Description("Идентификатор медиафайла (Guid)")] Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var detail = await _fileService.GetDetail(id, ct);
            if (detail is null)
                return Error($"Файл '{id}' не найден");

            var thumbnails = detail.Meta?.Thumbnails?.Values.Select(t => new
            {
                name = t.Name,
                url = t.FileUrl,
                t.Width,
                t.Height,
            }) ?? [];

            return JsonSerializer.Serialize(new
            {
                ok = true,
                file = new
                {
                    id = detail.Id,
                    detail.Name,
                    url = detail.UrlRelative,
                    detail.Size,
                    detail.Ext,
                    image = detail.IsImage,
                    width = detail.Meta?.ImageInfo?.Width,
                    height = detail.Meta?.ImageInfo?.Height,
                    thumbnails,
                },
            }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: GetMedia {FileId} failed", id);
            return Error(ex.GetBaseException().Message);
        }
    }

    [Description("Прочитать текстовое содержимое медиафайла напрямую с диска (без HTTP). " +
                 "Кодировка определяется автоматически (BOM → UTF-8 → windows-1251) или задаётся параметром encoding. " +
                 "Большие файлы читай фрагментами через offset.")]
    public async Task<string> ReadMediaFile(
        [Description("Идентификатор медиафайла (Guid)")] Guid id,
        [Description("Отступ в символах, с которого читать (по умолчанию 0)")] int offset = 0,
        [Description("Кодировка файла: utf-8, windows-1251, koi8-r, cp866, utf-16 и т.п.; если не указана — определится автоматически")] string? encoding = null,
        [Description("Максимум возвращаемых символов (по умолчанию 8000, максимум 30000)")] int maxChars = DefaultReadChars,
        CancellationToken ct = default)
    {
        try
        {
            var detail = await _fileService.GetDetail(id, ct);
            if (detail is null)
                return Error($"Файл '{id}' не найден");

            var bytes = _fileStorage.Read(detail.FilePhysicalPath);
            if (bytes.Length == 0)
                return JsonSerializer.Serialize(new { ok = true, name = detail.Name, text = "", length = 0, offset = 0, truncated = false }, SerializerOptions);

            if (IsBinary(bytes))
                return Error("Файл выглядит бинарным — содержимое недоступно как текст");

            var (text, usedEncoding) = DecodeText(bytes, encoding);

            var take = Math.Clamp(maxChars, 1, MaxReadChars);
            var start = Math.Clamp(offset, 0, text.Length);
            var slice = text.Length > start + take ? text.Substring(start, take) : text[start..];
            var truncated = start + take < text.Length;

            return JsonSerializer.Serialize(new
            {
                ok = true,
                name = detail.Name,
                encoding = usedEncoding,
                length = text.Length,
                offset = start,
                truncated,
                text = slice,
            }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: ReadMediaFile {FileId} failed", id);
            return Error(ex.GetBaseException().Message);
        }
    }

    [Description("Скачать файл по внешнему http/https URL и добавить его в медиатеку. Возвращает id и URL нового файла.")]
    public async Task<string> AddMedia(
        [Description("http/https URL файла")] string url,
        [Description("Имя файла с расширением; если не указано — берётся из URL")] string? name = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return Error("Нужен абсолютный http/https URL");

            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return Error($"Не удалось скачать: HTTP {(int)response.StatusCode}");

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);

            if (buffer.Length == 0)
                return Error("Скачанный файл пуст");
            if (buffer.Length > MaxDownloadBytes)
                return Error("Файл больше 32 МБ — не добавлен");

            var fileName = SanitizeFileName(name, uri);
            var subpath = $"Media/AiChat/{DateTimeOffset.Now.Year}";
            var fileId = await _fileService.WriteUpload(fileName, subpath, buffer.ToArray(), _userId, ct);

            var detail = await _fileService.GetDetail(fileId, ct);
            _logger.LogInformation("AiChat: media file added from {Url} → {FileId} ({Name})", url, fileId, fileName);

            return JsonSerializer.Serialize(new { ok = true, id = fileId, name = fileName, url = detail?.UrlRelative }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: AddMedia {Url} failed", url);
            return Error(ex.GetBaseException().Message);
        }
    }

    [Description("Удалить файл из медиатеки вместе с миниатюрами. Необратимо — сначала подтверди у пользователя через ask_user.")]
    public async Task<string> DeleteMedia(
        [Description("Идентификатор медиафайла для удаления (Guid)")] Guid id,
        CancellationToken ct = default)
    {
        try
        {
            var deleted = await _fileService.Delete(id, ct);
            _logger.LogInformation("AiChat: media file deleted {FileId} ({Name})", deleted.Id, deleted.Name);
            return JsonSerializer.Serialize(new { ok = true, deleted = new { id = deleted.Id, deleted.Name } }, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: DeleteMedia {FileId} failed", id);
            return Error(ex.GetBaseException().Message);
        }
    }

    /// <summary>
    /// Декодирует байты в текст: запрошенная кодировка → BOM → строгий UTF-8 → windows-1251.
    /// Возвращает текст и имя использованной кодировки (с пометкой, если она угадана).
    /// </summary>
    static (string Text, string Encoding) DecodeText(byte[] bytes, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            // неизвестная кодировка бросит ArgumentException — уйдёт в ошибку инструмента
            var enc = Encoding.GetEncoding(requested.Trim());
            return (enc.GetString(bytes), enc.WebName);
        }

        if (bytes.Length >= 3 && bytes[0] is 0xEF && bytes[1] is 0xBB && bytes[2] is 0xBF)
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), "utf-8 (BOM)");
        if (bytes.Length >= 2 && bytes[0] is 0xFF && bytes[1] is 0xFE)
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), "utf-16le (BOM)");
        if (bytes.Length >= 2 && bytes[0] is 0xFE && bytes[1] is 0xFF)
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), "utf-16be (BOM)");

        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            return (strictUtf8.GetString(bytes), "utf-8");
        }
        catch (DecoderFallbackException)
        {
            // самая частая «легаси»-кодировка для текстов на сайте
            return (Encoding.GetEncoding("windows-1251").GetString(bytes), "windows-1251 (auto)");
        }
    }

    static bool IsBinary(byte[] bytes)
    {
        var probe = Math.Min(bytes.Length, 8_000);
        for (var i = 0; i < probe; i++)
        {
            if (bytes[i] == 0) return true;
        }
        return false;
    }

    static string SanitizeFileName(string? name, Uri uri)
    {
        var candidate = string.IsNullOrWhiteSpace(name)
            ? uri.Segments.LastOrDefault()?.Trim('/') ?? ""
            : name.Trim();

        candidate = string.Join("_", candidate.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        if (candidate == "")
            candidate = "file";
        if (Path.GetExtension(candidate) == "")
            candidate += ".bin";

        return candidate.Length <= 200 ? candidate : candidate[^200..];
    }

    static string Error(string message)
        => JsonSerializer.Serialize(new { ok = false, error = message }, SerializerOptions);
}
