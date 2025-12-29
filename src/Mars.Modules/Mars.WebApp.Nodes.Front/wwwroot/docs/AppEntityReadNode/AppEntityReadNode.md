# AppEntityReadNode

AppEntityReadNode is a node for reading data from application entities using LINQ queries.

The node allows you to:
- select an application entity (Post, User, etc.),
- construct a query using a visual LINQ builder or string expression,
- configure method chains (Where, Select, OrderBy, Include, etc.),
- obtain typed data ready for use in subsequent nodes.

## Expression examples

| Name	|   Description |
| --- | --- |
| all entries | Posts.ToList()
| records with the 'post' subtype | post.Skip(1).Take(10+1)
| posts with Title 111 | post.Where(post.Title!="111") |
| first entry | page.First()
| all entries with 'draft' status  | post.Where(post.Status == "draft")
| the entry with the longest title |    post.OrderByDescending(x=>x.Title.Length)
| the post contain tag | post.Where(post.Tags.Contains("tag1")) |

### Methods
| Name	|   Description
| --- | --- |
| .Count(@expr?) |	Возвращает количество элементов в запросе. Если @expr не указано, то возвращает общее количество элементов в запросе
| .First(@expr?) |	Возвращает первый элемент. Если @expr указано, то возвращает применяет выборку.
| .Last(@expr?) |	Возвращает последний элемент. Если @expr указано, то возвращает применяет выборку.
| .Where(@expr) |	Фильтрует элементы по указанному выражению. @expr - выражение для фильтрации
| .Skip(@expr) |	Пропускает указанное количество элементов. @expr - выражение для вычисления количества элементов для пропуска
| .Take(@expr) |	Ограничивает количество элементов в запросе. @expr - выражение для вычисления количества элементов для ограничения
| .ToList() |	Преобразует запрос в список.
| .Select(@expr) |	Выбирает элементы из запроса по указанному выражению. @expr - выражение для выбора элементов
| .Include(@expr) |	Включает связанные данные в запрос. @expr - имя навигационного свойства или список свойств через запятую
| .OrderBy(@fieldName) |	Сортирует элементы по указанному полю. @fieldName - имя поля для сортировки
| .OrderByDescending(@fieldName) |	Сортирует элементы по указанному полю в порядке убывания. @fieldName - имя поля для сортировки
| .ThenBy(@fieldName) |	Продолжает сортировку элементов по указанному полю. @fieldName - имя поля для сортировки
| .ThenByDescending(@fieldName) |	Продолжает сортировку элементов по указанному полю в порядке убывания. @fieldName - имя поля для сортировки
| .Search(@searchText) |	Поиск по тексту. @searchText - текст для поиска
| .Table(@page, @size) |	Пагинация. Возвращает элементы в виде таблицы с пагинацией. @page - номер страницы, @size - количество элементов на странице
| .Union(@secondQueryable) |	Объединяет текущий запрос с другим запросом. @secondQueryable - второй запрос для объединения
