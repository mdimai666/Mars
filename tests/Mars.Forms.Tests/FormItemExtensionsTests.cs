using FluentAssertions;
using Mars.Forms.Contracts;

namespace Mars.Forms.Tests;

public class FormItemExtensionsTests
{
    [Fact]
    public void Field_FindsFieldAmongStructuralNodes()
    {
        var form = new FormDefinition
        {
            OwnerModel = "post.article",
            Items =
            [
                new FormItem { Key = "row-1", Kind = FormItemKind.Row, Zone = "main" },
                new FormItem { Key = "col-1", Kind = FormItemKind.Column, Parent = "row-1" },
                new FormItem { Key = "content", Parent = "col-1", Field = Descriptor("content") },
                new FormItem { Key = "heading-1", Kind = FormItemKind.Heading, Parent = "col-1" },
            ],
        };

        form.Field("content")!.Field!.Key.Should().Be("content");
        form.Field("col-1").Should().BeNull("структурный узел — не поле");
        form.Field("heading-1").Should().BeNull("неполевой элемент дескриптора не имеет");
        form.Field("missing").Should().BeNull();
    }

    static FormFieldDescriptor Descriptor(string key) => new()
    {
        Key = key,
        Title = key,
        Type = FormFieldType.Text,
        Editor = FormEditorCatalog.BlockEditor,
    };
}
