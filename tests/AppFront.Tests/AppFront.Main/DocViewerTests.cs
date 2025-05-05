using System.Linq.Expressions;
using System.Reflection;
using AppFront.Shared.Components;
using AppFront.Tests.Utils;
using Mars.Core.Attributes;
using FluentAssertions;

namespace AppFront.Tests.AppFront.Main;

public class DocViewerTests
{
    private const string DocAssetUrlForType = "./_content/NodeFormEditor/Docs/doc{.lang}.md";
    private const string DocAssetUrlForField = "./_content/NodeFormEditor/Docs/{lang}/field.md";
    private const string DocAssetUrlForMethod = "./_content/NodeFormEditor/Docs/doc_method{.lang}.md";

    [FunctionApiDocument(DocAssetUrlForType)]
    class DocAttributedClass
    {

        [FunctionApiDocument(DocAssetUrlForField)]
        public string Propeprty1 { get; set; } = "";

        [FunctionApiDocument(DocAssetUrlForMethod)]
        public void Method1()
        {
        }
    }

    [Fact]
    public async Task ExtractUrl_AttributeFromGetTypeMethod_Success()
    {
        // Arrange
        _ = nameof(FunctionApiDocumentAttribute);
        object node = new DocAttributedClass();
        Expression<Func<Type>> For = () => node.GetType();
        var attr = typeof(DocAttributedClass).GetCustomAttribute<FunctionApiDocumentAttribute>();
        var expectUrl = FunctionApiDocumentAttribute.ReplaceLang(attr.Url, "ru");

        // Act
        var html = await AppFrontTestUtils.RenderComponent<DocViewer<Type>>(new() { ["For"] = For });

        // Assert
        expectUrl.Should().BeEquivalentTo(FunctionApiDocumentAttribute.ReplaceLang(DocAssetUrlForType, "ru"));
        html.Should().Contain(expectUrl);
    }

    [Fact]
    public async Task ExtractUrl_AttributeFromField_Success()
    {
        // Arrange
        _ = nameof(FunctionApiDocumentAttribute);
        var node = new DocAttributedClass();
        Expression<Func<string>> For = () => node.Propeprty1;
        var attr = typeof(DocAttributedClass).GetProperty(nameof(DocAttributedClass.Propeprty1))!.GetCustomAttribute<FunctionApiDocumentAttribute>();
        var expectUrl = FunctionApiDocumentAttribute.ReplaceLang(attr.Url, "ru");

        // Act
        var html = await AppFrontTestUtils.RenderComponent<DocViewer<string>>(new() { ["For"] = For });

        // Assert
        expectUrl.Should().BeEquivalentTo(FunctionApiDocumentAttribute.ReplaceLang(DocAssetUrlForField, "ru"));
        html.Should().Contain(expectUrl);
    }

    [Fact]
    public async Task ExtractUrl_AttributeWhenPassTypeOf_Success()
    {
        // Arrange
        _ = nameof(FunctionApiDocumentAttribute);
        var node = new DocAttributedClass();
        Expression<Func<Type>> For = () => typeof(DocAttributedClass);
        var attr = typeof(DocAttributedClass).GetCustomAttribute<FunctionApiDocumentAttribute>();
        var expectUrl = FunctionApiDocumentAttribute.ReplaceLang(attr.Url, "ru");

        // Act
        var html = await AppFrontTestUtils.RenderComponent<DocViewer<Type>>(new() { ["For"] = For });

        // Assert
        expectUrl.Should().BeEquivalentTo(FunctionApiDocumentAttribute.ReplaceLang(DocAssetUrlForType, "ru"));
        html.Should().Contain(expectUrl);
    }

    [Fact]
    public async Task ExtractUrl_AttributeFromMethod_Success()
    {
        // Arrange
        _ = nameof(FunctionApiDocumentAttribute);
        var node = new DocAttributedClass();
        Expression<Func<Action>> For = () => node.Method1;

        var attr = typeof(DocAttributedClass).GetMethod(nameof(node.Method1))!.GetCustomAttribute<FunctionApiDocumentAttribute>();
        var expectUrl = FunctionApiDocumentAttribute.ReplaceLang(attr.Url, "ru");

        // Act
        var html = await AppFrontTestUtils.RenderComponent<DocViewer<Action>>(new() { ["For"] = For });

        // Assert
        expectUrl.Should().BeEquivalentTo(FunctionApiDocumentAttribute.ReplaceLang(DocAssetUrlForMethod, "ru"));
        html.Should().Contain(expectUrl);
    }

}
