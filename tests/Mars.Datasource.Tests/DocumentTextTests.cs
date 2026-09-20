using FluentAssertions;
using Mars.Datasource.Contracts.Catalog;
using Mars.Datasource.Contracts.Config;
using Mars.Datasource.Contracts.Document;
using Mars.Datasource.Contracts.Query;

namespace Mars.Datasource.Tests;

/// <summary>
/// Документ запросов как текст: блоки, переход к блоку по строке курсора, вставка и удаление.
/// Рабочая область правит документ целиком, поэтому вся арифметика по строкам проверяется здесь.
/// </summary>
public class DocumentTextTests
{
    const string Document = """
        @host = https://example.org

        ###
        # @name posts
        GET {{host}}/wp/v2/posts
        Accept: application/json

        ###
        # @name users
        GET {{host}}/wp/v2/users
        """;

    const string TwoPosts = """
        ###
        # @name posts
        GET /wp/v2/posts

        ###
        # @name posts (2)
        GET /wp/v2/posts
        """;

    [Fact]
    public void Blocks_SplitBySeparator()
    {
        var blocks = DocumentText.Blocks(Document);

        blocks.Should().HaveCount(2);
        blocks[0].StartLine.Should().Be(3);
        blocks[0].EndLine.Should().Be(6);
        blocks[0].Text.Should().StartWith("###\n# @name posts");
        blocks[1].StartLine.Should().Be(8);
        blocks[1].EndLine.Should().Be(10);
    }

    [Fact]
    public void Blocks_DocumentWithoutSeparatorIsOneBlock()
    {
        var blocks = DocumentText.Blocks("GET /wp/v2/posts");

        blocks.Should().ContainSingle();
        blocks[0].StartLine.Should().Be(1);
        blocks[0].EndLine.Should().Be(1);
    }

    [Fact]
    public void Blocks_PreambleWithVariablesIsNotABlock()
    {
        var blocks = DocumentText.Blocks("@host = https://example.org\n\n###\nGET /x");

        blocks.Should().ContainSingle();
        blocks[0].StartLine.Should().Be(3);
    }

    [Fact]
    public void Blocks_PreambleWithRequestIsFirstBlock()
    {
        var blocks = DocumentText.Blocks("@host = https://example.org\nGET {{host}}/wp/v2/posts\n\n###\nGET /x");

        blocks.Should().HaveCount(2);
        blocks[0].StartLine.Should().Be(1);
        blocks[0].EndLine.Should().Be(2);
        blocks[1].StartLine.Should().Be(4);

        DocumentText.BlockAt("@host = https://example.org\nGET {{host}}/wp/v2/posts\n\n###\nGET /x", 2)
            .Should().Be(blocks[0]);
    }

    [Fact]
    public void Blocks_EmptyDocumentHasNoBlocks()
    {
        DocumentText.Blocks(null).Should().BeEmpty();
        DocumentText.Blocks("   ").Should().BeEmpty();
    }

    [Fact]
    public void BlockAt_FindsBlockUnderCursor()
    {
        DocumentText.BlockAt(Document, 4)!.Text.Should().Contain("@name posts");
        DocumentText.BlockAt(Document, 9)!.Text.Should().Contain("@name users");
    }

    [Fact]
    public void BlockAt_OutsideBlocksIsNull()
    {
        DocumentText.BlockAt(Document, 1).Should().BeNull();
        DocumentText.BlockAt(Document, 500).Should().BeNull();
    }

    [Fact]
    public void BlockContaining_FindsByFragment()
    {
        DocumentText.BlockContaining(Document, "/wp/v2/users")!.Text.Should().Contain("@name users");
        DocumentText.BlockContaining(Document, "/wp/v2/nothing").Should().BeNull();
        DocumentText.BlockContaining(Document, "").Should().BeNull();
    }

