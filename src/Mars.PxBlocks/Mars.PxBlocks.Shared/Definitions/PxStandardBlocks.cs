namespace Mars.PxBlocks.Shared.Definitions;

/// <summary>
/// Определения стандартных блоков языка (логика, циклы, математика, текст,
/// переменные) в конвенции typeId <c>core.категория.имя</c>. Отдаются сервером
/// в каждое определение контекста — редактор не зависит от встроенных
/// определений Blockly. Имена мутаторов (controls_if_mutator и т.д.) — штатные
/// Blockly, зарегистрированы в blockly/blocks. Исполнение — стандартные листья
/// Runtime (Std*) и структурная семантика ядра PxInterpreter/PxParser
/// (константы PxCoreBlocks синхронизированы с этими typeId).
/// Формулировки сообщений — стандартные английские Blockly/MakeCode.
/// </summary>
public sealed class PxStandardBlocks : PxBlockSet
{
    private const string Logic = "#5b80a5";
    private const string Loops = "#5ba55b";
    private const string Math = "#5b67a5";
    private const string Text = "#5ba58c";
    private const string Variables = "#a55b80";
    // Цвет штатных процедурных блоков Blockly (hue 290), чтобы return не отличался
    // от «to do something» во flyout «Functions»; цвет рейки — свой у категории.
    private const string Functions = "#995ba5";
    // Цвет штатных lists-блоков Blockly (hue 260) — свои массивные блоки не должны
    // отличаться от встроенных create/length в той же категории.
    private const string Arrays = "#745ba6";

    public PxStandardBlocks()
    {
        AddLogic();
        AddLoops();
        AddMath();
        AddText();
        AddArrays();
        AddVariables();
        AddFunctions();
    }

    private void AddLogic()
    {
        Add(PxMaster.Define("core.logic.if").Colour(Logic)
            .Tooltip("If a value is true, then do some statements (else-if and else are added via the gear)")
            .Message("if %1 do %2", PxMaster.Value("IF0", "Boolean"), PxMaster.Do("DO0"))
            .Mutator("controls_if_mutator"));

        Add(PxMaster.Define("core.logic.if_else").Colour(Logic)
            .Tooltip("If a value is true, then do the first block of statements. Otherwise, do the second block of statements.")
            .Message("if %1 do %2 else %3",
                PxMaster.Value("IF0", "Boolean"), PxMaster.Do("DO0"), PxMaster.Do("ELSE")));

        Add(PxMaster.Define("core.logic.compare").Output("Boolean").Colour(Logic)
            .Tooltip("Compare two values; the result is true or false")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A"),
                PxMaster.Dropdown("OP", ("=", "EQ"), ("≠", "NEQ"), ("<", "LT"), ("≤", "LTE"), (">", "GT"), ("≥", "GTE")),
                PxMaster.Value("B")));

