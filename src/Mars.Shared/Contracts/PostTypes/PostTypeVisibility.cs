namespace Mars.Shared.Contracts.PostTypes;

/// <summary>
/// Видимость типа поста.
/// <see cref="Public"/> — обычный публичный тип;
/// <see cref="Component"/> — встроенный тип-компонент (строки списков, галереи и т.п.):
/// в админке виден под флагом, в публичные меню не попадает.
/// </summary>
public enum PostTypeVisibility
{
    Public = 0,
    Component = 1,
}
