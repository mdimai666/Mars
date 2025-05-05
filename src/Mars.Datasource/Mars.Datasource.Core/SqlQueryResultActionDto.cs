using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Mars.Core.Models;

namespace Mars.Datasource.Core;

public class SqlQueryResultActionDto : IUserActionResult<string[][]?>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = default!;
    
    public string DatabaseDriver { get; set; } = default!;
    public string[][]? Data { get; set; }
}

public class SqlQueryJsonResultActionDto : IUserActionResult<JsonArray?>
{
    public bool Ok { get; set; }
    public string Message { get; set; } = default!;
    public string DatabaseDriver { get; set; } = default!;
    public JsonArray? Data { get; set; }
    public string[]? Fields { get; set; }
}