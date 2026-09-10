# ModelSchemaGenerator

Этот handler автоматически определяет:

- Типы данных (string, int, decimal, bool, DateTime, enum)
- Обязательные поля (через атрибут [Required] или для record параметров)
- Вложенные объекты и коллекции
- Ограничения (MaxLength, Range, RegularExpression)
- Форматы (email, uuid, date-time)
- Примеры значений

# Пример 1: Простой record
```csharp
public record User(
    [property: Required, Description("User's full name")] string Name,
    [property: Range(18, 120), Example(25)] int Age,
    [property: EmailAddress] string Email
);
```

# Пример 2: Сложный класс
```csharp
[Description("Product information model")]
public class Product
{
    [Required]
    [JsonPropertyName("productId")]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Description("Product display name")]
    public string Name { get; set; }
    
    [Range(0, 9999.99)]
    public decimal Price { get; set; }
    
    public List<string> Tags { get; set; }
    
    public Category Category { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

# Использование

```csharp
var generator = new ModelSchemaGenerator();

// Генерация схемы
string userSchema = generator.GenerateSchema<User>();
string productSchema = generator.GenerateSchema<Product>();

Console.WriteLine(userSchema);
```

# Результат для User record:
```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "User's full name",
      "example": "string value"
    },
    "age": {
      "type": "integer",
      "minimum": 18,
      "maximum": 120,
      "example": 25
    },
    "email": {
      "type": "string",
      "format": "email",
      "example": "string value"
    }
  },
  "required": ["name", "age", "email"],
  "additionalProperties": false
}
```
