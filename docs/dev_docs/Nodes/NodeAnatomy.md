<!-- Title: Устройство ноды -->
<!-- Order: 2 -->

# Устройство ноды

Справочник по всем частям ноды визуального редактора Mars: сообщение, порты, поля значений,
выражения, контракты выходов, отладка. Пошаговое создание первой ноды —
в [CreateFirstNode](CreateFirstNode.md).

## Из чего состоит нода

| Часть | Что делает | Где живёт |
|---|---|---|
| Модель (`XxxNode : Node`) | Настройки ноды; публичные свойства сохраняются в JSON схемы | Frontend/Shared проект |
| Реализация (`INodeImplement<XxxNode>`) | Логика выполнения на сервере | Backend проект |
| Форма (`XxxNodeForm.razor`) | Редактирование настроек двойным кликом | Frontend проект |
| Справка (`XxxNode.md` + `.ru.md`) | Вкладка помощи в форме ноды | `wwwroot` frontend-проекта |
| Пример (`INodeExample<XxxNode>`) | Готовая схема в списке примеров редактора | по модели ноды |

Жизненный цикл: модели нод создаются при открытии схемы, реализации — при деплое схемы
(кнопка Deploy) и живут до следующего деплоя. Поэтому **не храните состояние между
сообщениями в полях реализации** — используйте сообщение, контексты или Var-ноды.

## Сообщение NodeMsg

По проводам схемы течёт `NodeMsg` — payload плюс словарь контекста:

```csharp
input.Payload                 // основная нагрузка (объект, строка, число — что положили)
input.Get("status")           // значение из контекста по ключу
input.Set("status", "ok")     // записать в контекст
input.Get<RequestUserInfo>()  // значение из контекста по имени типа
input.AsFullDict()            // Payload + Context одним словарём
```

В выражениях и подсказках контекст доступен как `msg.<ключ>` (например, `msg.status`),
а payload — как `msg.Payload`.

Соглашение нод с полями (Inject, VariableSet и др.): поле с ключом `Payload` попадает
в `msg.Payload`, остальные ключи — в контекст сообщения.

## Порты и провода

- `Inputs` / `Outputs` — списки портов ноды; количество выходов можно менять и из формы
  (`OutputCount = N`, как у Switch — по одному выходу на условие).
- Передача сообщения дальше — делегат `callback` из `Execute`:

```csharp
callback(input);       // в первый выходной порт (0)
callback(input, 2);    // в порт с индексом 2
// callback не вызван — ветка схемы здесь заканчивается (нода-тупик)
```

- `callback` можно вызвать несколько раз — так работают Split и Foreach (по сообщению
  на элемент).
- `input.Copy()` создаёт копию сообщения с тем же payload (по ссылке) и копией контекста —
  полезно, когда в разные порты нужно отправить разные данные.

## Статус, отладка и ошибки

```csharp
RNS.Status(new NodeStatus("текст под нодой"));
RNS.DebugMsg(DebugMessage.NodeMessage(Node.Id, "сообщение в панель отладки"));
RNS.DebugMsg(DebugMessage.NodeException(Node.Id, ex));
throw new NodeExecuteException(Node, "текст ошибки");   // ошибка на ноде + остановка ветки
```

В редакторе есть тумблер **DEBUG**: в этом режиме рантайм запоминает последние сообщения,
которые каждая нода отдала дальше (ключ — «нода + порт»), и они видны в панели INPUT
и в подсказках полей вместе со значениями. Режим выключается сам через 30 минут;
от кода ноды ничего не требуется.

## Поля со значениями: const, msg, expression

Каждое поле ноды, принимающее значение, может работать в одном из трёх режимов
(«источник значения», в JSON — свойство `ValueKind`):

| Kind | Что значит | Пример значения |
|---|---|---|
| `const` | литерал, разбирается по типу поля (`VarType`) | `42`, `true`, `{"a":1}` (JSON для массивов) |
| `msg` | путь к полю входящего сообщения | `Payload.status`, `items[0].name` |
| `expression` | C#-выражение (движок DynamicExpresso) | `msg.Payload.Count() + 1` |

Ось «тип значения» (`VarType`: `int`, `long`, `float`, `double`, `decimal`, `bool`, `string`,
`DateTime`, `Guid`, массивы и `timestamp`) ортогональна оси источника: результат любого
источника приводится к declared-типу, ошибка разбора — это `NodeExecuteException` с именем поля.

В выражениях доступны корни:

- `msg` — входящее сообщение (`msg.Payload`, `msg.<ключ контекста>`);
- `GlobalContext` и `FlowContext` — глобальные и потоковые переменные;
- `VarNode` — значения Var-нод схемы;
- `env("KEY")` — переменные окружения;
- LINQ подключён (`msg.Payload.Count()`, `.Where(...)`, `.Select(...)`).

### Ввод значений в формах

- `MarsValueInput` — поле значения. Соглашение ввода: текст без префикса — `const`;
  с префиксом `@` — выражение (`@msg.Payload.Count() + 1`). Поле, сохранённое как `msg`,
  отображается как `@msg.<путь>`.
  Хоткеи: `Ctrl+Space` — автокомплит, `Alt+Down` — дерево полей, `Alt+Up` — операции,
  `F4` — дата-пикер для `DateTime`.
- `MarsPathInput` — поле пути к свойству (без `@`), например для Template или Debug-нод.

Оба компонента сами показывают подсказки: пути строятся по контрактам выходов
предыдущих нод (см. ниже), а в режиме DEBUG — ещё и по реальным значениям.

### Как подключить такое поле к своей ноде

