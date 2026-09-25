# План: движок выражений нод — пул-сервис, hot path, NCalc для простых выражений

> **Статус: фазы 0–4 выполнены и закоммичены 2026-09-26 (030dedd8 → d5399319); InlineFunctionNode —
> вариант A выполнен (не закоммичен), вариант B открыт. Итог: `@msg.Payload` 107 мкс→~98 нс / 56 Б
> (~1100×), простая арифметика →358 нс / 1 КБ, сложное C#-выражение →934 нс / 3 КБ; 649/649 тестов.**
> Задача-источник: замер стоимости интерпретации `@msg.Payload` (2026-09-26) показал ~107 мкс
> и ~34 КБ на сообщение в текущем пути; главное узкое место — `InputValueResolver.CreateInterpreter`
> на каждое сообщение (~81 мкс, 75%). Замеры: `benchmarks/Benchmark.NodeExpression/`
> (`RESULT.md` — производительность, `SEMANTIC.md` — сравнение семантики DE vs NCalc,
> запуск бенчмарка — exe из bin, семантики — `--semantic`).
> Дизайн согласован с пользователем 2026-09-26: НЕ хранить кэш в полях impl («технический код,
> хранить поля в Impl не очень»), а ленивый пул-сервис на ноду + extension к RNS + обёртка над
> движками; классификатор простых выражений (не try-parse); NCalc — только для простых; фаза 4 отложена;
> InlineFunctionNode — отдельно обсудить позже.

## Принятые решения (2026-09-26)

1. **Пул-сервис, а не поля в impl**: `ExpressionRunnerPool` (singleton) хранит по node.Id мешок
   `ExpressionRunner` (rent/return) — параллелизм `NodeTaskJob` (до 10 воркеров на канал) не даёт
   шарить один Interpreter на ноду без синхронизации; аренда вместо лока не сериализует воркеров.
2. **Доступ через extension к `IRuntimeNodeScope`** (`rns.Expressions(node)` → disposable-сессия):
   `ServiceProvider` у RNS уже есть, impl не «достаёт» сервис руками. Пула нет в провайдере
   (тесты/моки с NSubstitute) → ephemeral runner, поведение идентично текущему (создание на вызов).
3. **Обёртка над движками, лестница стоимости**: hot path (одиночный корневой путь, ~2 нс) →
   NCalc для простых выражений (~27 нс) → DynamicExpresso для полных C#-выражений (~400 нс–2 мкс).
   Текущий путь — 107 мкс.
4. **Классификатор «простых» выражений — явный (regex/сканер), не try-parse**: выражение, валидное
   в обоих движках, но с разным результатом, — тихая катастрофа; найден ровно один такой класс —
   **целочисленное деление** (NCalc: `2/4=0.5`, C#: `0`; штатной опции переключения в NCalc 7.2.0
   нет — проверен enum `ExpressionOptions` в исходниках). Решение: в v1 (фаза 3) `/` не пускали;
   в фазе 4 `/` РАЗРЕШЁН — C#-семантика обеспечивается обработчиком `EvaluateBinary`
   (`ExpressionRunner.CSharpIntegerDivision`, промоция по правилам C#; ulong не обрабатывается).
   Второй найденный класс (фаза 4): **string+number коэрция** — NCalc по умолчанию `"7" + 1` →
   `8.0`, C# → `"71"`; закрыт опцией `NoStringTypeCoercion` (смешанные выражения падают в NCalc
   и уходят в DE-fallback; string+string конкатенация сохраняется).
