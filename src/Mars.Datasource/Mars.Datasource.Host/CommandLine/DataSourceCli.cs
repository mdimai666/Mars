﻿using System.CommandLine;
using Mars.Datasource.Host.Core.Models;
using Mars.Datasource.Host.Services;
using Mars.Host.Shared.CommandLine;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Datasource.Host.CommandLine;

public class DataSourceCli : CommandCli
{
    public DataSourceCli(ICommandLineApi cli) : base(cli)
    {
        var datasourceCommand = new Command("ds", "datasource subcommand");

        //options
        var optionFile = new Option<FileInfo>(["--file", "-f"], "file path (.sql,.tar,.tar.gz)");
        var optionFileRequired = new Option<FileInfo>(["--file", "-f"], "file path (.sql,.tar,.tar.gz)") { IsRequired = true };
        var optionDir = new Option<DirectoryInfo>(["--dir", "-d"], "directory path. File will have template '{database}_{yyyy-MM-dd}.sql'");

        //backup
        var dsBackup = new Command("backup", "backup database") { optionFile, optionDir };
        dsBackup.SetHandler(BackupCommand, optionFile, optionDir);
        datasourceCommand.AddCommand(dsBackup);

        //restore
        var dsRestore = new Command("restore", "restore database") { optionFileRequired };
        dsRestore.SetHandler(RestoreCommand, optionFileRequired);
        datasourceCommand.AddCommand(dsRestore);

        cli.AddCommand(datasourceCommand);
    }

    async Task BackupCommand(FileInfo? file, DirectoryInfo? dir)
    {
        if (file is null && dir is null)
            throw new ArgumentException("require is --file (-f) or --dir (-d) ");
        if (dir is not null && file is not null)
            throw new ArgumentException("most one of --file (-f) or --dir (-d) ");

        using var scope = app.Services.CreateScope();
        var ds = scope.ServiceProvider.GetRequiredService<IDatasourceService>();
        var bs = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

        string dateTimeFormat = "yyyy-MM-dd";

        string templateFilename = string.Format("{0}_{1}.sql", ds.DefaultConfig.GetDatabaseName(), DateTime.Now.ToString(dateTimeFormat));
        string filePath = file?.FullName ?? Path.Join(dir.FullName, templateFilename);

        var ext = file?.Extension ?? Path.GetExtension(filePath);

        var settings = new BackupSettings()
        {
            DumpMode = DumpMode.SchemaAndData,
            FilePath = filePath,
            Mode = ext switch
            {
                ".gz" => BackupOutputMode.Compressed,
                _ => BackupOutputMode.PlainSql
            }
        };

        try
        {
            await bs.Backup(ds.DefaultConfig, settings);
            Console.WriteLine("file = " + filePath);
            OutResult(UserActionResult.Success());
        }
        catch (Exception ex)
        {
            OutResult(UserActionResult.Exception(ex));
        }
    }

    async Task RestoreCommand(FileInfo file)
    {
        using var scope = app.Services.CreateScope();
        var ds = scope.ServiceProvider.GetRequiredService<IDatasourceService>();
        var bs = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

        string filePath = file.FullName;
        var ext = file.Extension;

        var settings = new RestoreSettings()
        {
            DumpMode = DumpMode.SchemaAndData,
            FilePath = filePath,
            Mode = ext switch
            {
                ".gz" => BackupOutputMode.Compressed,
                _ => BackupOutputMode.PlainSql
            }
        };

        try
        {
            await bs.Restore(ds.DefaultConfig, settings);
            Console.WriteLine("file = " + filePath);
            OutResult(UserActionResult.Success());
        }
        catch (Exception ex)
        {
            OutResult(UserActionResult.Exception(ex));
        }
    }
}
