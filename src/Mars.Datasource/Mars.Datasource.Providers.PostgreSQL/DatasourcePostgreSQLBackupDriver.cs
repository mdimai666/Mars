using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Mars.Core.Extensions;
using Mars.Datasource.Abstractions.Exceptions;
using Mars.Datasource.Abstractions.Interfaces;
using Mars.Datasource.Abstractions.Mappings;
using Mars.Datasource.Abstractions.Sql;
using Mars.Datasource.Contracts.Sql;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;
using Npgsql;

namespace Mars.Datasource.Providers.PostgreSQL;

public class DatasourcePostgreSQLBackupDriver : IDatasourceBackupDriver
{
    public string Driver => "psql";

    public async Task Backup(string connectionString, BackupSettings settings, CancellationToken cancellationToken = default)
    {
        NpgsqlConnectionStringBuilder builder = new(connectionString);

        var db = new DatasourcePostgreSQLDriver(new DatasourceConfig { ConnectionString = connectionString, Driver = "psql" });
        var pgDumpPath = settings.DumpBinaryPath ?? await PgDumpBinPath(db);

        var args = ResolveBackupArgs(builder, settings);

        var env = new Dictionary<string, string>()
        {
            ["PGPASSWORD"] = builder.Password!
        };

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // pg_dump на Windows пишет сообщения в OEM-кодировке
        var enc = Encoding.GetEncoding("windows-1251");

        var cmd = new Process();
        cmd.StartInfo.FileName = pgDumpPath;
        cmd.StartInfo.Arguments = args.JoinStr(" ");
        if (isWindows)
        {
            cmd.StartInfo.StandardInputEncoding = enc;
            cmd.StartInfo.StandardOutputEncoding = enc;
            cmd.StartInfo.StandardErrorEncoding = enc;
        }

        foreach (var arg in env)
        {
            cmd.StartInfo.Environment.Add(arg!);
        }

        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.RedirectStandardError = true;
        cmd.StartInfo.CreateNoWindow = false;
        cmd.StartInfo.UseShellExecute = false;

        cmd.Start();

        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        await cmd.WaitForExitAsync(cancellationToken);

        var error = cmd.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(error))
        {
            throw new DatasourceOperationException(error);
        }

        _ = cmd.StandardOutput.ReadToEnd();
    }

    /// <summary>
    /// Путь pg_dump из настроек сервера: <c>C:\Program Files\PostgreSQL\14\bin\pg_dump.exe</c>
    /// или <c>/usr/lib/postgresql/15/bin/pg_dump</c>.
    /// </summary>
    public async Task<string?> PgDumpBinPath(DatasourcePostgreSQLDriver db)
    {
        var binPath = await ScalarAsync(db, "SELECT setting FROM pg_config WHERE name = 'BINDIR';");
        var osName = await ScalarAsync(db, @"SELECT setting
FROM   pg_catalog.pg_file_settings
WHERE  name = 'dynamic_shared_memory_type'");

        var isWindows = string.Equals(osName, "windows", StringComparison.OrdinalIgnoreCase);
        var pgDumpPath = binPath + "/pg_dump" + (isWindows ? ".exe" : "");

        if (File.Exists(pgDumpPath)) return pgDumpPath;

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? FindWindowsPgDumpPath() : null;
    }

    static async Task<string?> ScalarAsync(DatasourcePostgreSQLDriver db, string sql)
    {
        var result = await db.Query(new DatasourceRequest { Query = sql, MaxRows = 1 });

        if (!result.Ok) throw new DatasourceOperationException(result.Message);

        return result.Rows.FirstOrDefault()?.FirstOrDefault();
    }

    public string? FindWindowsPgDumpPath()
    {
        var start = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
        var versionDirs = Directory.GetDirectories(start)
                                    .Select(s => Path.GetFileName(s))
                                    .Select(s => int.TryParse(s, out var i) ? i : 0)
                                    .Where(s => s > 9)
                                    .OrderDescending()
                                    .ToList();

        foreach (var ver in versionDirs)
        {
            var bin = Path.Join(start, ver.ToString(), "bin", "pg_dump.exe");
            if (File.Exists(bin)) return bin;
        }
        return null;
    }

    string[] ResolveBackupArgs(NpgsqlConnectionStringBuilder builder, BackupSettings settings)
    {
        var b = builder;

        var sqlFileName = settings.FilePath;

        string[] args = [
            @$"--file ""{sqlFileName}""",
            @$"--host ""{b.Host}""",
            @$"--port ""{b.Port}""",
            @$"--username ""{b.Username}""",
            @$"--no-password",
            //@$"--format=p",
            //@$"--format=t",
            @$"--encoding ""UTF8""",
            //@$"--section=pre-data",
            //@$"--section=data",
            //@$"--section=post-data",
            @$"--no-owner",
            @$"--no-privileges",
            //@$"--data-only",
            //@$"--verbose",
            //@$"--compress",
            
        ];

        if (settings.Mode == BackupOutputMode.PlainSql)
        {
            args = [..args,
                "--format=p",
                "--inserts",
            ];
        }
        else if (settings.Mode == BackupOutputMode.Compressed)
        {
            args = [..args,
                "--format=t",
                "--compress"
            ];
        }
        else
        {
            throw new NotImplementedException($"BackupOutputMode '{settings.Mode}' not implement");
        }

        if (settings.DumpMode == DumpMode.SchemaAndData)
        {
            args = [..args,
                @$"--section=pre-data",
                @$"--section=data",
                @$"--section=post-data",
            ];
        }
        else if (settings.DumpMode == DumpMode.Schema)
        {
            args = [..args,
                @$"--schema-only",
            ];
        }
        else if (settings.DumpMode == DumpMode.DataOnly)
        {
            args = [..args,
                @$"--data-only",
            ];
        }
        else
        {
            throw new NotImplementedException($"DumpMode '{settings.DumpMode}' not implement");
        }

        args = [.. args, $"{b.Database}"];

        return args;
    }

    public Task Restore(string connectionString, RestoreSettings settings, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

}
