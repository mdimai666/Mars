using System.Collections.ObjectModel;
using Mars.Host.Services;
using Mars.Shared.Models.Interfaces;
using FluentAssertions;

namespace Test.Mars.Host.Services;

public class BasicModelService
{
    class Entity : IBasicEntity
    {
        public Entity(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
    };

    [Fact]
    public void CollectionDiffById()
    {
        var id1 = new Entity(new Guid("01004aea-1ca1-4b7b-b643-93074b29272e"));
        var id2 = new Entity(new Guid("0200f703-1b90-4361-bd18-480ce19f75e6"));
        var id3 = new Entity(new Guid("030096b7-67b9-4db6-9728-596c8cb76c8b"));
        var id4 = new Entity(new Guid("040069d1-9329-4b6c-b4e4-2caf222e414d"));

        Collection<Entity> existList = [id1, id2];
        Collection<Entity> newList = [id2, id3, id4];

        var (added, removed) = ModelServiceTools.CollectionDiffById(existList, newList);

        added.Should().BeEquivalentTo([id3, id4]);
        removed.Should().BeEquivalentTo([id1]);
    }
}
