namespace Mars.Datasource.Contracts.Models
{
    public class ConnectionStringTestDto
    {
        public string Kind { get; set; } = DatasourceKind.Sql;
        public string Driver { get; set; } = default!;
        public string ConnectionString { get; set; } = default!;
        public Dictionary<string, string> Settings { get; set; } = [];
    }
}
