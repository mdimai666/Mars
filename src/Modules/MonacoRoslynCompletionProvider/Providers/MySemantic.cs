using Microsoft.CodeAnalysis.Classification;

namespace MonacoRoslynCompletionProvider.Providers;

public class MySemantic
{
    public enum SemanticHighlightClassification
    {
        Comment,
        ExcludedCode,
        Identifier,
        Keyword,
        ControlKeyword,
        NumericLiteral,
        Operator,
        OperatorOverloaded,
        PreprocessorKeyword,
        StringLiteral,
        WhiteSpace,
        Text,
        StaticSymbol,
        PreprocessorText,
        Punctuation,
        VerbatimStringLiteral,
        StringEscapeCharacter,
        ClassName,
        DelegateName,
        EnumName,
        InterfaceName,
        ModuleName,
        StructName,
        TypeParameterName,
        FieldName,
        EnumMemberName,
        ConstantName,
        LocalName,
        ParameterName,
        MethodName,
        ExtensionMethodName,
        PropertyName,
        EventName,
        NamespaceName,
        LabelName,
        XmlDocCommentAttributeName,
        XmlDocCommentAttributeQuotes,
        XmlDocCommentAttributeValue,
        XmlDocCommentCDataSection,
        XmlDocCommentComment,
        XmlDocCommentDelimiter,
        XmlDocCommentEntityReference,
        XmlDocCommentName,
        XmlDocCommentProcessingInstruction,
        XmlDocCommentText,
        XmlLiteralAttributeName,
        XmlLiteralAttributeQuotes,
        XmlLiteralAttributeValue,
        XmlLiteralCDataSection,
        XmlLiteralComment,
        XmlLiteralDelimiter,
        XmlLiteralEmbeddedExpression,
        XmlLiteralEntityReference,
        XmlLiteralName,
        XmlLiteralProcessingInstruction,
        XmlLiteralText,
        RegexComment,
        RegexCharacterClass,
        RegexAnchor,
        RegexQuantifier,
        RegexGrouping,
        RegexAlternation,
        RegexText,
        RegexSelfEscapedCharacter,
        RegexOtherEscape,
    }

    //internal static readonly ImmutableDictionary<SemanticHighlightClassification, SemanticTokenType> _coreTokenMap =
    //new Dictionary<SemanticHighlightClassification, SemanticTokenType>()
    //{
    //    [SemanticHighlightClassification.Comment] = SemanticTokenType.Comment,
    //    [SemanticHighlightClassification.Keyword] = SemanticTokenType.Keyword,
    //    [SemanticHighlightClassification.NumericLiteral] = SemanticTokenType.Number,
    //    [SemanticHighlightClassification.Operator] = SemanticTokenType.Operator,
    //    [SemanticHighlightClassification.StringLiteral] = SemanticTokenType.String,
    //    [SemanticHighlightClassification.ClassName] = SemanticTokenType.Class,
    //    [SemanticHighlightClassification.StructName] = SemanticTokenType.Struct,
    //    [SemanticHighlightClassification.NamespaceName] = SemanticTokenType.Namespace,
    //    [SemanticHighlightClassification.EnumName] = SemanticTokenType.Enum,
    //    [SemanticHighlightClassification.InterfaceName] = SemanticTokenType.Interface,
    //    [SemanticHighlightClassification.TypeParameterName] = SemanticTokenType.TypeParameter,
    //    [SemanticHighlightClassification.ParameterName] = SemanticTokenType.Parameter,
    //    [SemanticHighlightClassification.LocalName] = SemanticTokenType.Variable,
    //    [SemanticHighlightClassification.PropertyName] = SemanticTokenType.Property,
    //    [SemanticHighlightClassification.MethodName] = SemanticTokenType.Method,
    //    [SemanticHighlightClassification.EnumMemberName] = SemanticTokenType.EnumMember,
    //    [SemanticHighlightClassification.EventName] = SemanticTokenType.Event,
    //    [SemanticHighlightClassification.PreprocessorKeyword] = SemanticTokenType.Macro,
    //    [SemanticHighlightClassification.LabelName] = SemanticTokenType.Label,
    //}.ToImmutableDictionary();

