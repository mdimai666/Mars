using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// Файлы источника как таблицы: CSV и листы XLSX из медиа-хранилища Mars или с диска хоста.
/// Запрос — предикат Dynamic LINQ (<c>Val.Num(age) &gt; 30</c>), пустой запрос отдаёт все строки.
/// </summary>
public class FileDatasourceProvider : IDatasourceProvider
{
    /// <summary>Файл читается целиком в память, поэтому у выборки есть жёсткий предел.</summary>
    public const int MaxSourceRows = 100_000;

    /// <summary>Разделитель листа в идентификаторе объекта: <c>книга.xlsx#Лист1</c>.
    /// В пути файла он практически не встречается, а ':' занят диском в Windows-путях.</summary>
    public const char SheetSeparator = '#';

    readonly DatasourceConfig _config;
    readonly IDatasourceFileSource _files;
    readonly IReadOnlyList<ITabularFileReader> _readers;
    readonly FileSourceSettings _settings;

    public FileDatasourceProvider(DatasourceConfig config, IDatasourceFileSource files, DatasourceKindProfile? profile = null)
        : this(config, files, [new CsvTabularFileReader(), new XlsxTabularFileReader()], profile)
    {
    }

    public FileDatasourceProvider(DatasourceConfig config, IDatasourceFileSource files, IReadOnlyList<ITabularFileReader> readers,
        DatasourceKindProfile? profile = null)
    {
        _config = config;
        _files = files;
        _readers = readers;
        _settings = FileSourceSettings.From(config);
        Profile = profile ?? FileDatasourceProfile.Create();
    }

    public DatasourceKindProfile Profile { get; }

    public Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
    {
        DatasourceCatalogGroup group = new() { Name = "" };

        foreach (var reference in _settings.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Reader(reference) is not { } reader) continue;

            using var stream = _files.OpenRead(reference);

            var sheets = reader.Read(stream, SampleOptions());
            var singleSheet = sheets.Count == 1;

            foreach (var sheet in sheets)
            {
                group.Objects.Add(ToObject(reference, sheet, singleSheet));
            }
        }

        DatasourceCatalog catalog = new()
        {
            SourceName = _config.Label,
            Profile = Profile,
            Groups = group.Objects.Count > 0 ? [group] : [],
        };

