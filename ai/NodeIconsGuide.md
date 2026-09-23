# Node Icons Guide — векторный набор иконок нод в стиле VS two-tone

Гайд для агента: как рисовать и подключать иконки нод Mars. Стиль утверждён 2026-09-24
(обсуждение с пользователем: четыре развилки — язык графики, акценты, обводка, метафоры).
Эталон — `src/Mars.Nodes/Mars.Nodes.Workspace/wwwroot/nodes/debug.svg` (жук-баг).

## Инварианты (спека)

- **Формат**: рукописный SVG, `viewBox="0 0 48 48"` + `width="48" height="48"`; без `<text>`
  (зависимость от шрифтов — прецедент битого `string.svg`), без градиентов/теней/фильтров,
  без внешних ссылок.
- **Расположение и имена**: `src/Mars.Nodes/Mars.Nodes.Workspace/wwwroot/nodes/<name>.svg`,
  имена семантические kebab-case по смыслу ноды (`debug.svg`, `catch-error.svg`),
  НЕ по источнику (`chat-left.svg` — антипример).
- **Подключение**: `Icon = "_content/Mars.Nodes.Workspace/nodes/<name>.svg";`
  в конструкторе ноды (`src/Mars.Nodes/Mars.Nodes.Core/Nodes/**`). Если у ноды иконки не
  было — добавить строку в конструктор. Fallback-хардкоды в компонентах вида
  `CommentNodeComponent.razor.cs` (`chat.svg` при пустом `Node.Icon`) не трогать:
  `Node.Icon` имеет приоритет.
- **Рендер**: `Mars.Nodes.Front.Abstractions/Components/NodeViews/NodeComponent.razor` —
  `<image>` 26×26 (x=5, y=2) в левой зоне 30px; подложка = `Node.Color` + чёрный 5%
  (`.red-ui-flow-node-icon-shade`, `Mars.Nodes.Workspace/wwwroot/styles.css`).
- **Палитра**: чернила `#404040`; внутренняя заливка (counter) `#F0F0F0`;
  keyline `#FFFFFF`; акценты точечно (ошибка/предупреждение — `#FFC400`, см. `catch-error.svg`).
- **Two-tone обязателен**: тёмный контур/формы + светлая внутренняя заливка. Палитра нод —
  от `#f5f4f4` (CommentNode) до `#1e1e1e` (DevMicroschemeNode): одноцветный глиф
  проваливается на одном из краёв диапазона, two-tone самодостаточен.
- **Структура файла**: две группы. (1) keyline-копия силуэта: `fill="#FFFFFF"
  stroke="#FFFFFF"`, `stroke-linecap/linejoin="round"`, `stroke-width` = толщина чернил + 4
  (гало ≈2 единицы снаружи); заливные фигуры — `stroke-width="4"`. (2) лицевая: чернила/counter.
  Пунктир (плейсхолдеры) — только в лицевой группе, keyline-копия сплошная.
- **Толщины (сетка 48)**: основной контур 4; второстепенные штрихи/строки 3–3.5;
  точки r 2.4–2.6; скругления малые (rx 4–6). Отступ силуэта с keyline от края ≥2 единицы.
- **Акцент**: не более одного на иконку и только там, где несёт смысл (ошибка, бренд-марка);
  база набора монохромная.
- **Пары и семейства**: связанные ноды (link in/out) — зеркальные глифы; одна метафора не
  должна сидеть на двух разных нодах одновременно.

## Как добавить иконку (рецепт)

1. Метафора: отличима от соседей по набору; сверить с текущими `nodes/*.svg`, не дублировать.
2. Нарисовать svg по спеке (две группы: keyline, затем лицевая).
3. Подключить `Icon =` в `.cs` ноды (или добавить строку, если иконки не было).
4. `dotnet build src/Mars.Nodes/Mars.Nodes.Core`.
5. Превью: дописать `<img … width="40" height="40">` в строки подложек
   `.qwen/tmp/icon-preview.html` (`#7AB073`, `#ffea9f`, `#1e1e1e`, `#ffffff`) и в строку 26px,
   затем скриншот headless Edge:
   `"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless=new --disable-gpu --hide-scrollbars --window-size=480,300 --screenshot="C:\Users\D\Documents\VisualStudio\2026\Mars\.qwen\tmp\icon-preview.png" "file:///C:/Users/D/Documents/VisualStudio/2026/Mars/.qwen/tmp/icon-preview.html"`
   и прочитать png.
6. Чек-лист: читается на всех четырёх подложках и в 26px; keyline не «толстит» форму;
   пары/соседи различимы; на подложке цвета самой ноды (grep `Color = "#` в её `.cs`).

## Грабли

- Белые монохромные svg (legacy `chat-left.svg`, `function.svg`, `chat.svg`) невидимы на
  светлых нодах — не повторять; тёмные монохромные тонут на `#1e1e1e`.
- Keyline делать ТОЛЬКО задней копией фигур; не фильтром/тенью и не stroke поверх лицевой
  группы (испортит two-tone).
- Одна иконка на несколько нод = ноды неразличимы в редакторе. Прецеденты до 2026-09-24:
  `box-arrow-in-right.svg` на Inject/LinkIn/LinkOut, `loop-start.svg` на
  CatchError/KillTaskJob/TerminateAllJobs.
- Замена содержимого существующего файла по тому же URL: браузер держит статику `_content`
  эвристическим кешем без `Cache-Control` — либо новое имя файла, либо bump
  `MarsAppVersion` (см. гайд по cache-busting статики). Новые имена — без bump.
- Legacy-имена и ассеты VS Image Library (`*-48.png`) — референс стиля, не источник:
  чужая лицензия, растр, мыло в 26px.

## Отклонено

- Белый трафарет (одноцветный глиф) — провал на светлых нодах.
- Бейдж на нейтральной подложке-тайле (как `mqtt-64.svg`) — узел «в клетку».
- Контур с прозрачным низом — провал на тёмных нодах.
- Готовые наборы (Bootstrap/Fluent/Azure) как есть — стилистически вразнобой.

## Статус

- **Готово (2026-09-24), группа common**: `debug.svg` (жук), `inject.svg` (молния),
  `link-in.svg`/`link-out.svg` (кольцо-портал + стрелка, зеркальная пара),
  `comment.svg` (пузырь со строками), `var.svg` (ячейка с косым крестом),
  `catch-error.svg` (треугольник с «!», акцент `#FFC400`), `unknown.svg` (пунктирный
  квадрат с «?»). CallNode/CallResponseNode — отложены пользователем.
- **Освободились**: `chat-left.svg` и `chat.svg` (потребителей нет);
  `box-arrow-in-right.svg` — только CounterNode; `loop-start.svg` — только TaskNodes.
- **Очередь**: legacy `*-48.png`/`*-64.*` и белые svg остальных групп
  (карта: grep `Icon = "` по `src/Mars.Nodes/Mars.Nodes.Core/Nodes/**`),
  затем CallNode/CallResponseNode.
