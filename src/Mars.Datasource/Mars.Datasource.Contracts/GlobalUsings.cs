// Домены контрактов ссылаются друг на друга (конфиг → kind языка запроса, каталог → профиль,
// документ → параметры операций): внутри библиотеки видим их без явных using в каждом файле.
global using Mars.Datasource.Contracts.Catalog;
global using Mars.Datasource.Contracts.Config;
global using Mars.Datasource.Contracts.Document;
global using Mars.Datasource.Contracts.Query;
