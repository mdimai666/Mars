# Mars Package Reference Guide for Agent

Как работать с `PackageReference` в Mars: что можно удалять, что выглядит лишним, но не является,
и как проверять неиспользуемые ссылки. Гайд написан по итогам аудита пакетов 2026-08-30
(после реструктуризации в части проектов оказались приписаны чужие пакеты — например,
`Microsoft.EntityFrameworkCore` в `Mars.Nodes.Core.Implements`).

## Базовые правила

- Центральное управление версиями: версии пакетов — только в корневом `Directory.Packages.props`,
  в csproj `PackageReference` указывается **без `Version`**.
- Важное свойство: центральная версия в props **не пинит транзитивные** зависимости. Пин работает,
  только если пакет указан прямой ссылкой в csproj потребителя. Поэтому «лишние» на первый взгляд
  прямые ссылки часто являются осознанными пинами транзитивных версий.
- `System.Text.Encoding.CodePages` с .NET 10 входит в shared framework — NuGet сам просит убрать
  ссылку (предупреждение NU1510, Package Pruning). Не добавлять этот пакет в проекты.

## Пины транзитивных версий — НЕ удалять

В коде не используются, но гасят уязвимые/конфликтные транзитивные версии. Помечены комментариями
в csproj; при чистках не трогать (или переносить вместе с комментарием):

| Проект | Пакет | Зачем |
|---|---|---|
| `Mars.WebApp` | `Microsoft.CodeAnalysis.Common/CSharp/CSharp.Workspaces/Workspaces.Common/Workspaces.MSBuild` | без них `Microsoft.EntityFrameworkCore.Design` даёт конфликт Roslyn 5.0.0/5.3.0 (NU1107) |
| `Mars.WebApp.Nodes.Host` | `System.Drawing.Common` | без него `dotless.Core` транзитивно тянет уязвимый 4.5.0 (NU1904) |
| `Mars.Excel.Host` | `System.IO.Packaging`, `System.Linq.Dynamic.Core` | пины против уязвимых версий, тянущихся через ClosedXML/ClosedXML.Report (NU1903) |

Если при удалении «неиспользуемого» пакета в выводе сборки появляются новые `NU19xx`/`NU1107` —
это он и был пином; вернуть обратно с комментарием.

## Неочевидные использования — тоже НЕ удалять

Пакет нужен, хотя прямого `using <имя пакета>` в коде нет:

| Проект | Пакет | Где используется |
|---|---|---|
| `Mars.Nodes.Core.Implements` | `Microsoft.EntityFrameworkCore` | `FunctionNodeImpl` отдаёт `typeof(EntityFrameworkQueryableExtensions).Assembly` в ссылки пользовательских скриптов ноды Function — EF-расширения доступны в скриптах |
| `Mars.Nodes.Core` | `Microsoft.AspNetCore.Components.DataAnnotations.Validation` | атрибут `[ValidateComplexType]` в `VariableSetNode` |
| `Mars.Nodes.Host` | `Microsoft.AspNet.WebApi.Client` | `HttpClientFactory` в `NodeRuntime` |
| `Mars.Test.Common` | `AutoFixture.SeedExtensions` | extension-методы `builder.Create(...)` в `FixtureCustomizes/*` — имя пакета в коде не упоминается |

## Инфраструктурные ссылки — не трогать при аудите

- `Microsoft.SourceLink.GitHub` (пакуемые проекты), `Microsoft.NET.Test.Sdk`/`xunit*`/`coverlet.collector` (тесты).
- `PhotoSauce.NativeCodecs.*` в `Mars.Media.Host` — рантайм-компаньоны `MagicScaler` (форматы изображений), в коде не видны.
- `*.Design`-пакеты EF (`Npgsql.EntityFrameworkCore.PostgreSQL.Design`, `Microsoft.EntityFrameworkCore.Design`) — тулинг миграций.
- `PluginExample` — образец плагина: парные `ItemGroup` для Debug/Release (`Private=false`, `ExcludeAssets=runtime`) — осознанная конструкция, не дубли.

## Как проверять, лишняя ли ссылка

1. Поиск по исходникам проекта: `using <корневой неймспейс пакета>`, характерные типы,
   вызовы-расширения (`AddXxx`/`UseXxx`), теги компонентов в `.razor`.
2. Учитывать исключения из разделов выше (пины, расширения, скриптовые ссылки).
3. **Обязательна полная сборка `dotnet build Mars.slnx`** — только она ловит неявные использования
   (примеры из аудита: `ValidateComplexType`, `HttpClientFactory`, `Create(...)` из SeedExtensions)
   и транзитивные разрывы. Удаление без сборки даёт ложноположительные результаты.
4. Если пакет реально не нужен в проекте, но нужен его тестам/потребителям транзитивно —
   предпочесть прямую ссылку в потребителе, а не транзитивное протекание через общий проект
   (пример аудита: `NSubstitute` добавлен напрямую в `Mars.Nodes.Tests` и `Mars.AiServices.Integration.Tests`).

## Итог аудита 2026-08-30

Убраны лишние ссылки в ~15 проектах (самые крупные: `Duende.IdentityServer`,
`ApiAuthorization.IdentityServer`, `JsonPatch`, `Humanizer`, `Cryptography.Pkcs` из `Mars.WebApp`;
`Extensions.AI*`/`Caching.Memory` из `Mars.SemanticKernel.Abstractions`; identity-пакеты из SSO-контрактов),
вычищены мёртвые записи из `Directory.Packages.props`, драйверы `Mars.Datasource.Host.MsSQL/MySQL`
переведены с EF-провайдеров на прямые ADO-клиенты (`Microsoft.Data.SqlClient`, `MySqlConnector`),
`EFCore.NamingConventions` переехал из `Mars.Data` в `Mars.Data.PostgreSQL` к единственному потребителю.
Сборка зелёная.