        return Task.FromResult(catalog);
    }

    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        QueryResultDto result = new()
        {
            Kind = DatasourceKind.File,
            Command = request.Query,
        };

        try
        {
            var sheet = Read(request.ObjectId, MaxSourceRows, cancellationToken);

            result.Fields = Fields(sheet).ToArray();

            IQueryable<Dictionary<string, string?>> rows = sheet.Rows.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                rows = rows.Where(FileQueryConfig.ParsingConfig, request.Query);
            }

            var matched = rows.ToList();
            var take = request.MaxRows > 0 ? Math.Min(request.MaxRows, matched.Count) : matched.Count;

            result.Rows = matched
                .Take(take)
                .Select(row => sheet.Columns.Select(column => row.TryGetValue(column, out var value) ? value : null).ToArray())
                .ToArray();

            result.Truncated = sheet.Truncated || matched.Count > take;
            result.Ok = true;
            result.Message = "success";
        }
        catch (Exception ex)
        {
            result.Ok = false;
            result.Message = QueryResultMapping.Error(ex);
        }

        stopwatch.Stop();
        result.ElapsedMs = stopwatch.ElapsedMilliseconds;

        return Task.FromResult(result);
    }

    public Task<DatasourceModifyResult> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new DatasourceModifyResult
        {
            Ok = false,
            Message = "Файловый источник доступен только для чтения",
            Kind = DatasourceKind.File,
        });

    TabularReadOptions SampleOptions() => new()
    {
        HasHeaders = _settings.HasHeaders,
        Delimiter = _settings.Delimiter,
        MaxRows = FileTypeInference.SampleRows,
    };

    TabularSheet Read(string? objectId, int maxRows, CancellationToken cancellationToken)
    {
        var (reference, sheetName) = SplitObjectId(ResolveObjectId(objectId));

        if (!_files.Exists(reference))
        {
            throw new FileNotFoundException($"Файл источника \"{reference}\" не найден", reference);
        }

        var reader = Reader(reference)
            ?? throw new NotSupportedException($"Формат файла \"{reference}\" не поддерживается: нужны csv или xlsx");

        using var stream = _files.OpenRead(reference);

        var sheets = reader.Read(stream, new TabularReadOptions
        {
            HasHeaders = _settings.HasHeaders,
            Delimiter = _settings.Delimiter,
            Sheet = sheetName,
            MaxRows = maxRows,
        });

        cancellationToken.ThrowIfCancellationRequested();

        return sheets.FirstOrDefault()
            ?? throw new InvalidOperationException(sheetName is null
                ? $"В файле \"{reference}\" нет данных"
                : $"Лист \"{sheetName}\" не найден в файле \"{reference}\"");
    }

    string ResolveObjectId(string? objectId)
    {
        if (!string.IsNullOrWhiteSpace(objectId)) return objectId.Trim();

        return _settings.Files.Count switch
        {
            1 => _settings.Files[0],
            0 => throw new InvalidOperationException($"В настройках источника \"{_config.Slug}\" не указано ни одного файла"),
            _ => throw new InvalidOperationException($"Укажите объект: в источнике \"{_config.Slug}\" несколько файлов"),
        };
    }

    /// <summary>
    /// Идентификатор объекта — ссылка на файл, у книги с несколькими листами — <c>ссылка#Лист</c>.
    /// Если после разделителя файла нет, '#' считается частью пути.
    /// </summary>
    (string Reference, string? Sheet) SplitObjectId(string objectId)
    {
        var separator = objectId.LastIndexOf(SheetSeparator);

        if (separator < 0) return (objectId, null);

        var reference = objectId[..separator];
        var sheet = objectId[(separator + 1)..];

        return !_files.Exists(reference) && _files.Exists(objectId)
            ? (objectId, null)
            : (reference, string.IsNullOrWhiteSpace(sheet) ? null : sheet);
    }

    ITabularFileReader? Reader(string reference)
        => _readers.FirstOrDefault(reader => reader.CanRead(reference));

    DatasourceCatalogObject ToObject(string reference, TabularSheet sheet, bool singleSheet)
    {
        var fileName = Path.GetFileName(reference.Replace('\\', '/'));
        var hasSheetName = !singleSheet && !string.IsNullOrEmpty(sheet.Name);
        var id = hasSheetName ? $"{reference}{SheetSeparator}{sheet.Name}" : reference;
        var fields = Fields(sheet).ToList();

        return new DatasourceCatalogObject
        {
            Id = id,
            Name = hasSheetName ? $"{fileName} · {sheet.Name}" : fileName,
            ObjectType = DatasourceObjectType.File,
            DefaultLanguage = DatasourceLanguage.Linq,
            Fields = fields.Select((column, index) => new DatasourceField
            {
                Name = column.Name,
                Ordinal = index + 1,
                DataTypeName = column.DataTypeName,
                ClrTypeName = column.ClrTypeName,
                IsNullable = true,
            }).ToList(),
        };
    }

    IEnumerable<DatasourceField> Fields(TabularSheet sheet)
    {
        var sample = sheet.Rows.Take(FileTypeInference.SampleRows).ToList();

        foreach (var column in sheet.Columns)
        {
            var typeName = FileTypeInference.Infer(sample.Select(row => row.TryGetValue(column, out var value) ? value : null));

            yield return new DatasourceField
            {
                Name = column,
                DataTypeName = typeName,
                ClrTypeName = FieldTypeMapping.ClrType(typeName).FullName ?? typeof(string).FullName!,
                IsNullable = true,
            };
        }
    }
}