    ////https://github.com/OmniSharp/csharp-language-server-protocol/blob/d6dbb601e753094da074ebee98edb6554a50f2f6/src/Protocol/Features/Document/SemanticTokensFeature.cs#L593
    //[StringEnum]
    //public readonly partial struct SemanticTokenType
    //{
    //    public static SemanticTokenType Comment { get; } = new SemanticTokenType("comment");
    //    public static SemanticTokenType Keyword { get; } = new SemanticTokenType("keyword");
    //    public static SemanticTokenType String { get; } = new SemanticTokenType("string");
    //    public static SemanticTokenType Number { get; } = new SemanticTokenType("number");
    //    public static SemanticTokenType Regexp { get; } = new SemanticTokenType("regexp");
    //    public static SemanticTokenType Operator { get; } = new SemanticTokenType("operator");
    //    public static SemanticTokenType Namespace { get; } = new SemanticTokenType("namespace");
    //    public static SemanticTokenType Type { get; } = new SemanticTokenType("type");
    //    public static SemanticTokenType Struct { get; } = new SemanticTokenType("struct");
    //    public static SemanticTokenType Class { get; } = new SemanticTokenType("class");
    //    public static SemanticTokenType Interface { get; } = new SemanticTokenType("interface");
    //    public static SemanticTokenType Enum { get; } = new SemanticTokenType("enum");
    //    public static SemanticTokenType TypeParameter { get; } = new SemanticTokenType("typeParameter");
    //    public static SemanticTokenType Function { get; } = new SemanticTokenType("function");
    //    public static SemanticTokenType Method { get; } = new SemanticTokenType("method");
    //    public static SemanticTokenType Property { get; } = new SemanticTokenType("property");
    //    public static SemanticTokenType Macro { get; } = new SemanticTokenType("macro");
    //    public static SemanticTokenType Variable { get; } = new SemanticTokenType("variable");
    //    public static SemanticTokenType Parameter { get; } = new SemanticTokenType("parameter");
    //    public static SemanticTokenType Label { get; } = new SemanticTokenType("label");
    //    public static SemanticTokenType Modifier { get; } = new SemanticTokenType("modifier");
    //    public static SemanticTokenType Event { get; } = new SemanticTokenType("event");
    //    public static SemanticTokenType EnumMember { get; } = new SemanticTokenType("enumMember");
    //}

