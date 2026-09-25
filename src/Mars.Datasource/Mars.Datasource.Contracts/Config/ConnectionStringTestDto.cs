namespace Mars.Datasource.Contracts.Config
{
    public class ConnectionStringTestDto
    {
        public string Kind { get; set; } = DatasourceKind.Sql;

        /// <summary>Вариант провайдера внутри типа источника; пустой, если вариант один (file, rest).</summary>
        public string Driver { get; set; } = "";

        /// <summary>Строка подключения sql-источника; у остальных типов источника пуста.</summary>
        public string ConnectionString { get; set; } = "";

        public Dictionary<string, string> Settings { get; set; } = [];
    }
}
