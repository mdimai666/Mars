namespace MonacoRoslynCompletionProvider.Providers;

public class MonacoSemantic
{
    public MonacoType FomrInt(int code)
    {
        return code switch
        {
            0 => MonacoType.Method,
            1 => MonacoType.Function,
            2 => MonacoType.Constructor,
            3 => MonacoType.Field,
            4 => MonacoType.Variable,
            5 => MonacoType.Class,
            6 => MonacoType.Struct,
            7 => MonacoType.Interface,
            8 => MonacoType.Module,
            9 => MonacoType.Property,
            10 => MonacoType.Event,
            11 => MonacoType.Operator,
            12 => MonacoType.Unit,
            13 => MonacoType.Value,
            14 => MonacoType.Constant,
            15 => MonacoType.Enum,
            16 => MonacoType.EnumMember,
            17 => MonacoType.Keyword,
            18 => MonacoType.Text,
            19 => MonacoType.Color,
            20 => MonacoType.File,
            21 => MonacoType.Reference,
            22 => MonacoType.Customcolor,
            23 => MonacoType.Folder,
            24 => MonacoType.TypeParameter,
            25 => MonacoType.User,
            26 => MonacoType.Issue,
            27 => MonacoType.Snippet,
            _ => throw new NotImplementedException()
        };
    }
}

public enum MonacoType : int
{
    Class = 5,
    Color = 19,
    Constant = 14,
    Constructor = 2,
    Customcolor = 22,
    Enum = 15,
    EnumMember = 16,
    Event = 10,
    Field = 3,
    File = 20,
    Folder = 23,
    Function = 1,
    Interface = 7,
    Issue = 26,
    Keyword = 17,
    Method = 0,
    Module = 8,
    Operator = 11,
    Property = 9,
    Reference = 21,
    Snippet = 27,
    Struct = 6,
    Text = 18,
    TypeParameter = 24,
    Unit = 12,
    User = 25,
    Value = 13,
    Variable = 4,
}