5. **Опции NCalc**: `StrictTypeMatching` (гасит коэрцию вида `1 == true` → в C# это ошибка)
   + `OrdinalStringComparer` (ordinal-семантика сравнения строк как в C#).
6. **NCalc — только простые выражения**: member access (`.Count()`, `.Length`, `.Substring()`,
   статика) в NCalc не работает (issue ncalc/ncalc#216: только кастомный parser/visitor) —
   остаётся на DE. Тернарник `? :` в NCalc v7 работает, но в v1 не пускаем (мало данных).
7. **InlineFunctionNode (@expr-аргументы) — НЕ мигрируем** в этот заход (обсуждение отдельно).
   `NodeService.VarNodesSetDefaultValues` (не per-message) — тоже не трогаем.
8. **Фаза 4 отложена**: DE Lambda-кэш с типизированными параметрами; кастомный evaluation visitor
   NCalc для C#-семантики деления (открыл бы `/` для быстрого пути).

## Замеры (этап 0, отправная точка)

`benchmarks/Benchmark.NodeExpression/RESULT.md` (i7-13700, .NET 10.0.12, ShortRun):

| Путь | Mean | Allocated |
|---|---:|---:|
| Прямое чтение `msg.Payload` | ~1 нс | 0 Б |
| Текущий прод (`InjectNodeImpl` + `InputValueResolver`) | 107 мкс | 34 779 Б |
| Hot path (прототип) | 2.0 нс | 0 Б |
| NCalc, кэш `Expression` | 27.4 нс | 112 Б |
| NCalc, `new Expression` на сообщение (внутренний parse-кэш) | 117 нс | 1 024 Б |
| DE, Lambda с объявленными параметрами | 195 нс | 624 Б |
| DE, кэш Lambda + SetVariable + wrapper | 345 нс | 888 Б |
| `CreateInterpreter` (главная стоимость текущего пути) | 81 мкс | 28 168 Б |
| `Eval("msg.Payload")` без BindRootPaths (dynamic-байндинг) | 555 мкс | 26 131 Б |

Семантика: `SEMANTIC.md` — 28 выражений, единственное тихое расхождение — целочисленное деление;
C#-member-access в NCalc падает на парсинге (безопасно); `and`/`or` — NCalc-only синтаксис.

Git-предыстория: NCalc-ветка уже была в `SwitchNodeImpl` (`35933d6b`, под мёртвым `#if`, пакет не
подключался; удалена в `c06f4da6`). Проблема тех лет «точка в имени параметра — Parsing error»
в v7 решена скобками `[msg.Payload]`; подход с `EvaluateParameter` воспроизведён в бенчмарке (108 нс).

---

## Фаза 1 — пул-сервис + переиспользование Interpreter (семантика не меняется) ✅

Новые файлы в `src/Mars.Nodes/Mars.Nodes.Expressions/`:

- [x] `ExpressionRunner.cs` — на ноду: кэшированный `Interpreter` (дорогая часть — `new Interpreter`
      + `Reference(typeof(Enumerable))` — один раз), на сообщение — дешёвая перепривязка переменных
      (`msg`-wrapper, GlobalContext/FlowContext/VarNode-обёртки; RNS пересоздаётся на сообщение —
      `NodeTaskJob.callbackNext`, поэтому перепривязка ОБЯЗАТЕЛЬНА). API: `Resolve(kind, value,
      varType, msg, rns, node, source)` + `GetInterpreter(msg, rns)` (для `VariableSetNodeImpl.
      SetExpression`). В `InputValueResolver` выделить `RebindVariables(interpreter, scope)` —
      общий для `CreateInterpreter` и runner'а.
- [x] `ExpressionRunnerPool.cs` — singleton: `ConcurrentDictionary<string nodeId,
      ConcurrentBag<ExpressionRunner>>`; `Rent(nodeId)` (пусто → new), `Return(nodeId, runner)`.
      Runner не привязан к RNS (всё перепривязывается на сообщение) — сброс пула на редеплой
      flow не нужен; node.Id стабилен.
- [x] `RuntimeNodeScopeExtensions.cs` — `Expressions(this IRuntimeNodeScope rns, Node node)` →
      `ExpressionSession : IDisposable` (аренда; `Dispose` — возврат). Нет пула в
      `rns.ServiceProvider` → ephemeral runner (тесты).
- [x] Регистрация `AddSingleton<ExpressionRunnerPool>()` — `Mars.Nodes.Host/MainNodes.cs`.

Миграция impl'ов (`interpreter ??= CreateInterpreter(RNS, input)` + `InputValueResolver.Resolve(…)`
→ `using var expr = RNS.Expressions(Node)` + `expr.Resolve(…)`): Inject, Switch, Eval, FileWrite,
FileRead, HttpRequest, MqttOut, EmailSend, VariableSet, DevAdminConnection.

- [x] Тесты: `tests/Mars.Nodes.Tests/Expressions/ExpressionRunnerPoolTests.cs` — 6 тестов
      (rent/return reuse, параллельная аренда, изоляция по node.Id, rebind msg на сообщение,
      fallback без пула, сессии через пул); `Mars.Nodes.Tests` — 572/572.
- [x] Проверка: build slnx 0 errors + прогон бенчмарка — `ExpressionCurrent` 107→**16.7 мкс**,
      34 779→**7 689 Б** (RESULT.md, прогон 4). Остаток — `Expression.Compile` на каждый Eval;
      ожидается фазами 2–3 (горячие выражения) и фазой 4 (сложные, отложена).

## Фаза 2 — hot path: одиночный корневой путь без движка ✅

- [x] В `ExpressionRunner.Resolve`: kind `expression`/`msg`, значение — ЦЕЛИКОМ одиночный корневой
      путь (`InputValueResolver.IsSingleRootPath`, anchored-regex `^(msg|GlobalContext|FlowContext|
      VarNode)(\.ident)+$`) → `TryResolveRootPath` + `ConvertToVarType`, без движков. Путь не
      разрешился (null) → fallthrough в DE (сохраняет его ошибки; тексты ошибок для неразрешимых
      путей могут отличаться от DE — оба `NodeExecuteException`).
- [x] Для `msg.Payload` при payload null/string/primitive — чтение `NodeMsg.Payload` напрямую, без
      `DynamicNodeMsgWrapper` (у таких payload нет свойства "Payload", затенения обёртки не возникает;
      complex-payload идёт через wrapper — приоритет его свойств сохраняется).
- [x] Тесты: `ExpressionRunnerHotPathTests` — 9 фактов (payload/context/conversion/null-поведение/
      fallback-ошибки/сложные выражения через движок) + 12 InlineData на `IsSingleRootPath`;
      `Mars.Nodes.Tests` — 593/593.
- [x] Проверка: бенчмарк (прогон 5) — `ExpressionCurrent` 16 655→**97.0 нс**, 7 689→**56 Б**
      (~1100× к исходным 107 мкс).

## Фаза 3 — NCalc для простых выражений ✅

- [x] `SimpleExpressionClassifier` (Expressions): полный match текста token-regex'ом:
      корневые пути, строковые/числовые/bool/null-литералы, `+ - * %`, `== != > >= < <=`,
      `&& || !`, скобки, пробелы. Отклоняет: `/`, вызовы (`(` не после оператора/скобки),
      голые идентификаторы и статику (не корень), `? :`, NCalc-only `and`/`or`, интерполяцию.
      Много-сегментные пути-свойства (`msg.Payload.Length`) пропускает — оба движка резолвят
      их ОДНИМ `TryResolveRootPath`, паритет подтверждён тестом. Решение и конвертация
      кэшируются: `ConcurrentDictionary<string, ConvertedForm>` (текстов выражений конечное
      число — пишут люди); `Reject(text)` — после parse/eval-ошибки NCalc.
- [x] Конвертация: корневые пути → bare-идентификаторы (`msg.count` → `msg_count`), пути внутри
      строковых литералов не подменяются (token-сканер); значения — `TryResolveRootPath`,
      неразрешимый путь (null) → отказ и DE-путь (паритет ошибок). Кэш `NCalc.Expression` —
      в runner'е (объект мутируемый: `Parameters`). Опции: `StrictTypeMatching | OrdinalStringComparer`.
- [x] Fallback: parse/eval-ошибка NCalc → `Reject` + DE-путь (безопасно: тихое расхождение только
      `/`, он исключён классификатором).
- [x] Пакет `NCalc` 7.2.0 — ссылка в `Mars.Nodes.Expressions.csproj`.
- [x] Тесты: `SimpleExpressionNCalcTests` — классификатор (11 accepts / 10 rejects), конвертер
      (пути + литералы), parity-набор (порт таблицы A `SEMANTIC.md`: 14 выражений, результат
      runner'а == результату DE-пути), задокументированное расхождение `1 == true` → false
      (решение 5), fallback на DE после `Reject`. `Mars.Nodes.Tests` — 632/632.
- [x] Проверка: build slnx 0 errors + бенчмарк (прогон 6) — `ExpressionCurrentArithmetic`
      (`msg.count * 2 + 1`) = **342 нс / 1041 Б** против ~16.7 мкс DE-пути (~49×); hot path
      не деградировал (100 нс). Основная остаточная аллокация — `DynamicNodeMsgWrapper`
      в `TryResolveRootPath` (~840 Б); кандидат фазы 4.

## InlineFunctionNode (@expr-аргументы) — вариант A выполнен (2026-09-26), B открыт

- [x] **A (минимальный, без изменения семантики):** `CreateInterpreter` на сообщение заменён на
      сессию `RNS.Expressions(Node)` внутри локальной функции `BuildArgs()` — аренда только на
      цикл сборки аргументов, `DynamicInvoke` делегата (произвольный долгий код) — вне сессии.
      `ppt.Eval(text, param.ParameterType)` сохранён как есть (без BindRootPaths/NCalc/кэша).
      Выигрыш — снятие ~80 мкс CreateInterpreter; Eval по-прежнему компилирует на каждый вызов.
      `Mars.Nodes.Tests` — 649/649 без изменений.
- [ ] **B (полная унификация, обсудить):** @expr-аргументы через лестницу runner'а. Понадобится
      overload `Resolve(..., Type targetType)` (конверсия в произвольный CLR-тип параметра делегата
      вместо VarType-строки). Следствия: BindRootPaths заработает → LINQ в аргументах починится
      (сейчас `@msg.Payload.Count()` падает RuntimeBinderException — Eval без BindRootPaths);
      простые аргументы ускорятся до ~100–360 нс; НО ошибки обернутся в `NodeExecuteException`
      (другие тексты) и ранее падавшие выражения начнут работать — изменение поведения для flows.
- Вариант C (кэш typed-Lambda в текущем механизме) — не делать: поглощается B.

## Фаза 4 — DE Lambda-кэш + C#-деление в NCalc ✅ (закоммичена — d5399319)

- [x] **DE Lambda-кэш** (`ExpressionRunner.TryResolveDeCached`): `InputValueResolver.CollectRootPaths`
      (выделен из `BindRootPaths` — тот же обход с литеральными масками, но значения отдаёт списком,
      а не SetVariable); bound-текст парсится ОДИН РАЗ с объявленными параметрами по фактическим
      типам значений, кэш-ключ — bound-текст + типы (тип значения пути может меняться между
      сообщениями → отдельная Lambda); на сообщение только `lambda.Invoke(new Parameter(...))`.
      Не кэшируется (уходит полным Eval-путём): тексты с остаточными ссылками на
      `msg|GlobalContext|FlowContext|VarNode` (`ResidualRootRegex`) и тексты с Parse-ошибкой
      (`_lambdaNoCache`). Ошибки Invoke оборачиваются в `NodeExecuteException` с той же формулой
      сообщения, что `ResolveExpression`.
- [x] **C#-деление в NCalc** (`ExpressionRunner.CSharpIntegerDivision`, подписка на
      `Expression.EvaluateBinary`): `BinaryExpressionType.Div` (в 7.2.0 enum в корне `NCalc`,
      член `Div` — НЕ `Division`) с целыми операндами → целочисленное деление по промоции C#
      (long доминирует; uint/uint → uint; uint с остальными → long; прочее целое → int;
      ulong не обрабатывается — в типах VarNode его нет). Классификатор теперь пропускает `/`.
      DivideByZero → Reject → DE (каноническая ошибка).
- [x] **`NoStringTypeCoercion`** в опциях NCalc (см. решение 4): закрывает расхождение `"7" + 1`.
- [x] Тесты: `ExpressionRunnerLambdaCacheTests` (parity 7 сложных выражений со старым путём,
      свежесть значений между сообщениями на кэшированной Lambda, смена типа значения,
      residual-fallback, деление на ноль, Parse-ошибка); деление в parity-наборе
      (`2/4`→0, `7/2`→3, `msg.count/4*4`→40, `msg.ts/1000`→long, `7.0/2`→3.5, `-7%3`→-1).
      `Mars.Nodes.Tests` — 649/649.
- [x] Проверка: build slnx 0 errors; бенчмарк (прогон 7): `ExpressionCurrentComplex`
      (`msg.Payload.Count() + 1`) = **934 нс / 3008 Б** против ~16.7 мкс полного Eval (~18×);
      hot path и NCalc-ветки без регрессии.
- InlineFunctionNode (@expr-аргументы) — ПО-ПРЕЖНЕМУ не мигрирован (обсуждение отдельно).

## Грабли

- **Перепривязка переменных на сообщение обязательна**: RNS — новый объект на каждое сообщение
  (`NodeTaskJob.callbackNext`: `nextNode.RNS = CreateContextForNode(...)`), `msg` — тем более.
  Кэш интерпретатора без Rebind = stale msg.
- **Стареющие `msg_*`-переменные** в переиспользуемом Interpreter от `BindRootPaths`: корректность
  не ломается (неразрешившийся путь не подменяется в тексте → идёт dynamic-веткой; разрешившийся —
  перезаписывается), но переменные накапливаются; число ограничено distinct-путями выражений ноды.
- **Один runner арендуется одним воркером**; несколько одновременных `NodeTaskJob` одного flow →
  несколько runner'ов на ноду — это норма, мешок пула растёт до фактического параллелизма.
- **Приоритет свойств payload над Context** в `DynamicNodeMsgWrapper` (см. NodesReworkPlan «Грабли») —
  hot path фазы 2 для `msg.<key>` должен соблюдать ТОТ ЖЕ приоритет (сначала свойства NodeMsg/Payload,
  потом Context) — иначе расхождение с DE-путём.
- **NCalc-коэрция**: без `StrictTypeMatching` `'1' == 1` → true; с флагом → false (C# бросил бы
  исключение — closer). `ArithmeticNullOrEmptyStringAsZero` НЕ включаем (в C# null-арифметика — ошибка).
- Тестовые базы (`NodeServiceUnitTestBase`) мокают `IServiceProvider` через NSubstitute —
  `GetService(ExpressionRunnerPool)` вернёт null: fallback-ветка должна быть полноценной.
- **Кэшированная Lambda DynamicExpresso захватывает переменные интерпретатора КОНСТАНТАМИ на
  момент Parse** (делегат печётся по `UsedParameters`; `Invoke()` без явных параметров подставляет
  значения времени парсинга) — поэтому Lambda-кэш применим только к bound-текстам БЕЗ остаточных
  ссылок на `msg`/контексты (`ResidualRootRegex`), всё зависящее от сообщения передаётся
  явными параметрами на Invoke. `env` — исключение: статичная lambda, ставится один раз.
- **NCalc коэрцит строку в число по умолчанию**: `"7" + 1` → `8.0` (C#: `"71"`) — второй тихий
  класс после деления; закрыт `NoStringTypeCoercion` ( mixed string+number → ошибка NCalc →
  DE-fallback). `StringConcat` НЕ подходит: он превращает в конкатенацию ЛЮБОЙ `+` (`1+2`→"12").
- Бенчмарк-проект в slnx исключён из Debug-сборки решения (`Build Solution="Debug|*" Project="false"`);
  запуск — из bin после `dotnet build -c Release`.

## Инварианты

- Поведение DE-пути (семантика, тексты ошибок `NodeExecuteException`) не меняется ни в одной фазе —
  hot path и NCalc обязаны давать тот же результат, что DE для проходимых ими выражений
  (parity-тесты фаз 2–3).
- Классификатор консервативен: всё, что не match'ится ПОЛНОСТЬЮ, — в DE. Новое разрешение
  (например `/` в фазе 4) — только с parity-тестами.
- `ExpressionSession` — единственная точка входа impl'ов в резолвинг полей (кроме legacy
  `InputValueResolver` для немигрированных потребителей: InlineFunction, NodeService).

## Отклонено

- **Кэш Interpreter/Lambda в полях impl** — отклонён пользователем («технический код, хранить поля
  для этого в Impl не очень»); пул-сервис вместо него.
- **Try-parse классификация** (пробуем NCalc-парсер, упало → DE) — тихие семантические расхождения
  на валидных-в-обоих выражениях (деление) неприемлемы; явный классификатор предсказуем.
- **Единый движок DynamicExpresso** (без NCalc) — параметризованная Lambda DE (195 нс) не достигает
  уровня NCalc (27 нс) на простых выражениях; замер `DynExpressoParamLambda`.
- **Лок на ноду вместо пула** — сериализовал бы до 10 воркеров `NodeTaskJob`; аренда дешевле.
- **`Evaluate<T>` / `StrictTypeMatching` как фикс деления** — не меняют семантику `/` (проверено
  по докам NCalc: конвертится только финальный результат).