    [Fact]
    public void DocumentVariables_TakesDeclarationsBeforeFirstSeparator()
    {
        DocumentText.DocumentVariables(Document).Should().Be("@host = https://example.org");

        // комментарии и пустые строки между объявлениями не мешают; объявление внутри блока —
        // уже переменная блока, не документа
        DocumentText.DocumentVariables("@a = 1\n# комментарий\n\n@b.c = 2\n\n###\n@d = 3\nGET /x")
            .Should().Be("@a = 1\n@b.c = 2");
    }

    [Fact]
    public void DocumentVariables_StopsAtFirstRequestLine()
    {
        DocumentText.DocumentVariables("@host = x\nGET /y\n@late = 1").Should().Be("@host = x");
        DocumentText.DocumentVariables("GET /y\n@host = x").Should().Be("");
        DocumentText.DocumentVariables(null).Should().Be("");
    }

    [Fact]
    public void Append_AddsSeparatorAndBlock()
    {
        var text = DocumentText.Append("###\nGET /a", "GET /b");

        text.Should().Be("###\nGET /a\n\n###\nGET /b\n");
    }

    [Fact]
    public void Append_ToEmptyDocumentHasNoLeadingSeparator()
    {
        DocumentText.Append(null, "GET /a").Should().Be("GET /a\n");
        DocumentText.Append("", "  GET /a  ").Should().Be("GET /a\n");
        DocumentText.Append("###\nGET /a", "   ").Should().Be("###\nGET /a");
    }

    [Fact]
    public void Duplicate_InsertsCopyAfterBlockWithNewName()
    {
        var block = DocumentText.Blocks(Document)[0];

        var text = DocumentText.Duplicate(Document, block);

        DocumentText.Blocks(text).Should().HaveCount(3);
        DocumentText.Blocks(text)[1].Text.Should().Contain("# @name posts (2)");

        // Исходный блок не тронут, документ заканчивается переводом строки
        DocumentText.Blocks(text)[0].Text.Should().Contain("# @name posts\n");
        text.Should().EndWith("\n");
    }

    [Fact]
    public void Duplicate_TakesNextFreeNumber()
    {
        var block = DocumentText.Blocks(TwoPosts)[1];

        var text = DocumentText.Duplicate(TwoPosts, block);

        DocumentText.Blocks(text).Select(item => DocumentText.BlockName(item.Text))
            .Should().Equal("posts", "posts (2)", "posts (3)");
    }

    [Fact]
    public void Duplicate_BlockWithoutNameIsCopiedAsIs()
    {
        var block = DocumentText.Blocks("###\nGET /a")[0];

        var text = DocumentText.Duplicate("###\nGET /a", block);

        DocumentText.Blocks(text).Should().HaveCount(2);
        DocumentText.Blocks(text)[1].Text.Should().Be("###\nGET /a");
    }

    [Fact]
    public void Remove_DropsBlockWithSeparator()
    {
        var text = DocumentText.Remove(Document, DocumentText.Blocks(Document)[0]);

        DocumentText.Blocks(text).Should().ContainSingle();
        DocumentText.Blocks(text)[0].Text.Should().Contain("@name users");
        text.Should().NotContain("posts");
    }

    [Fact]
    public void Remove_LastBlockKeepsFirst()
    {
        var text = DocumentText.Remove(Document, DocumentText.Blocks(Document)[1]);

        DocumentText.Blocks(text).Should().ContainSingle();
        DocumentText.Blocks(text)[0].Text.Should().Contain("@name posts");
    }

    [Fact]
    public void Remove_OnlyBlockGivesEmptyDocument()
    {
        DocumentText.Remove("###\nGET /a", DocumentText.Blocks("###\nGET /a")[0]).Should().Be("");
    }

    [Theory]
    [InlineData("###\n# @name posts\nGET /x", "posts")]
    [InlineData("### Список постов\nGET /x", "Список постов")]
    [InlineData("###\nGET /x", "")]
    public void BlockName_ComesFromDirectiveOrLabel(string blockText, string expected)
    {
        DocumentText.BlockName(blockText).Should().Be(expected);
    }
}
