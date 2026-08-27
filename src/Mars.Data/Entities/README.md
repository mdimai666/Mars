# Entities

# json mapping
[Column(TypeName = "jsonb")] утарела
используйте .ToJson(); see: https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cjsondocument#tojson-owned-entity-mapping

Разница в .ToJson() можно ходить по содержимому при ef.Where, а в [Column(TypeName = "jsonb")] нельзя
