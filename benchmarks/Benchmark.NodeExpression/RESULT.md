// * Summary *

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700 2.10GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

Прогон 2 (2026-09-26, с NCalc 7.2.0):

| Method                      | Mean            | Error           | StdDev        | Ratio      | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------- |----------------:|----------------:|--------------:|-----------:|---------:|-------:|-------:|----------:|------------:|
| DirectRead                  |       0.9483 ns |       0.1043 ns |     0.0057 ns |       1.00 |     0.01 |      - |      - |         - |          NA |
| ExpressionCurrent           | 107,113.1185 ns |  80,990.3079 ns | 4,439.3521 ns | 112,955.35 | 4,096.85 | 2.1973 | 1.9531 |  34779 B |          NA |
| ExpressionHotPath           |       2.2725 ns |       0.2521 ns |     0.0138 ns |       2.40 |     0.02 |      - |      - |         - |          NA |
| ExpressionCachedLambda      |     350.0109 ns |      52.4974 ns |     2.8776 ns |     369.10 |     3.26 | 0.0563 |      - |     888 B |          NA |
| NCalcBracketParam           |     123.0134 ns |      69.2286 ns |     3.7947 ns |     129.72 |     3.53 | 0.0651 |      - |    1024 B |          NA |
| NCalcEvaluateParameterEvent |     107.9537 ns |      31.4155 ns |     1.7220 ns |     113.84 |     1.68 | 0.0622 | 0.0001 |     976 B |          NA |
| NCalcCachedExpression       |      28.6473 ns |       7.6892 ns |     0.4215 ns |      30.21 |     0.42 | 0.0071 |      - |     112 B |          NA |
| DynExpressoParamLambda      |     194.771 ns  |      33.962 ns  |     1.862 ns  |         —  |        — | 0.0396 |      - |     624 B |          NA |
| DynExpressoParamLambdaWithPathResolve | 395.587 ns |     116.73 ns  |     6.399 ns  |         —  |        — | 0.0930 |      - |    1464 B |          NA |
| CreateInterpreter           |  81,042.1468 ns |   6,991.8380 ns |    383.2462 ns |  85,462.40 |   566.46 | 1.7090 |      - |  28168 B |          NA |
| BindRootPaths               |     504.6382 ns |     314.2064 ns |    17.2227 ns |     532.16 |    15.97 | 0.1001 |      - |    1584 B |          NA |
| ParseExpression             |     594.2251 ns |      40.8842 ns |     2.2410 ns |     626.64 |     3.85 | 0.0887 |      - |    1400 B |          NA |
| EvalRawExpression           | 554,869.9870 ns | 174,798.6500 ns | 9,581.3039 ns | 585,134.06 | 9,266.46 | 0.9766 |      - |   26251 B |          NA |

Прогон 1 (2026-09-26, без NCalc) — первые 4 ветки и декомпозиция совпадают в пределах шума
(DirectRead 2.18 нс, ExpressionCurrent 104.5 мкс, HotPath 3.28 нс, CachedLambda 344 нс).

Прогон 4 (2026-09-26, после внедрения фазы 1 — пул `ExpressionRunnerPool`, `ExpressionCurrent`
меряет новый прод-путь):

| Method | Mean | Allocated | Было (прогон 2) |
|---|---:|---:|---|
| ExpressionCurrent | **16,655 ns** | **7 689 B** | 107 113 ns / 34 779 B → **6.4× / 4.5×** |
| остальные ветки | — | — | в пределах шума |

Остаточные ~16 мкс — `Expression.Compile` внутри каждого `interpreter.Eval` (делегат компилируется
на вызов; `ParseExpression`=597 нс — только парсинг без компиляции). Снимаются фазой 2 (hot path)
и фазой 3 (NCalc) для горячих выражений; для сложных C#-выражений — фаза 4 (Lambda-кэш, отложена).

Прогон 5 (2026-09-26, после фазы 2 — hot path одиночных корневых путей в `ExpressionRunner`):

| Method | Mean | Allocated | Было (прогон 4) |
|---|---:|---:|---|
| ExpressionCurrent | **97.0 ns** | **56 B** | 16 655 ns / 7 689 B → **171× / 137×** |

Прод-путь для `@msg.Payload` — 97 нс / 56 Б: чуть дороже прототипов (hot path 3.2 нс, NCalc cached
24.8 нс) из-за аренды сессии, интерполяции source и конверсии VarType, но это уже «бесплатно»
против исходных 107 мкс (итоговое ускорение ~1100×).

Прогон 6 (2026-09-26, после фазы 3 — NCalc для простых выражений; новая ветка
`ExpressionCurrentArithmetic` — прод-путь с полем `msg.count * 2 + 1`):

| Method | Mean | Allocated |
|---|---:|---:|
| ExpressionCurrent (`msg.Payload`, hot path) | 100.1 ns | 56 B |
| **ExpressionCurrentArithmetic (`msg.count * 2 + 1`, NCalc)** | **342.5 ns** | **1041 B** |

