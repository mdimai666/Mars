# Expressions

## Расширенные выражения добавляемые в плагине Марса

Для просмотра подобной информации в процессе создания шаблона можно воспользоваться командой `{{#help}}`, которая выведет список доступных хелперов и их описание.

Исходный код можно посмотреть [здесь](https://github.com/mdimai666/Mars/blob/master/src/Mars.Modules/Mars.WebSiteProcessor.Handlebars/HandlebarsFunc/MyHandlebars.cs)

| Helper Name         | Type   | Example                                                   | Description |
|---------------------|--------|------------------------------------------------------------|-------------|
| date                | Inline | `{{#date @DateTime}}`                                     | Formats a DateTime or DateTimeOffset to a short date string. |
| dateFormat          | Inline | `{{#dateFormat @DateTime "yyyy-MM-dd HH:mm"}}`            | Formats a DateTime or DateTimeOffset according to the specified format string. |
| encode              | Inline | `{{#encode @text}}`                                       | Encodes the text for HTML output, escaping special characters. |
| help                | Inline | `{{#help}}`                                               | Displays information about available helpers. |
| L                   | Inline | `{{#L @string_key values[]?}}`                            | Localization helper. Returns localized string by key. If values are provided, they will be used for formatting the string. |
| lookup              | Inline | *(example missing)*                                       | *(description missing)* |
| nl2br               | Inline | `{{#nl2br @text}}`                                        | Converts newlines in the text to `<br>` tags. |
| parsedateandformat  | Inline | `{{#parsedateandformat @string @parseformat @outformat}}` | Parse string to date and formats it to a short date string. |
| raw_block           | Inline | `{{#raw_block @block_name}}`                              | Выводит содержимое блока с именем `block_name`. Блоки регистрируются в `WebSiteTemplate.Parts`. |
| striphtml           | Inline | `{{#striphtml @html}}`                                    | Strips HTML tags from the text. |
| text_ellipsis       | Inline | `{{#text_ellipsis @string @count?}}`                      | Truncates the text to the specified character count, adding an ellipsis if necessary. |
| text_excerpt        | Inline | `{{#text_excerpt @string @count?}}`                       | Generates an excerpt from the text, stripping HTML and truncating to the specified character count. |
| ToHumanizedSize     | Inline | `{{#ToHumanizedSize @size}}`                              | Converts a size in bytes to a human-readable format. |
| tojson              | Inline | `{{#tojson @object}}`                                     | Serializes an object to JSON format. |
| youtubeId           | Inline | `{{#youtubeId @url}}`                                     | Extracts the YouTube video ID from a YouTube URL. |
| !mobile             | Block  | `{{#!mobile}}...{{/!mobile}}`                             | Печатает содержимое в блоке если запрос **НЕ** с мобильного устройства. |
| and                 | Block  | `{{#and @args[]}}`                                        | Conditionally renders if all arguments are truthy. |
| Contains            | Block  | `{{#contains @source @item}}`                             | Conditionally renders if the source contains the item. |
| context             | Block  | `{{#context @key? @cache?="10m"}}...{{/context}}`         | Контекстный блок. Позволяет использовать контекстные данные. При указании ключа данные кэшируются на заданное время. |
| eq                  | Block  | `{{#eq @left @right}}`                                    | Conditionally renders if left == right. |
| eqstr               | Block  | `{{#eqstr @left @right}}`                                 | Conditionally renders if left == right (as strings). |
| for                 | Block  | `{{#for @start @end @step?}}`                             | Iterates from start to end. |
| gt                  | Block  | `{{#gt @left @right}}`                                    | Conditionally renders if left > right. |
| gte                 | Block  | `{{#gte @left @right}}`                                   | Conditionally renders if left >= right. |
| if_divided_by       | Block  | *(example missing)*                                       | *(description missing)* |
| iff                 | Block  | `{{#iff 'x>y'}}...{{/iff}}`                               | Условный блок. Выполняет содержимое, если условие истинно. Условие должно быть в кавычках. |
| IsEmpty             | Block  | `{{#isEmpty @value}}`                                     | Conditionally renders if the value is empty (null, empty string, empty collection). |
| lt                  | Block  | `{{#lt @left @right}}`                                    | Conditionally renders if left < right. |
| lte                 | Block  | `{{#lte @left @right}}`                                   | Conditionally renders if left <= right. |
| mobile              | Block  | `{{#mobile}}...{{/mobile}}`                               | Печатает содержимое в блоке если запрос с мобильного устройства. |
| neq                 | Block  | `{{#neq @left @right}}`                                   | Conditionally renders if left != right. |
| neqstr              | Block  | `{{#neqstr @left @right}}`                                | Conditionally renders if left != right (as strings). |
| or                  | Block  | `{{#or @args[]}}`                                         | Conditionally renders if any argument is truthy. |
| with                | Block  | *(example missing)*                                       | *(description missing)* |

## Supported Query ef.<type>.<Method> methods
| Helper Name         | Example                          | Description |
|---------------------|----------------------------------|-------------|
| Count               | `.Count(@expr?)`                | Возвращает количество элементов в запросе. Если `@expr` не указано, то возвращает общее количество элементов в запросе. |
| First               | `.First(@expr?)`                | Возвращает первый элемент. Если `@expr` указано, то применяет выборку. |
| Include             | `.Include(@expr)`               | Включает связанные данные в запрос. `@expr` — имя навигационного свойства или список свойств через запятую. |
| Last                | `.Last(@expr?)`                 | Возвращает последний элемент. Если `@expr` указано, то применяет выборку. |
| OrderBy             | `.OrderBy(@fieldName)`          | Сортирует элементы по указанному полю. `@fieldName` — имя поля для сортировки. |
| OrderByDescending   | `.OrderByDescending(@fieldName)`| Сортирует элементы по указанному полю в порядке убывания. `@fieldName` — имя поля для сортировки. |
| Search              | `.Search(@searchText)`          | Поиск по тексту. `@searchText` — текст для поиска. |
| Select              | `.Select(@expr)`                | Выбирает элементы из запроса по указанному выражению. `@expr` — выражение для выбора элементов. |
| Skip                | `.Skip(@expr)`                  | Пропускает указанное количество элементов. `@expr` — выражение для вычисления количества элементов для пропуска. |
| Table               | `.Table(@page, @size)`          | Пагинация. Возвращает элементы в виде таблицы с пагинацией. `@page` — номер страницы, `@size` — количество элементов на странице. |
| Take                | `.Take(@expr)`                  | Ограничивает количество элементов в запросе. `@expr` — выражение для вычисления количества элементов. |
| ThenBy              | `.ThenBy(@fieldName)`           | Продолжает сортировку элементов по указанному полю. `@fieldName` — имя поля для сортировки. |
| ThenByDescending    | `.ThenByDescending(@fieldName)` | Продолжает сортировку элементов по указанному полю в порядке убывания. `@fieldName` — имя поля для сортировки. |
| ToList              | `.ToList()`                     | Преобразует запрос в список. |
| Union               | `.Union(@secondQueryable)`      | Объединяет текущий запрос с другим. `@secondQueryable` — второй запрос для объединения. |
| Where               | `.Where(@expr)`                 | Фильтрует элементы по указанному выражению. `@expr` — выражение для фильтрации. |

## Context Block

Этот блок позволяет запрашивать данные

Sample
```hbs
{{#context}}
posts = ef.myType.Where(post.pinned==true).Take(3)
x == 1 + 1
sum == x*2
request = Req("GET", "http://localhost:5003/api/posts")
{{/context}}
```

Доступные команды
1. x == 2 - два знака '==' вызов c# выражения или присваивание константы
2. x = @Function - вызов зарегестрированной функции

## Registered #context functions

Встроенные функции [Исходник](https://github.com/mdimai666/Mars/blob/master/src/Mars.Host/Templators/TemplatorRegisterFunctions.cs)

| Helper Name  | Example                                                  | Description |
|--------------|----------------------------------------------------------|-------------|
| Paginator    | `x = Paginator(@page?, total, pageSize)`                | Создает объект пагинатора для управления постраничным выводом данных. Если `@page?` не передан, то пытается прочитать `_req.Query["page"] ?? 1`. |
| Req          | `x = Req("GET", "https://example.com/api/data", @postData?)` | Выполняет HTTP-запрос. |
| Node         | `x = Node(callNodeName, payload? = null)`               | Вызов ноды CallNode с именем `nodeName` и передача ей необязательного параметра `payload`. Возвращает результат выполнения ноды. |