В модели храните режим и значение плоско, строками:

```csharp
public string UrlKind { get; set; } = InputValueKind.Const;   // "const" | "msg" | "expression"
public string Url { get; set; } = "";
```

В реализации резолвьте единым резолвером (свой интерпретатор создавать не нужно):

```csharp
var interpreter = InputValueResolver.CreateInterpreter(RNS, input);
var url = (string)InputValueResolver.Resolve(
    Node.UrlKind, Node.Url, "string",
    interpreter, new ExpressionScope(RNS, input), Node, "Url")!;
```

В форме используйте `MarsValueInput` с конвертером `@`-префикса (готовый образец —
форма ноды FileWrite, `FileWriteNodeForm.razor` в репозитории).

## Контракты выходов (подсказки полей)

Чтобы редактор подсказывал пути `msg.` после вашей ноды, объявите, что она кладёт в сообщение:

- выход одного типа всегда — атрибут **на реализации** (можно несколько):

```csharp
[NodeOutputValueSpec(typeof(string))]                                  // Payload : string
[NodeOutputValueSpec(typeof(MyDto), Name = "requestInfo")]             // msg.requestInfo : MyDto
[NodeOutputValueSpec(typeof(int), OutputPort = 1, Description = "...")]// значение уходит из порта 1
public class MyNodeImpl : INodeImplement<MyNode> { ... }
```

  Тип разворачивается в плоские пути автоматически: вложенные свойства — `Payload.user.name`,
  массивы — `Payload.items[].id`. `Dictionary`/`JsonElement` не разворачиваются (ключи неизвестны).

- выход зависит от настроек ноды — интерфейс **на модели** (как у Inject — спек из списка полей):

```csharp
public class MyNode : Node, INodeOutputValueSpec
{
    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        yield return new OutputValueSpec("Payload", "string");
    }
}
```

- ничего не объявили — нода считается транзитной: подсказки придут от предыдущих нод по проводам.

## Конфиг-ноды

Общие настройки (SMTP, MQTT-брокер, подключения) выносятся в конфиг-ноды
(`XxxConfigNode : ConfigNode`), а рабочая нода ссылается на них полем `InputConfig<TConfigNode>`.
В реализации конфиг обязательно разрешается в конструкторе:

```csharp
public MyNodeImpl(MyNode node, IRuntimeNodeScope rns)
{
    Node = node;
    RNS = rns;
    Node.Config = RNS.GetConfig(node.Config);   // всегда
}
```

В форме для такого поля есть компонент `InputConfigField`.

## Ноды с ручным завершением

Обычно job завершается, когда `Execute` отработал и все ветки дошли до тупиков. Если нода
отдаёт результат асинхронно позже (например, HTTP-ответ приходит отдельным запросом),
реализуйте маркерный интерфейс `ISelfFinalizingNode` и вызовите `RNS.Done(parameters)`
в точке завершения. Без маркера `Done` бросает исключение.

## JSON ноды

Схема (flows.json) хранит ноды как JSON; ключевое поле — `TypeId` (у встроенных нод —
`core.InjectNode` и т.п., у плагинов — полное имя класса). Пример ноды Inject:

```json
{
  "TypeId": "core.InjectNode",
  "Id": "…",
  "Fields": [
    { "Key": "Payload", "VarType": "string", "ValueKind": "const", "Value": "Hello!" },
    { "Key": "doubled", "VarType": "int", "ValueKind": "expression", "Value": "21 * 2" }
  ],
  "Wires": [["<id следующей ноды>"]]
}
```

`Wires` — список по одному на выходной порт ноды (индекс списка = номер выхода); внутри —
провода этого выхода в формате `"<id ноды-получателя>"` (вход в порт 0) или
`"<id ноды-получателя>#<номер входного порта>"`.

`TypeId` — идентификатор ноды в сохранённых схемах: переименование класса ноды плагина
без явного `override TypeId` сломает существующие схемы пользователей.

## Палитра: имя, группа, цвет, иконка

- Группа — `[Display(GroupName = "…")]` на модели; без атрибута нода попадает в `other`.
- Имя — свойство `Name` (задаётся в форме), иначе имя класса без суффикса `Node`.
- Цвет — `Color` в конструкторе (hex), служит фоном ноды.
- Иконка — `Icon` в конструкторе: URL картинки (svg/png). Встроенные иконки лежат в
  `Mars.Nodes.Workspace/wwwroot/nodes/`, у плагинов — в их `wwwroot` (`/_plugin/<имя>/…`).
- `IsInjectable = true` — кнопка ручного запуска прямо на ноде (как у Inject).

## Примеры для списка примеров

Класс `INodeExample<TNode>` рядом с моделью добавляет готовую схему в список примеров
редактора:

```csharp
public class MyNodeExample1 : INodeExample<MyNode>
{
    public string Name => "My node basics";
    public string Description => "Inject → MyNode → Debug.";

    public IReadOnlyCollection<Node> Handle(IEditorState editorState) =>
        NodesWorkflowBuilder.Create()
            .AddNext(new InjectNode().SetPayload("hello"))
            .AddNext(new MyNode())
            .AddNext(new DebugNode())
            .Build();
}
```

## Инструкция для AI-агента

Если вы используете AI-агента для создания ноды внутри репозитория Mars или плагина,
передайте ему файл:

> [ai/NodeCreationGuide.md](../../../ai/NodeCreationGuide.md)

Он содержит состав ноды, конвенции (TypeId, категории, справка, тесты), рецепты полей
значений и контрактов выходов, грабли и инварианты.
