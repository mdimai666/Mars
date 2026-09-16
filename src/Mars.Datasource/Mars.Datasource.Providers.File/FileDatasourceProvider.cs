using System.Diagnostics;
using System.Linq.Dynamic.Core;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Models;
using Mars.Datasource.Contracts.Models;

namespace Mars.Datasource.Providers.File;

/// <summary>
/// Файл источника как таблица: CSV и листы XLSX из <c>data/datasource/&lt;slug&gt;/files/</c>.
/// Запрос — предикат Dynamic LINQ (<c>val.num(age) &gt; 30</c>), пустой запрос отдаёт все строки.
/// </summary>
public class FileDatasourceProvider : IDatasourceProvider
{
    /// <summary>Файл читается целиком в память, поэтому у выборки есть жёсткий предел.</summary>
    public const int MaxSourceRows = 100_000;

    readonly DatasourceConfig _config;
    readonly IDatasourceStore _store;
    readonly IReadOnlyList<ITabularFileReader> _readers;
    readonly FileSourceSettings _settings;

    public FileDatasourceProvider(DatasourceConfig config, IDatasourceStore store)
        : this(config, store, [new CsvTabularFileReader(), new XlsxTabularFileReader()])
    {
    }

    public FileDatasourceProvider(DatasourceConfig config, IDatasourceStore store, IReadOnlyList<ITabularFileReader> readers)
    {
        _config = config;
        _store = store;
        _readers = readers;
        _settings = FileSourceSettings.From(config);
    }

    public DatasourceCapabilities Capabilities { get; } = new()
    {
        CanQuery = true,
        CanBrowse = true,
    };

    string ProviderName => DatasourceKind.File;

    public Task<DatasourceCatalog> Catalog(CancellationToken cancellationToken = default)
    {
        DatasourceCatalogGroup group = new() { Name = "" };

        foreach (var fileName in _store.ListDataFiles(_config.Slug))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Reader(fileName) is not { } reader) continue;

            using var stream = _store.OpenDataFile(_config.Slug, fileName);

            var sheets = reader.Read(stream, SampleOptions());

            for (var index = 0; index < sheets.Count; index++)
            {
                group.Objects.Add(ToObject(fileName, sheets[index], sheets.Count == 1));
            }
        }

        DatasourceCatalog catalog = new()
        {
            Kind = string.IsNullOrWhiteSpace(_config.Kind) ? DatasourceKind.File : _config.Kind,
            SourceName = _config.Label,
            Capabilities = Capabilities,
            Groups = group.Objects.Count > 0 ? [group] : [],
        };

        return Task.FromResult(catalog);
    }

    public Task<QueryResultDto> Query(DatasourceRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        QueryResultDto result = new()
        {
            DatabaseDriver = ProviderName,
            Command = request.Query,
        };

        try
        {
            var sheet = Read(request.ObjectId, MaxSourceRows, cancellationToken);

            result.Columns = Columns(sheet).ToArray();

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

    public Task<SqlNonQueryResultActionDto> Modify(DatasourceRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SqlNonQueryResultActionDto
        {
            Ok = false,
            Message = "Файловый источник доступен только для чтения",
            DatabaseDriver = ProviderName,
        });

    TabularReadOptions SampleOptions() => new()
    {
        HasHeaders = _settings.HasHeaders,
        Delimiter = _settings.Delimiter,
        MaxRows = FileTypeInference.SampleRows,
    };

    TabularSheet Read(string? objectId, int maxRows, CancellationToken cancellationToken)
    {
        var (fileName, sheetName) = SplitObjectId(ResolveObjectId(objectId));

        if (!_store.DataFileExists(_config.Slug, fileName))
        {
            throw new FileNotFoundException($"Файл источника \"{fileName}\" не найден", fileName);
        }

        var reader = Reader(fileName)
            ?? throw new NotSupportedException($"Формат файла \"{fileName}\" не поддерживается: нужны csv или xlsx");

        using var stream = _store.OpenDataFile(_config.Slug, fileName);

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
                ? $"В файле \"{fileName}\" нет данных"
                : $"Лист \"{sheetName}\" не найден в файле \"{fileName}\"");
    }

    string ResolveObjectId(string? objectId)
    {
        if (!string.IsNullOrWhiteSpace(objectId)) return objectId.Trim();

        if (!string.IsNullOrWhiteSpace(_settings.File)) return _settings.File;

        var files = _store.ListDataFiles(_config.Slug).ToList();

        return files.Count switch
        {
            1 => files[0],
            0 => throw new InvalidOperationException($"В источнике \"{_config.Slug}\" нет файлов данных"),
            _ => throw new InvalidOperationException($"Укажите объект: в источнике \"{_config.Slug}\" несколько файлов"),
        };
    }

    /// <summary>Идентификатор объекта — имя файла, у книги с несколькими листами — <c>книга.xlsx:Лист</c>.</summary>
    static (string FileName, string? Sheet) SplitObjectId(string objectId)
    {
        var separator = objectId.IndexOf(':');

        return separator < 0
            ? (objectId, null)
            : (objectId[..separator], objectId[(separator + 1)..]);
    }

    ITabularFileReader? Reader(string fileName)
        => _readers.FirstOrDefault(reader => reader.CanRead(fileName));

    DatasourceCatalogObject ToObject(string fileName, TabularSheet sheet, bool singleSheet)
    {
        var id = singleSheet || string.IsNullOrEmpty(sheet.Name) ? fileName : $"{fileName}:{sheet.Name}";
        var columns = Columns(sheet).ToList();

        return new DatasourceCatalogObject
        {
            Id = id,
            Name = id,
            ObjectType = DatasourceObjectType.File,
            DefaultLanguage = DatasourceLanguage.Linq,
            Columns = columns.Select((column, index) => new DatasourceCatalogColumn
            {
                Name = column.Name,
                Ordinal = index + 1,
                DataTypeName = column.DataTypeName,
                ClrTypeName = column.ClrTypeName,
                IsNullable = true,
            }).ToList(),
        };
    }

    IEnumerable<QueryColumn> Columns(TabularSheet sheet)
    {
        var sample = sheet.Rows.Take(FileTypeInference.SampleRows).ToList();

        foreach (var column in sheet.Columns)
        {
            var typeName = FileTypeInference.Infer(sample.Select(row => row.TryGetValue(column, out var value) ? value : null));

            yield return new QueryColumn
            {
                Name = column,
                DataTypeName = typeName,
                ClrTypeName = QColumnMapping.ClrType(typeName).FullName ?? typeof(string).FullName!,
                IsNullable = true,
            };
        }
    }
}
