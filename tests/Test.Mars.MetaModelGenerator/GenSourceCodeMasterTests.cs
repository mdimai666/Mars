using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Mars.Data.Entities;
using Mars.MetaModelGenerator;
using Mars.Test.Common.FixtureCustomizes;
using Test.Mars.MetaModelGenerator.Tools;

namespace Test.Mars.MetaModelGenerator;

public class GenSourceCodeMasterTests
{
    private readonly IFixture _fixture;

    public GenSourceCodeMasterTests()
    {
        _fixture = new Fixture();
    }

    [Theory]
    [InlineData("mytype", "MytypeMto")]
    [InlineData("temp-page", "TempPageMto")]
    [InlineData("MyType", "MyTypeMto")]
    [InlineData("a_b-c", "A_bCMto")]
    public void GetNormalizedTypeName_InvalidChars_PascalCasesSegments(string typeName, string expected)
    {
        GenSourceCodeMasterHelper.GetNormalizedTypeName(typeName).Should().Be(expected);
    }

    [Fact]
    public void Generate_CSharpKeywordKey_EscapedAndCompiles()
    {
        //Arrange — ключ метаполя становится именем свойства; ключевое слово C# должно экранироваться '@'
        MetaFieldEntity[] metaFields = [
            new ()
            {
                Key = "int",
                Title = "int",
                Type = EMetaFieldType.Int,
            }];

        var master = new GenSourceCodeMaster();
        var newClassName = GenSourceCodeMasterHelper.GetNormalizedTypeName("MyType");

        //Act
        var code = master.Generate(newClassName, typeof(PostEntity), metaFields, new(), null);

        //Assert
        code.Should().Contain("public int? @int { get; set; }");
        code.Should().Contain("@int = post.MetaValues!");
        code.Should().Contain("f.MetaField.Key == \"int\"");

        var metaType = TestingScriptCompiler.Compile(code, newClassName);
        metaType.GetProperty("int", BindingFlags.Instance | BindingFlags.Public).Should().NotBeNull();
    }

    [Fact]
    public void Generate_PrimitiveTypes_Succeeds()
    {
        //Arrange
        MetaFieldEntity[] metaFields = [
            new ()
            {
                Key = "str1",
                Title = "String 1",
                Type = EMetaFieldType.String,
            },
            new ()
            {
                Key = "int1",
                Title = "Integer 1",
                Type = EMetaFieldType.Int,
            }];

        var master = new GenSourceCodeMaster();
        var newClassName = GenSourceCodeMasterHelper.GetNormalizedTypeName("MyType");

        //Act
        var code = master.Generate(newClassName, typeof(PostEntity), metaFields, new(), null);

        //Assert
        var metaType = TestingScriptCompiler.Compile(code, newClassName);

        metaFields.Should().AllSatisfy(mf => metaType.GetProperty(mf.Key, BindingFlags.Instance | BindingFlags.Public).Should().NotBeNull());
    }

    [Theory]
    [InlineData([EMetaFieldType.Bool])]
    [InlineData([EMetaFieldType.Int])]
    [InlineData([EMetaFieldType.Long])]
    [InlineData([EMetaFieldType.Float])]
    [InlineData([EMetaFieldType.Decimal])]
    [InlineData([EMetaFieldType.DateTime])]
    [InlineData([EMetaFieldType.String])]
    [InlineData([EMetaFieldType.Text])]
    public void Generate_AllPrimitiveTypes_Succeeds(EMetaFieldType fieldType)
    {
        //Arrange
        MetaFieldEntity[] metaFields = [
            new ()
            {
                Key = "key_" + fieldType.ToString().ToLower(),
                Title = "Title " + fieldType.ToString(),
                Type = fieldType,
            }];

        var master = new GenSourceCodeMaster();
        var newClassName = GenSourceCodeMasterHelper.GetNormalizedTypeName("MyType");

        //Act
        var code = master.Generate(newClassName, typeof(PostEntity), metaFields, new(), null);

        //Assert
        var metaType = TestingScriptCompiler.Compile(code, newClassName);

        metaFields.Should().AllSatisfy(mf => metaType.GetProperty(mf.Key, BindingFlags.Instance | BindingFlags.Public).Should().NotBeNull());
    }

    [Theory]
    [InlineData([EMetaFieldType.Select])]
    [InlineData([EMetaFieldType.SelectMany])]
    [InlineData([EMetaFieldType.Image])]
    [InlineData([EMetaFieldType.File])]
    [InlineData([EMetaFieldType.Relation])]// add relation Post Post.page File and other, add Plugin
    public void Generate_AllNonPrimitiveTypes_Succeeds(EMetaFieldType fieldType)
    {
        //Arrange
        _ = nameof(MtFieldInfo);
        var newClassName = GenSourceCodeMasterHelper.GetNormalizedTypeName("MyType");
        var key1 = "key_" + fieldType.ToString().ToLower();
        MetaFieldEntity[] metaFields = [
            new ()
            {
                Key = key1,
                Title = "Title " + fieldType.ToString(),
                Type = fieldType,
                ModelName = fieldType == EMetaFieldType.Relation ? $"Post.{key1}" : null,
            }];

        var master = new GenSourceCodeMaster();

        var metaModelTypesResolverDict = new Dictionary<string, MetaModelResolveTypeInfo>
        {
            //["Post.post"] = new(true, GenSourceCodeMasterHelper.GetNormalizedTypeName("post"), null),
            [$"Post.{key1}"] = new(true, newClassName, null),
        };

        //Act
        var code = master.Generate(newClassName, typeof(PostEntity), metaFields, new(), metaModelTypesResolverDict);

        //Assert
        var metaType = TestingScriptCompiler.Compile(code, newClassName);

        metaFields.Should().AllSatisfy(mf => metaType.GetProperty(mf.Key, BindingFlags.Instance | BindingFlags.Public).Should().NotBeNull());
    }

}

/*
 status, user, fill relation mf
 */
