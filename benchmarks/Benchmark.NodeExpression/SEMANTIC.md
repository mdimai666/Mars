# Семантический тест-набор: DynamicExpresso vs NCalc 7.2.0

Прогон: `Benchmark.NodeExpression.exe --semantic` (2026-09-26, `SemanticComparison.cs`).
DynamicExpresso — через продакшен-путь `InputValueResolver.ResolveExpression` (varType="",
BindRootPaths + LateBindObject). NCalc — подстановка `msg.*`-путей параметрами (как делала бы
обёртка с классификатором). Сообщение: Payload="hello", count=42 (int), flag=true,
ts=1758800000000 (long), topic="sensor/1".

| # | Выражение | DynamicExpresso | NCalc | Вердикт |
|--:|---|---|---|---|
| 1 | `msg.Payload` | `"hello"` (String) | `"hello"` (String) | MATCH |
| 2 | `msg.count` | `42` (Int32) | `42` (Int32) | MATCH |
| 3 | `msg.count * 2 + 1` | `85` (Int32) | `85` (Int32) | MATCH |
| 4 | `msg.count > 40` | `true` | `true` | MATCH |
| 5 | `msg.count > 40 && msg.flag` | `true` | `true` | MATCH |
| 6 | `msg.count > 50 \|\| msg.flag` | `true` | `true` | MATCH |
| 7 | `!msg.flag` | `false` | `false` | MATCH |
| 8 | `msg.Payload == "hello"` | `true` | `true` | MATCH |
| 9 | `msg.Payload != "hello"` | `false` | `false` | MATCH |
| 10 | `2 / 4` | `0` (Int32) | `0.5` (Double) | ❌ VALUE-DIFF |
| 11 | `7 / 2` | `3` (Int32) | `3.5` (Double) | ❌ VALUE-DIFF |
| 12 | `msg.count / 4` | `10` (Int32) | `10.5` (Double) | ❌ VALUE-DIFF |
| 13 | `msg.count / 4 * 4` | `40` (Int32) | `42` (Double) | ❌ VALUE-DIFF |
| 14 | `10 % 3` | `1` (Int32) | `1` (Int32) | MATCH |
| 15 | `1.5 + 2` | `3.5` (Double) | `3.5` (Double) | MATCH |
| 16 | `"a" == "A"` | `false` | `false` | MATCH |
| 17 | `msg.Payload + "!"` | `"hello!"` (String) | `"hello!"` (String) | MATCH |
| 18 | `msg.ts + 1000` | `1758800001000` (Int64) | `1758800001000` (Int64) | MATCH |
| 19 | `msg.count + 0.5` | `42.5` (Double) | `42.5` (Double) | MATCH |
| 20 | `true` | `true` | `true` | MATCH |
| 21 | `1 == true` | ERR (Invalid Operation) | `true` | ⚠️ NCalc допускает то, что C# отвергает |
| 22 | `msg.missing == null` | ERR (no 'missing') | ERR (Parameter not defined) | BOTH-FAIL (эквивалентно) |
| 23 | `msg.Payload.Count() + 1` | `6` (Int32) | ERR parse | NCalc-FAIL — безопасно |
| 24 | `msg.Payload.Length > 3` | `true` | ERR parse | NCalc-FAIL — безопасно |
| 25 | `msg.Payload.Substring(0, 2)` | `"he"` | ERR parse | NCalc-FAIL — безопасно |
| 26 | `msg.count > 10 ? "big" : "small"` | `"big"` | `"big"` | MATCH (тернарник в NCalc v7 работает) |
| 27 | `string.IsNullOrEmpty(msg.Payload)` | `false` | ERR parse | NCalc-FAIL — безопасно |
| 28 | `msg.count > 40 and msg.flag` | ERR syntax | `true` | NCalc-only синтаксис (`and`/`or`) |

## Выводы

1. **Единственный класс тихих расхождений — целочисленное деление**: NCalc всегда приводит `/`
   к floating point (`2/4 = 0.5`), C# — целочисленно (`0`). Опции переключения нет: в
   `ExpressionOptions` (проверен enum в src/NCalc.Core/ExpressionOptions.cs) division-флага нет,
   `StrictTypeMatching` касается только сравнений, `Evaluate<T>` конвертит лишь финальный
   результат. Расхождение всплывает и в составных выражениях (`msg.count / 4 * 4` → 40 vs 42).
2. Всё остальное из «простого» класса совпадает: арифметика (включая long без деградации в
   double), `%`, сравнения строк (case-sensitive), конкатенация `+`, логика `&&`/`||`/`!`,
   и — неожиданно — тернарный `? :` (однако в v1 классификатора его лучше не пускать без доп. проверок).
3. C#-member-access (`.Count()`, `.Length`, `.Substring`, статика `string.X`) в NCalc падает
   на парсинге — это безопасно: ошибка явная, не тихая.
4. NCalc терпимее к типам (`1 == true` → true, где C# бросает). Для существующих flows это
   смена поведения «ошибка → значение»; лечится `StrictTypeMatching` (тогда → false) или
   запретом сравнения разнотипных литералов в классификаторе.

## Правила классификатора v1 (следствие из набора)

Пускать в NCalc только выражения, целиком состоящие из:
- корневых путей (`msg.` / `flow.` / `global.` / `VarNode.` / `env`), числовых/строковых/bool-литералов;
- операторов `+ - * %`, сравнений `== != > >= < <=`, логики `&& || !`, скобок;
- **без `/`** (v1: деление всегда уходит в DynamicExpresso; альтернатива — кастомный
  evaluation visitor с C#-семантикой целочисленного деления, NCalc это поддерживает через DI);
- **без member access** (любая `.` после первого сегмента пути, кроме самих корневых путей),
  **без вызовов функций/методов**, без `? :`, без строковых литералов с интерполяцией.

Всё, что не прошло классификатор или упало при parse в NCalc, — в DynamicExpresso
(решение кэшировать на текст выражения). Обратный fallback NCalc→DE на parse-ошибке безопасен:
расхождение деления — единственный тихий класс, и он исключён правилом «без `/`».