        Add(PxMaster.Define("core.logic.operation").Output("Boolean").Colour(Logic)
            .Tooltip("Logical and / or")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A", "Boolean"),
                PxMaster.Dropdown("OP", ("and", "AND"), ("or", "OR")),
                PxMaster.Value("B", "Boolean")));

        Add(PxMaster.Define("core.logic.negate").Output("Boolean").Colour(Logic)
            .Tooltip("Returns true if the input is false. Returns false if the input is true.")
            .Message("not %1", PxMaster.Value("BOOL", "Boolean")));

        Add(PxMaster.Define("core.logic.boolean").Output("Boolean").Colour(Logic)
            .Tooltip("Returns true or false.")
            .Message("%1", PxMaster.Dropdown("BOOL", ("true", "TRUE"), ("false", "FALSE"))));

        Add(PxMaster.Define("core.logic.null").Output("Any").Colour(Logic)
            .Tooltip("Returns an empty value.")
            .Message("null"));

        Add(PxMaster.Define("core.logic.ternary").Output("Any").Colour(Logic)
            .Tooltip("If the condition is true, returns the first value; otherwise returns the second one")
            .Inline()
            .Message("if %1 then %2 else %3",
                PxMaster.Value("IF", "Boolean"), PxMaster.Value("THEN"), PxMaster.Value("ELSE")));
    }

    private void AddLoops()
    {
        Add(PxMaster.Define("core.loops.repeat").Colour(Loops)
            .Tooltip("Do some statements several times.")
            .Message("repeat %1 times %2", PxMaster.Value("TIMES", "Number"), PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.while_until").Colour(Loops)
            .Tooltip("Repeat the statements while the condition is true (or until it becomes true)")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Dropdown("MODE", ("repeat while", "WHILE"), ("repeat until", "UNTIL")),
                PxMaster.Value("BOOL", "Boolean"),
                PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.for").Colour(Loops)
            .Tooltip("Count from a start number to an end number by the specified interval, running the statements for each value")
            .Inline()
            .Message("count with %1 from %2 to %3 by %4 %5",
                PxMaster.Variable("VAR"),
                PxMaster.Value("FROM", "Number"), PxMaster.Value("TO", "Number"), PxMaster.Value("BY", "Number"),
                PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.for_each").Colour(Loops)
            .Tooltip("For each item in a list, set the item to the variable and then do some statements")
            .Inline()
            .Message("for each item %1 in list %2 %3",
                PxMaster.Variable("VAR"), PxMaster.Value("LIST"), PxMaster.Do("DO")));

        Add(PxMaster.Define("core.loops.flow").Colour(Loops)
            .Tooltip("Break out of the containing loop, or continue with the next iteration")
            .Message("%1", PxMaster.Dropdown("FLOW", ("break out", "BREAK"), ("continue with next iteration", "CONTINUE"))));

        Add(PxMaster.Define("core.loops.pause").Colour(Loops)
            .Tooltip("Pause for the given number of milliseconds")
            .Inline()
            .Message("wait %1 ms", PxMaster.Value("MS", "Number")));
    }

    private void AddMath()
    {
        Add(PxMaster.Define("core.math.number").Output("Number").Colour(Math)
            .Tooltip("A number.")
            .Message("%1", PxMaster.Number("NUM")));

        Add(PxMaster.Define("core.math.arithmetic").Output("Number").Colour(Math)
            .Tooltip("Do arithmetic on two numbers")
            .Inline()
            .Message("%1 %2 %3",
                PxMaster.Value("A", "Number"),
                PxMaster.Dropdown("OP", ("+", "ADD"), ("−", "MINUS"), ("×", "MULTIPLY"), ("÷", "DIVIDE"), ("^", "POWER")),
                PxMaster.Value("B", "Number")));

        Add(PxMaster.Define("core.math.single").Output("Number").Colour(Math)
            .Tooltip("Apply a function of one number: root, absolute, logarithm, etc.")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("√", "ROOT"), ("abs", "ABS"), ("−", "-"), ("ln", "LN"), ("log₁₀", "LOG10"), ("e^", "EXP"), ("10^", "POW10")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.trig").Output("Number").Colour(Math)
            .Tooltip("Return the trigonometric function of an angle in degrees")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("sin", "SIN"), ("cos", "COS"), ("tan", "TAN"), ("asin", "ASIN"), ("acos", "ACOS"), ("atan", "ATAN")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.constant").Output("Number").Colour(Math)
            .Tooltip("Return one of the common constants: π, e, φ, √2, √½, ∞")
            .Message("%1", PxMaster.Dropdown("CONSTANT",
                ("π", "PI"), ("e", "E"), ("φ", "GOLDEN_RATIO"), ("√2", "SQRT2"), ("√½", "SQRT1_2"), ("∞", "INFINITY"))));

        Add(PxMaster.Define("core.math.number_property").Output("Boolean").Colour(Math)
            .Tooltip("Check if a number is even, odd, prime, whole, positive, negative, or divisible by a certain number (the divisor is added via the gear)")
            .Inline()
            .Message("%1 is %2",
                PxMaster.Value("NUMBER_TO_CHECK", "Number"),
                PxMaster.Dropdown("PROPERTY",
                    ("even", "EVEN"), ("odd", "ODD"), ("prime", "PRIME"), ("whole", "WHOLE"),
                    ("> 0", "POSITIVE"), ("< 0", "NEGATIVE"), ("divisible by", "DIVISIBLE_BY")))
            .Mutator("math_is_divisibleby_mutator"));

        Add(PxMaster.Define("core.math.round").Output("Number").Colour(Math)
            .Tooltip("Round a number up or down.")
            .Inline()
            .Message("%1 %2",
                PxMaster.Dropdown("OP", ("round", "ROUND"), ("round up", "ROUNDUP"), ("round down", "ROUNDDOWN")),
                PxMaster.Value("NUM", "Number")));

        Add(PxMaster.Define("core.math.modulo").Output("Number").Colour(Math)
            .Tooltip("Return the remainder from a division of the two numbers.")
            .Inline()
            .Message("remainder of %1 ÷ %2", PxMaster.Value("DIVIDEND", "Number"), PxMaster.Value("DIVISOR", "Number")));

        Add(PxMaster.Define("core.math.random_int").Output("Number").Colour(Math)
            .Tooltip("Return a random integer between the two specified limits, inclusive.")
            .Inline()
            .Message("pick random %1 to %2", PxMaster.Value("FROM", "Number"), PxMaster.Value("TO", "Number")));

        Add(PxMaster.Define("core.math.random_float").Output("Number").Colour(Math)
            .Tooltip("Return a random number between 0.0 (inclusive) and 1.0 (exclusive).")
            .Message("random fraction"));

        Add(PxMaster.Define("core.math.min_max").Output("Number").Colour(Math)
            .Tooltip("Return the smaller or the larger of the two numbers")
            .Inline()
            .Message("%1 of %2 and %3",
                PxMaster.Dropdown("OP", ("min", "MIN"), ("max", "MAX")),
                PxMaster.Value("A", "Number"), PxMaster.Value("B", "Number")));

        Add(PxMaster.Define("core.math.map").Output("Number").Colour(Math)
            .Tooltip("Re-map a number from one range to another: from low/high is converted to to low/high proportionally")
            .Inline()
            .Message("map %1 from low %2 high %3 to low %4 high %5",
                PxMaster.Value("VALUE", "Number"),
                PxMaster.Value("FROM_LOW", "Number"), PxMaster.Value("FROM_HIGH", "Number"),
                PxMaster.Value("TO_LOW", "Number"), PxMaster.Value("TO_HIGH", "Number")));
    }

    private void AddText()
    {
        Add(PxMaster.Define("core.text.text").Output("String").Colour(Text)
            .Tooltip("A letter, word, or line of text.")
            .Message("\"%1\"", PxMaster.Text("TEXT")));

        // Как в Blockly: пустое сообщение — входы ADD0..ADDN (и заголовок) строит
        // хелпер мутатора при создании блока; базовые входы в JSON ломали бы состояние itemCount_.
        Add(PxMaster.Define("core.text.join").Output("String").Colour(Text)
            .Tooltip("Merge several values into one piece of text (the number of items is set via the gear)")
            .Message("")
            .Mutator("text_join_mutator"));

        Add(PxMaster.Define("core.text.append").Colour(Text)
            .Tooltip("Append some text to a variable.")
            .Inline()
            .Message("to %1 append text %2", PxMaster.Variable("VAR"), PxMaster.Value("TEXT", "String")));

        Add(PxMaster.Define("core.text.length").Output("Number").Colour(Text)
            .Tooltip("Returns the number of letters in the provided text.")
            .Message("length of %1", PxMaster.Value("VALUE", "String")));

        Add(PxMaster.Define("core.text.is_empty").Output("Boolean").Colour(Text)
            .Tooltip("Returns true if the provided text is empty.")
            .Message("%1 is empty", PxMaster.Value("VALUE", "String")));

        Add(PxMaster.Define("core.text.index_of").Output("Number").Colour(Text)
            .Tooltip("Returns the index of the first/last occurrence of the text (1 is the first; 0 if not found)")
            .Inline()
            .Message("in text %1 find %2 occurrence of text %3",
                PxMaster.Value("VALUE", "String"),
                PxMaster.Dropdown("END", ("first", "FIRST"), ("last", "LAST")),
                PxMaster.Value("FIND", "String")));

        Add(PxMaster.Define("core.text.char_at").Output("String").Colour(Text)
            .Tooltip("Returns the letter at the specified position (the gear adds the position number)")
            .Inline()
            .Message("in text %1 get %2 %3",
                PxMaster.Value("VALUE", "String"),
                PxMaster.Dropdown("WHERE", ("letter #", "FROM_START"), ("letter # from end", "FROM_END"), ("first letter", "FIRST"), ("last letter", "LAST"), ("random letter", "RANDOM")),
                PxMaster.Value("AT", "Number"))
            .Mutator("text_charAt_mutator"));

        Add(PxMaster.Define("core.text.change_case").Output("String").Colour(Text)
            .Tooltip("Return a copy of the text in a different case.")
            .Inline()
            .Message("change case %1 to %2",
                PxMaster.Value("TEXT", "String"),
                PxMaster.Dropdown("CASE", ("UPPERCASE", "UPPERCASE"), ("lowercase", "LOWERCASE"), ("Title Case", "TITLECASE"))));

        Add(PxMaster.Define("core.text.trim").Output("String").Colour(Text)
            .Tooltip("Return a copy of the text with spaces removed from one or both ends.")
            .Inline()
            .Message("trim spaces %1 of %2",
                PxMaster.Dropdown("MODE", ("from both sides", "BOTH"), ("from the left", "LEFT"), ("from the right", "RIGHT")),
                PxMaster.Value("TEXT", "String")));

        // Расширения текста — блоки MakeCode (libs/pxt-common/pxt-core.d.ts),
        // семантика 0-основная, как в MakeCode.
        Add(PxMaster.Define("core.text.substring").Output("String").Colour(Text)
            .Tooltip("Returns part of the text: from the given position (0 is the first, negative counts from the end) for the given number of characters; length 0 means to the end, negative gives an empty text")
            .Inline()
            .Message("substring of %1 from %2 of length %3",
                PxMaster.Value("VALUE", "String"), PxMaster.Value("START", "Number"), PxMaster.Value("LENGTH", "Number")));

        Add(PxMaster.Define("core.text.includes").Output("Boolean").Colour(Text)
            .Tooltip("Returns true if the text contains the given text")
            .Inline()
            .Message("%1 includes %2", PxMaster.Value("VALUE", "String"), PxMaster.Value("FIND", "String")));

        Add(PxMaster.Define("core.text.compare").Output("Number").Colour(Text)
            .Tooltip("Compares two texts in character order (ASCII): -1 if the first comes before the second, 0 if equal, 1 if after")
            .Inline()
            .Message("compare %1 to %2", PxMaster.Value("A", "String"), PxMaster.Value("B", "String")));

        Add(PxMaster.Define("core.text.split").Output("Array").Colour(Text)
            .Tooltip("Splits the text into a list of parts at each occurrence of the separator (an empty separator splits into characters)")
            .Inline()
            .Message("split %1 at %2", PxMaster.Value("VALUE", "String"), PxMaster.Value("SEPARATOR", "String")));

        Add(PxMaster.Define("core.text.parse").Output("Number").Colour(Text)
            .Tooltip("Reads a number from the start of the text; returns NaN if the text does not begin with a number")
            .Inline()
            .Message("parse to number %1", PxMaster.Value("VALUE", "String")));

        Add(PxMaster.Define("core.text.char_code").Output("Number").Colour(Text)
            .Tooltip("Returns the code of the character at the given position (0 is the first); NaN if there is no character at the position")
            .Inline()
            .Message("char code from %1 at %2", PxMaster.Value("VALUE", "String"), PxMaster.Value("INDEX", "Number")));

        Add(PxMaster.Define("core.text.print").Colour(Text)
            .Tooltip("Print the value to the output panel.")
            .Message("print %1", PxMaster.Value("TEXT")));
    }

    /// <summary>
    /// Массивы — набор MakeCode с 0-индексацией (Этап 14B). create_empty/create_with/
    /// repeat/length — встроенные блоки Blockly с лейблами MakeCode (Msg-оверрайды
    /// в JsSrc/index.ts, у create_with мутатор задаётся в init); здесь — остальные:
    /// get/set по индексу и поиск индекса (аналоги lists_index_get/lists_index_set/
    /// array_indexof из libs/pxt-common).
    /// </summary>
    private void AddArrays()
    {
        Add(PxMaster.Define("lists_index_get").Output("Any").Colour(Arrays)
            .Tooltip("Returns the value at the given index in an array (0 is the first; null if there is no value at the index)")
            .Inline()
            .Message("%1 get value at %2", PxMaster.Value("LIST", "Array"), PxMaster.Value("INDEX", "Number")));

        Add(PxMaster.Define("lists_index_set").Colour(Arrays)
            .Tooltip("Sets the value at the given index in an array (0 is the first; the array grows if the index is beyond the end)")
            .Inline()
            .Message("%1 set value at %2 to %3",
                PxMaster.Value("LIST", "Array"), PxMaster.Value("INDEX", "Number"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("array_indexof").Output("Number").Colour(Arrays)
            .Tooltip("Returns the index of the first occurrence of a value in an array (0 is the first; -1 if not found)")
            .Inline()
            .Message("%1 find index of %2", PxMaster.Value("LIST", "Array"), PxMaster.Value("VALUE")));

        // Остальные блоки массивов MakeCode (pxt-common/pxt-core.d.ts): изменение
        // мутирует список по ссылке; get-варианты возвращают значение (null на пустом).
        Add(PxMaster.Define("array_push").Colour(Arrays)
            .Tooltip("Append a new element to the end of an array")
            .Inline()
            .Message("%1 add value %2 to end", PxMaster.Value("LIST", "Array"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("array_pop").Output("Any").Colour(Arrays)
            .Tooltip("Remove the last element from an array and return it (null if the array is empty)")
            .Inline()
            .Message("get and remove last value from %1", PxMaster.Value("LIST", "Array")));

        Add(PxMaster.Define("array_pop_statement").Colour(Arrays)
            .Tooltip("Remove the last element from an array")
            .Inline()
            .Message("remove last value from %1", PxMaster.Value("LIST", "Array")));

        Add(PxMaster.Define("array_shift").Output("Any").Colour(Arrays)
            .Tooltip("Remove the first element from an array and return it (null if the array is empty)")
            .Inline()
            .Message("get and remove first value from %1", PxMaster.Value("LIST", "Array")));

        Add(PxMaster.Define("array_shift_statement").Colour(Arrays)
            .Tooltip("Remove the first element from an array")
            .Inline()
            .Message("remove first value from %1", PxMaster.Value("LIST", "Array")));

        Add(PxMaster.Define("array_unshift").Output("Number").Colour(Arrays)
            .Tooltip("Add one element to the beginning of an array and return the new length of the array")
            .Inline()
            .Message("%1 insert %2 at beginning", PxMaster.Value("LIST", "Array"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("array_unshift_statement").Colour(Arrays)
            .Tooltip("Add one element to the beginning of an array")
            .Inline()
            .Message("%1 insert %2 at beginning", PxMaster.Value("LIST", "Array"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("array_removeat").Output("Any").Colour(Arrays)
            .Tooltip("Remove and return the element at a certain index (null if there is no value at the index)")
            .Inline()
            .Message("%1 get and remove value at %2", PxMaster.Value("LIST", "Array"), PxMaster.Value("INDEX", "Number")));

        Add(PxMaster.Define("array_removeat_statement").Colour(Arrays)
            .Tooltip("Remove the element at a certain index")
            .Inline()
            .Message("%1 remove value at %2", PxMaster.Value("LIST", "Array"), PxMaster.Value("INDEX", "Number")));

        Add(PxMaster.Define("array_insertAt").Colour(Arrays)
            .Tooltip("Insert the value at a particular index, increases length by 1 (an index beyond the end appends)")
            .Inline()
            .Message("%1 insert at %2 value %3",
                PxMaster.Value("LIST", "Array"), PxMaster.Value("INDEX", "Number"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("array_pickRandom").Output("Any").Colour(Arrays)
            .Tooltip("Return a random value from the array (null if the array is empty)")
            .Inline()
            .Message("get random value from %1", PxMaster.Value("LIST", "Array")));

        Add(PxMaster.Define("array_reverse").Colour(Arrays)
            .Tooltip("Reverse the elements in an array: the first element becomes the last, and the last becomes the first")
            .Inline()
            .Message("reverse %1", PxMaster.Value("LIST", "Array")));
    }

    private void AddVariables()
    {
        Add(PxMaster.Define("core.variables.get").Output("Any").Colour(Variables)
            .Tooltip("Returns the value of this variable.")
            .Message("%1", PxMaster.Variable("VAR")));

        Add(PxMaster.Define("core.variables.set").Colour(Variables)
            .Tooltip("Sets this variable to be equal to the input.")
            .Message("set %1 to %2", PxMaster.Variable("VAR"), PxMaster.Value("VALUE")));

        Add(PxMaster.Define("core.variables.change").Colour(Variables)
            .Tooltip("Change this variable by the given amount.")
            .Inline()
            .Message("change %1 by %2", PxMaster.Variable("VAR"), PxMaster.Value("DELTA", "Number")));
    }

    /// <summary>
    /// Процедуры — Blockly-имена (фаза 2): определения/call-блоки — штатные Blockly,
    /// здесь только досрочный return, которого в штатном наборе нет (аналог
    /// function_return в PXT). Показывается во flyout «Functions» своим колбэком
    /// PROCEDURE в JsSrc/index.ts.
    /// </summary>
    private void AddFunctions()
    {
        Add(PxMaster.Define("procedures_return").Colour(Functions)
            .Tooltip("Exit the current function early, optionally returning a value to the caller")
            .Inline()
            .Message("return %1", PxMaster.Value("VALUE")));

        // «if … return …» для функций MakeCode: штатный procedures_ifreturn нельзя
        // использовать внутри function_definition (warning + disable вне
        // procedures_def*), поэтому свой блок с той же формой и семантикой.
        Add(PxMaster.Define("core.functions.if_return").Colour(Functions)
            .Tooltip("If the value is true, return the value from the function and stop it")
            .Inline()
            .Message("if %1 return %2",
                PxMaster.Value("CONDITION", "Boolean"), PxMaster.Value("VALUE")));
    }
}