    //---
    public static readonly Dictionary<string, SemanticHighlightClassification> _classificationMap =
            new()
            {
                [ClassificationTypeNames.Comment] = SemanticHighlightClassification.Comment,
                [ClassificationTypeNames.ExcludedCode] = SemanticHighlightClassification.ExcludedCode,
                [ClassificationTypeNames.Identifier] = SemanticHighlightClassification.Identifier,
                [ClassificationTypeNames.Keyword] = SemanticHighlightClassification.Keyword,
                [ClassificationTypeNames.ControlKeyword] = SemanticHighlightClassification.ControlKeyword,
                [ClassificationTypeNames.NumericLiteral] = SemanticHighlightClassification.NumericLiteral,
                [ClassificationTypeNames.Operator] = SemanticHighlightClassification.Operator,
                [ClassificationTypeNames.OperatorOverloaded] = SemanticHighlightClassification.OperatorOverloaded,
                [ClassificationTypeNames.PreprocessorKeyword] = SemanticHighlightClassification.PreprocessorKeyword,
                [ClassificationTypeNames.StringLiteral] = SemanticHighlightClassification.StringLiteral,
                [ClassificationTypeNames.WhiteSpace] = SemanticHighlightClassification.WhiteSpace,
                [ClassificationTypeNames.Text] = SemanticHighlightClassification.Text,
                [ClassificationTypeNames.StaticSymbol] = SemanticHighlightClassification.StaticSymbol,
                [ClassificationTypeNames.PreprocessorText] = SemanticHighlightClassification.PreprocessorText,
                [ClassificationTypeNames.Punctuation] = SemanticHighlightClassification.Punctuation,
                [ClassificationTypeNames.VerbatimStringLiteral] = SemanticHighlightClassification.VerbatimStringLiteral,
                [ClassificationTypeNames.StringEscapeCharacter] = SemanticHighlightClassification.StringEscapeCharacter,
                [ClassificationTypeNames.ClassName] = SemanticHighlightClassification.ClassName,
                [ClassificationTypeNames.RecordClassName] = SemanticHighlightClassification.ClassName,
                [ClassificationTypeNames.DelegateName] = SemanticHighlightClassification.DelegateName,
                [ClassificationTypeNames.EnumName] = SemanticHighlightClassification.EnumName,
                [ClassificationTypeNames.InterfaceName] = SemanticHighlightClassification.InterfaceName,
                [ClassificationTypeNames.ModuleName] = SemanticHighlightClassification.ModuleName,
                [ClassificationTypeNames.StructName] = SemanticHighlightClassification.StructName,
                [ClassificationTypeNames.RecordStructName] = SemanticHighlightClassification.StructName,
                [ClassificationTypeNames.TypeParameterName] = SemanticHighlightClassification.TypeParameterName,
                [ClassificationTypeNames.FieldName] = SemanticHighlightClassification.FieldName,
                [ClassificationTypeNames.EnumMemberName] = SemanticHighlightClassification.EnumMemberName,
                [ClassificationTypeNames.ConstantName] = SemanticHighlightClassification.ConstantName,
                [ClassificationTypeNames.LocalName] = SemanticHighlightClassification.LocalName,
                [ClassificationTypeNames.ParameterName] = SemanticHighlightClassification.ParameterName,
                [ClassificationTypeNames.MethodName] = SemanticHighlightClassification.MethodName,
                [ClassificationTypeNames.ExtensionMethodName] = SemanticHighlightClassification.ExtensionMethodName,
                [ClassificationTypeNames.PropertyName] = SemanticHighlightClassification.PropertyName,
                [ClassificationTypeNames.EventName] = SemanticHighlightClassification.EventName,
                [ClassificationTypeNames.NamespaceName] = SemanticHighlightClassification.NamespaceName,
                [ClassificationTypeNames.LabelName] = SemanticHighlightClassification.LabelName,
                [ClassificationTypeNames.XmlDocCommentAttributeName] = SemanticHighlightClassification.XmlDocCommentAttributeName,
                [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = SemanticHighlightClassification.XmlDocCommentAttributeQuotes,
                [ClassificationTypeNames.XmlDocCommentAttributeValue] = SemanticHighlightClassification.XmlDocCommentAttributeValue,
                [ClassificationTypeNames.XmlDocCommentCDataSection] = SemanticHighlightClassification.XmlDocCommentCDataSection,
                [ClassificationTypeNames.XmlDocCommentComment] = SemanticHighlightClassification.XmlDocCommentComment,
                [ClassificationTypeNames.XmlDocCommentDelimiter] = SemanticHighlightClassification.XmlDocCommentDelimiter,
                [ClassificationTypeNames.XmlDocCommentEntityReference] = SemanticHighlightClassification.XmlDocCommentEntityReference,
                [ClassificationTypeNames.XmlDocCommentName] = SemanticHighlightClassification.XmlDocCommentName,
                [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = SemanticHighlightClassification.XmlDocCommentProcessingInstruction,
                [ClassificationTypeNames.XmlDocCommentText] = SemanticHighlightClassification.XmlDocCommentText,
                [ClassificationTypeNames.XmlLiteralAttributeName] = SemanticHighlightClassification.XmlLiteralAttributeName,
                [ClassificationTypeNames.XmlLiteralAttributeQuotes] = SemanticHighlightClassification.XmlLiteralAttributeQuotes,
                [ClassificationTypeNames.XmlLiteralAttributeValue] = SemanticHighlightClassification.XmlLiteralAttributeValue,
                [ClassificationTypeNames.XmlLiteralCDataSection] = SemanticHighlightClassification.XmlLiteralCDataSection,
                [ClassificationTypeNames.XmlLiteralComment] = SemanticHighlightClassification.XmlLiteralComment,
                [ClassificationTypeNames.XmlLiteralDelimiter] = SemanticHighlightClassification.XmlLiteralDelimiter,
                [ClassificationTypeNames.XmlLiteralEmbeddedExpression] = SemanticHighlightClassification.XmlLiteralEmbeddedExpression,
                [ClassificationTypeNames.XmlLiteralEntityReference] = SemanticHighlightClassification.XmlLiteralEntityReference,
                [ClassificationTypeNames.XmlLiteralName] = SemanticHighlightClassification.XmlLiteralName,
                [ClassificationTypeNames.XmlLiteralProcessingInstruction] = SemanticHighlightClassification.XmlLiteralProcessingInstruction,
                [ClassificationTypeNames.XmlLiteralText] = SemanticHighlightClassification.XmlLiteralText,
                [ClassificationTypeNames.RegexComment] = SemanticHighlightClassification.RegexComment,
                [ClassificationTypeNames.RegexCharacterClass] = SemanticHighlightClassification.RegexCharacterClass,
                [ClassificationTypeNames.RegexAnchor] = SemanticHighlightClassification.RegexAnchor,
                [ClassificationTypeNames.RegexQuantifier] = SemanticHighlightClassification.RegexQuantifier,
                [ClassificationTypeNames.RegexGrouping] = SemanticHighlightClassification.RegexGrouping,
                [ClassificationTypeNames.RegexAlternation] = SemanticHighlightClassification.RegexAlternation,
                [ClassificationTypeNames.RegexText] = SemanticHighlightClassification.RegexText,
                [ClassificationTypeNames.RegexSelfEscapedCharacter] = SemanticHighlightClassification.RegexSelfEscapedCharacter,
                [ClassificationTypeNames.RegexOtherEscape] = SemanticHighlightClassification.RegexOtherEscape,
            };

    public static readonly Dictionary<CompletionItemKind, MonacoType> itemkind_to_monaco = new()
    {
        [CompletionItemKind.Text] = MonacoType.Text,
        [CompletionItemKind.Method] = MonacoType.Method,
        [CompletionItemKind.Function] = MonacoType.Function,
        [CompletionItemKind.Constructor] = MonacoType.Constructor,
        [CompletionItemKind.Field] = MonacoType.Field,
        [CompletionItemKind.Variable] = MonacoType.Variable,
        [CompletionItemKind.Class] = MonacoType.Class,
        [CompletionItemKind.Interface] = MonacoType.Interface,
        [CompletionItemKind.Module] = MonacoType.Module,
        [CompletionItemKind.Property] = MonacoType.Property,
        [CompletionItemKind.Unit] = MonacoType.Unit,
        [CompletionItemKind.Value] = MonacoType.Value,
        [CompletionItemKind.Enum] = MonacoType.Enum,
        [CompletionItemKind.Keyword] = MonacoType.Keyword,
        [CompletionItemKind.Snippet] = MonacoType.Snippet,
        [CompletionItemKind.Color] = MonacoType.Color,
        [CompletionItemKind.File] = MonacoType.File,
        [CompletionItemKind.Reference] = MonacoType.Reference,
        [CompletionItemKind.Folder] = MonacoType.Folder,
        [CompletionItemKind.EnumMember] = MonacoType.EnumMember,
        [CompletionItemKind.Constant] = MonacoType.Constant,
        [CompletionItemKind.Struct] = MonacoType.Struct,
        [CompletionItemKind.Event] = MonacoType.Event,
        [CompletionItemKind.Operator] = MonacoType.Operator,
        [CompletionItemKind.TypeParameter] = MonacoType.TypeParameter,
    };

}
