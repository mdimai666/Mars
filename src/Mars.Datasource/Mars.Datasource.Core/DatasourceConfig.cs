using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Datasource.Core;

public class DatasourceConfig
{

    [Display(Name = "Название")]
    public string Title { get; set; } = "";

    [Display(Name = "Slug")]
    [Required]
    public string Slug { get; set; } = "";

    [Display(Name = "ConnectionString")]
    [Required]
    public string ConnectionString { get; set; } = "";

    [Display(Name = "Driver")]
    public string Driver { get; set; } = "psql";

    [Display(Name = "Disabled")]
    public bool Disabled { get; set; }

    public static List<string> DriverList = new() { "psql", "mssql", "mysql" };

    //[Display(Name = "Database")]
    //public string Database { get; set; } = "";

    public string Label => string.IsNullOrEmpty(Title) ? Slug : Title;



    Dictionary<string, string> ConnStringParts() => ConnectionString.Split(';')
            .Select(t => t.Split(new char[] { '=' }, 2))
            .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);


    public string GetDatabaseName() => ConnStringParts()["database"];

    public string GetDefaultConnectionString()
    {
        return Driver switch
        {
            "psql" => "Host=127.0.0.1;Database=database;Username=postgres;Password=123456;Port=5432",
            "mssql" => "\"Server=.\\\\SQLEXPRESS;Database=database;User ID=sa;Password=123456;Trusted_Connection=True;TrustServerCertificate=True",
            "mysql" => "server=127.0.0.1;database=database;uid=user;pwd=123456;port=3306;Connection Timeout=2;persistsecurityinfo=True;SslMode=none",
            _ => ""
        };
    }

    public bool IsDefaultString()
    {
        return GetDatabaseName() == "database";
    }

    public string HelpLinkConnectionString => Driver switch
    {
        "psql" => "https://www.npgsql.org/doc/basic-usage.html",
        "mssql" => "https://learn.microsoft.com/en-us/ef/core/",
        "mysql" => "https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html",
        _ => ""
    };
}
