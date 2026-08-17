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

    public PxStandardBlocks()
    {
        AddLogic();
        AddLoops();
        AddMath();
        AddText();
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

        Add(PxMaster.Define("core.text.print").Colour(Text)
            .Tooltip("Print the value to the output panel.")
            .Message("print %1", PxMaster.Value("TEXT")));
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
    }
}
