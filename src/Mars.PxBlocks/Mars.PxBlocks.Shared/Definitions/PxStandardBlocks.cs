namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Определения стандартных блоков языка (логика, циклы, математика, текст,
/// переменные) в конвенции typeId <c>core.категория.имя</c>. Отдаются сервером
/// в каждое определение контекста — редактор не зависит от встроенных
/// определений Blockly. Имена мутаторов (controls_if_mutator и т.д.) — штатные
/// Blockly, зарегистрированы в blockly/blocks. Исполнение — стандартные листья
/// Runtime (Std*) и структурная семантика ядра PxInterpreter/PxParser
/// (константы PxCoreBlocks синхронизированы с этими typeId).
/// </summary>
public sealed class PxStandardBlocks : PxBlockSet
{
    private const string Logic = "#5b80a5";
    private const string Loops = "#5ba55b";
    private const string Math = "#5b67a5";
    private const string Text = "#5ba58c";
    private const string Variables = "#a55b80";

    public PxStandardBlocks()
    {
        AddLogic();
        AddLoops();
        AddMath();
        AddText();
        AddVariables();
    }

    private void AddLogic()
    {
        Add(PxMaster.Define("core.logic.if").Colour(Logic)
            .Tooltip("Если условие истинно, выполнить вложенные блоки (через шестерёнку добавляются «иначе если» и «иначе»)")
            .Message("если %1 то %2", PxMaster.Value("IF0", "Boolean"), PxMaster.Do("DO0"))
            .Mutator("controls_if_mutator"));

        Add(PxMaster.Define("core.logic.if_else").Colour(Logic)
            .Tooltip("Если условие истинно, выполнить первую ветку, иначе — вторую")
            .Message("если %1 то %2 иначе %3",
                PxMaster.Value("IF0", "Boolean"), PxMaster.Do("DO0"), PxMaster.Do("ELSE")));

        Add(PxMaster.Define("core.logic.compare").Output("Boolean").Colour(Logic)
            .Tooltip("Сравнить два значения; результат — истина или ложь")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A"),
                PxMaster.Dropdown("OP", ("=", "EQ"), ("≠", "NEQ"), ("<", "LT"), ("≤", "LTE"), (">", "GT"), ("≥", "GTE")),
                PxMaster.Value("B")));

        Add(PxMaster.Define("core.logic.operation").Output("Boolean").Colour(Logic)
            .Tooltip("Логическое «и» или «или»")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A", "Boolean"),
                PxMaster.Dropdown("OP", ("и", "AND"), ("или", "OR")),
                PxMaster.Value("B", "Boolean")));

        Add(PxMaster.Define("core.logic.negate").Output("Boolean").Colour(Logic)
            .Tooltip("Инвертировать значение: истина становится ложью и наоборот")
            .Message("не %1", PxMaster.Value("BOOL", "Boolean")));

        Add(PxMaster.Define("core.logic.boolean").Output("Boolean").Colour(Logic)
            .Tooltip("Логическая константа")
            .Message("%1", PxMaster.Dropdown("BOOL", ("истина", "TRUE"), ("ложь", "FALSE"))));

        Add(PxMaster.Define("core.logic.null").Output("Any").Colour(Logic)
            .Tooltip("Пустое значение (ничего)")
            .Message("пусто"));

        Add(PxMaster.Define("core.logic.ternary").Output("Any").Colour(Logic)
            .Tooltip("Если условие истинно — первое значение, иначе второе")
            .Inline()
            .Message("если %1 то %2 иначе %3",
                PxMaster.Value("IF", "Boolean"), PxMaster.Value("THEN"), PxMaster.Value("ELSE")));
    }

    private void AddLoops()
    {
        Add(PxMaster.Define("core.loops.repeat").Colour(Loops)
            .Tooltip("Повторить вложенные блоки заданное число раз")
            .Message("повторить %1 раз %2", PxMaster.Value("TIMES", "Number"), PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.while_until").Colour(Loops)
            .Tooltip("Повторять, пока условие истинно (или пока не станет истинным)")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Dropdown("MODE", ("повторять пока", "WHILE"), ("повторять до", "UNTIL")),
                PxMaster.Value("BOOL", "Boolean"),
                PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.for").Colour(Loops)
            .Tooltip("Цикл по переменной от начального значения до конечного с шагом")
            .Inline()
            .Message("цикл %1 от %2 до %3 с шагом %4 %5",
                PxMaster.Variable("VAR"),
                PxMaster.Value("FROM", "Number"), PxMaster.Value("TO", "Number"), PxMaster.Value("BY", "Number"),
                PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.for_each").Colour(Loops)
            .Tooltip("Для каждого элемента списка выполнить вложенные блоки")
            .Inline()
            .Message("для каждого %1 в списке %2 %3",
                PxMaster.Variable("VAR"), PxMaster.Value("LIST"), PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.flow").Colour(Loops)
            .Tooltip("Выйти из цикла или перейти к следующей итерации")
            .Message("%1", PxMaster.Dropdown("FLOW", ("выйти из цикла", "BREAK"), ("следующая итерация", "CONTINUE"))));
    }

    private void AddMath()
    {
        Add(PxMaster.Define("core.math.number").Output("Number").Colour(Math)
            .Tooltip("Число")
            .Message("%1", PxMaster.Number("NUM")));

        Add(PxMaster.Define("core.math.arithmetic").Output("Number").Colour(Math)
            .Tooltip("Арифметическая операция над двумя числами")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A", "Number"),
                PxMaster.Dropdown("OP", ("+", "ADD"), ("−", "MINUS"), ("×", "MULTIPLY"), ("÷", "DIVIDE"), ("^", "POWER")),
                PxMaster.Value("B", "Number")));

        Add(PxMaster.Define("core.math.single").Output("Number").Colour(Math)
            .Tooltip("Функция одного числа: корень, модуль, логарифм и т.д.")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("√", "ROOT"), ("модуль", "ABS"), ("−", "-"), ("ln", "LN"), ("log₁₀", "LOG10"), ("e^", "EXP"), ("10^", "POW10")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.trig").Output("Number").Colour(Math)
            .Tooltip("Тригонометрическая функция (углы в градусах)")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("sin", "SIN"), ("cos", "COS"), ("tan", "TAN"), ("asin", "ASIN"), ("acos", "ACOS"), ("atan", "ATAN")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.constant").Output("Number").Colour(Math)
            .Tooltip("Математическая константа")
            .Message("%1", PxMaster.Dropdown("CONSTANT",
                ("π", "PI"), ("e", "E"), ("φ", "GOLDEN_RATIO"), ("√2", "SQRT2"), ("√½", "SQRT1_2"), ("∞", "INFINITY"))));

        Add(PxMaster.Define("core.math.number_property").Output("Boolean").Colour(Math)
            .Tooltip("Проверить свойство числа (для «делится на» шестерёнка добавляет делитель)")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("PROPERTY",
                    ("чётное", "EVEN"), ("нечётное", "ODD"), ("простое", "PRIME"), ("целое", "WHOLE"),
                    ("> 0", "POSITIVE"), ("< 0", "NEGATIVE"), ("делится на", "DIVISIBLE_BY")),
                PxMaster.Value("NUMBER_TO_CHECK", "Number"))
            .Mutator("math_is_divisibleby_mutator"));

        Add(PxMaster.Define("core.math.round").Output("Number").Colour(Math)
            .Tooltip("Округлить число")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("округлить", "ROUND"), ("округлить вверх", "ROUNDUP"), ("округлить вниз", "ROUNDDOWN")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.modulo").Output("Number").Colour(Math)
            .Tooltip("Остаток от деления первого числа на второе")
            .Inline()
            .Message("остаток от %1 ÷ %2", PxMaster.Value("DIVIDEND", "Number"), PxMaster.Value("DIVISOR", "Number")));

        Add(PxMaster.Define("core.math.random_int").Output("Number").Colour(Math)
            .Tooltip("Случайное целое число в границах включительно")
            .Inline()
            .Message("случайное целое от %1 до %2", PxMaster.Value("FROM", "Number"), PxMaster.Value("TO", "Number")));

        Add(PxMaster.Define("core.math.random_float").Output("Number").Colour(Math)
            .Tooltip("Случайное число от 0 (включительно) до 1 (не включая)")
            .Message("случайное число от 0 до 1"));
    }

    private void AddText()
    {
        Add(PxMaster.Define("core.text.text").Output("String").Colour(Text)
            .Tooltip("Текст (строка)")
            .Message("«%1»", PxMaster.Text("TEXT")));

        // Как в Blockly: пустое сообщение — входы ADD0..ADDN (и заголовок) строит
        // хелпер мутатора при создании блока; базовые входы в JSON ломали бы состояние itemCount_.
        Add(PxMaster.Define("core.text.join").Output("String").Colour(Text)
            .Tooltip("Склеить несколько значений в один текст (число элементов — через шестерёнку)")
            .Message("")
            .Mutator("text_join_mutator"));

        Add(PxMaster.Define("core.text.append").Colour(Text)
            .Tooltip("Дописать текст к переменной")
            .Inline()
            .Message("к переменной %1 добавить текст %2", PxMaster.Variable("VAR"), PxMaster.Value("TEXT", "String")));

        Add(PxMaster.Define("core.text.length").Output("Number").Colour(Text)
            .Tooltip("Число символов в тексте")
            .Message("длина %1", PxMaster.Value("VALUE", "String")));

        Add(PxMaster.Define("core.text.is_empty").Output("Boolean").Colour(Text)
            .Tooltip("Истина, если текст пустой")
            .Message("%1 — пустой текст?", PxMaster.Value("VALUE", "String")));

        Add(PxMaster.Define("core.text.index_of").Output("Number").Colour(Text)
            .Tooltip("Позиция вхождения текста (1 — первое; 0 — не найдено)")
            .Inline()
            .Message("в тексте %1 найти %2 вхождение %3",
                PxMaster.Value("VALUE", "String"),
                PxMaster.Dropdown("END", ("первое", "FIRST"), ("последнее", "LAST")),
                PxMaster.Value("FIND", "String")));

        Add(PxMaster.Define("core.text.char_at").Output("String").Colour(Text)
            .Tooltip("Взять символ из текста (для «с номера» шестерёнка добавляет номер)")
            .Inline()
            .Message("в тексте %1 взять букву %2 %3",
                PxMaster.Value("VALUE", "String"),
                PxMaster.Dropdown("WHERE", ("с начала №", "FROM_START"), ("с конца №", "FROM_END"), ("первую", "FIRST"), ("последнюю", "LAST"), ("случайную", "RANDOM")),
                PxMaster.Value("AT", "Number"))
            .Mutator("text_charAt_mutator"));

        Add(PxMaster.Define("core.text.change_case").Output("String").Colour(Text)
            .Tooltip("Изменить регистр текста")
            .Inline()
            .Message("изменить регистр %1 на %2",
                PxMaster.Value("TEXT", "String"),
                PxMaster.Dropdown("CASE", ("ЗАГЛАВНЫЕ", "UPPERCASE"), ("строчные", "LOWERCASE"), ("Каждое Слово", "TITLECASE"))));

        Add(PxMaster.Define("core.text.trim").Output("String").Colour(Text)
            .Tooltip("Убрать пробельные символы по краям текста")
            .Inline()
            .Message("обрезать пробелы %1 %2",
                PxMaster.Dropdown("MODE", ("с обеих сторон", "BOTH"), ("слева", "LEFT"), ("справа", "RIGHT")),
                PxMaster.Value("TEXT", "String")));

        Add(PxMaster.Define("core.text.print").Colour(Text)
            .Tooltip("Вывести значение в консоль (панель вывода)")
            .Message("вывести %1", PxMaster.Value("TEXT")));
    }

    private void AddVariables()
    {
        Add(PxMaster.Define("core.variables.get").Output("Any").Colour(Variables)
            .Tooltip("Значение переменной")
            .Message("%1", PxMaster.Variable("VAR")));

        Add(PxMaster.Define("core.variables.set").Colour(Variables)
            .Tooltip("Присвоить переменной значение")
            .Message("присвоить %1 значение %2", PxMaster.Variable("VAR"), PxMaster.Value("VALUE")));
    }
}
