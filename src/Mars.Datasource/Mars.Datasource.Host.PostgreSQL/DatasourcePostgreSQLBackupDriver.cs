using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Mars.Datasource.Core;
using Mars.Datasource.Core.Interfaces;
using Mars.Datasource.Host.Core.Exceptions;
using Mars.Datasource.Host.Core.Models;
using Mars.Core.Extensions;
using Npgsql;

namespace Mars.Datasource.Host.PostgreSQL;

public class DatasourcePostgreSQLBackupDriver : IDatasourceBackupDriver
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="DatasourceOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Backup(string connectionString, BackupSettings settings, CancellationToken cancellationToken = default)
    {
        NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(connectionString);

        var db = new DatasourcePostgreSQLDriver(new DatasourceConfig { ConnectionString = connectionString, Driver = "psql" });
        var pgDumpPath = settings.DumpBinaryPath ?? await PgDumpBinPath(db);

        var args = ResolveBackupArgs(builder, settings);

        var env = new Dictionary<string, string>()
        {
            ["PGPASSWORD"] = builder.Password!
        };

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        //var enc = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
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

        //cmd.StandardInput.WriteLine(args.JoinStr(" "));

        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        await cmd.WaitForExitAsync(cancellationToken);
        //cmd.WaitForExit(5000);
        //cmd.WaitForInputIdle();

        var error = cmd.StandardError.ReadToEnd();
        //error = Decode866(error);

        if (!string.IsNullOrEmpty(error))
        {
            throw new DatasourceOperationException(error);
        }

        var result = cmd.StandardOutput.ReadToEnd();

        //return UserActionResult<string[][]>.Success([[result]]);
    }

    /// <summary>
    /// "C:\Program Files\PostgreSQL\14\bin\pg_dump.exe"
    /// <para/>
    /// "/usr/lib/postgresql/15/bin/pg_dump"
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public async Task<string?> PgDumpBinPath(DatasourcePostgreSQLDriver db)
    {
        var sqlBinQuery = "SELECT setting FROM pg_config WHERE name = 'BINDIR';";
        var sqlOSQuery = @"SELECT setting 
FROM   pg_catalog.pg_file_settings
WHERE  name = 'dynamic_shared_memory_type'";
        var binPath = (await db.SqlQueryJson(sqlBinQuery)).Data[0]["pg_config.setting"].GetValue<string>();

        var OSName = (await db.SqlQueryJson(sqlOSQuery)).Data[0]["pg_file_settings.setting"].GetValue<string>();
        var isWindows = OSName == "windows";

        //var pg_dump__BinName = "pg_dump" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");
        var pg_dump__BinName = "pg_dump" + (isWindows ? ".exe" : "");

        var pgDumpPath = binPath + "/" + pg_dump__BinName;

        if (File.Exists(pgDumpPath)) return pgDumpPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var winPgDump = FindWindowsPgDumpPath();
            if (winPgDump != null) return winPgDump;
        }

        return null;
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