До фазы 3 арифметика шла DE-путём (~16.7 мкс / ~7.7 КБ, прогон 4) → **~49× быстрее**.
От прототипа NCalcCachedExpression (24 нс) прод-путь отличают: аренда/возврат сессии,
классификатор-lookup, `DynamicNodeMsgWrapper` в `TryResolveRootPath` (~200 нс / 840 Б — основная
статья аллокаций) и заполнение Parameters.

Прогон 7 (2026-09-26, после фазы 4 — DE Lambda-кэш + C#-деление в NCalc; новая ветка
`ExpressionCurrentComplex` — прод-путь с `msg.Payload.Count() + 1`):

| Method | Mean | Allocated |
|---|---:|---:|
| ExpressionCurrent (`msg.Payload`, hot path) | ~98 ns | 56 B |
| ExpressionCurrentArithmetic (`msg.count * 2 + 1`, NCalc) | 358 ns | 1041 B |
| **ExpressionCurrentComplex (`msg.Payload.Count() + 1`, DE Lambda-кэш)** | **934 ns** | **3008 B** |

Сложные C#-выражения до фазы 4 шли полным Eval-путём (~16.7 мкс, прогон 4) → **~18× быстрее**.
Итоговая лестница прод-пути: hot path ~100 нс → NCalc ~360 нс → DE Lambda-кэш ~930 нс
(против 107 мкс на старте). Остаток в DE-ветке: wrapper + regex CollectRootPaths + Parameter[]
на сообщение; полный Eval остаётся только для fallback'ов (остаточные ссылки на msg/контексты,
Parse-ошибки).

## Условия

Все ветки — исполнение ноды с одним полем `Payload` (`ValueKind=expression`, `Value="msg.Payload"`,
`VarType=string`) на одном сообщении (Payload — строка, 3 Context-ключа). Ветка ExpressionCurrent —
реальный `InjectNodeImpl` + `InputValueResolver` из продакшена; остальные — прототипы внутри бенчмарка.

NCalc-ветки (v7.2.0):
- `NCalcBracketParam` — `new Expression("[msg_Payload]")` на сообщение + параметр-значение
  (аналог BindRootPaths; внутренний parse-кэш NCalc).
- `NCalcEvaluateParameterEvent` — подход из git-истории (`SwitchNodeImpl` @ `35933d6b`):
  имя с точкой в скобках `[msg.Payload]` + обработчик `EvaluateParameter`.
- `NCalcCachedExpression` — объект `Expression` кэширован, на сообщение только Parameters + Evaluate.

## Выводы (2026-09-26)

- Текущий путь `@msg.Payload` стоит **~107 мкс и ~34 КБ аллокаций на сообщение**.
- **Главное узкое место — `CreateInterpreter`: ~81 мкс и 28 КБ** (75% времени ветки ExpressionCurrent).
  Интерпретатор создаётся заново на каждое сообщение (`InjectNodeImpl.Execute`).
- **Hot path** (точное совпадение с `msg.Payload`) — 2.3 нс / 0 Б, но лечит только один шаблон.
- **NCalc быстрее DynamicExpresso на всех уровнях**:
  - даже `new Expression` на сообщение (с внутренним parse-кэшем) — **123 нс**, в ~3 раза быстрее
    DynamicExpresso cached Lambda (350 нс);
  - подход из git-истории с `EvaluateParameter` — 108 нс (имя с точкой в скобках работает,
    проблема 2024 года «dot is Parsing error» решена);
  - кэш объекта `Expression` — **28.6 нс / 112 Б** (~12× быстрее DynamicExpresso cached Lambda,
    в 30 раз медленнее прямого чтения — практически бесплатно).
- `Eval("msg.Payload")` без BindRootPaths — 555 мкс: dynamic-байндинг по `DynamicNodeMsgWrapper`
  на каждый eval. Статическая типизация приёмника критична.
- Ограничение NCalc: **синтаксис не C#** — нет LINQ/методов на объектах (`msg.Payload.Count() + 1`
  не переносится), member access «из коробки» по-прежнему нет (issue ncalc/ncalc#216 — только
  кастомный parser/visitor). Семантические отличия (например, арифметика) требуют проверки до
  перевода реальных выражений. Роль NCalc — простые выражения (арифметика/сравнение/логика),
  сложные остаются на DynamicExpresso.
- Кэш `Expression` у NCalc разделяемый и мутируемый (`Parameters`) — при параллельных воркерах
  `NodeTaskJob` (до 10) нужна та же стратегия, что и для Interpreter: пул/лок на ноду.

Прогон 3 (2026-09-26): DynamicExpresso с Lambda на объявленных параметрах (без SetVariable/wrapper
на Invoke) — **194.8 нс / 624 Б**; с честным резолвингом пути через `DynamicNodeMsgWrapper.GetValueByPath`
(как в `TryResolveRootPath`) — **395.6 нс / 1464 Б**. Параметризация даёт ~1.8× против
`ExpressionCachedLambda` (350 нс), но до NCalc cached (27 нс) не дотягивает: у DynamicExpresso
сам Invoke через Parameter-обёртки дороже, а ~200 нс / 840 Б сверху — это конструктор
`DynamicNodeMsgWrapper` (рефлексия свойств payload). Вывод: для простых выражений NCalc быстрее
любого варианта DynamicExpresso; единый движок «только DynamicExpresso» не достигает уровня NCalc.
