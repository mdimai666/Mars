using Mars.Core.Utils;

namespace Test.Mars.Core;

public class DiffListTests
{
    [Fact]
    public void FindDifferences_ExpectDifferences_Success()
    {
        //Arrange
        int[] exist = [1, 2, 3];
        int[] news = [2, 3, 4];

        //Act
        var diff = DiffList.FindDifferences(exist, news);

        //Assert
        Assert.Equal(diff.ToAdd, [4]);
        Assert.Equal(diff.ToRemove, [1]);
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public void FindDifferences_EqualsHasNoChanges_Success()
    {
        //Arrange
        int[] exist = [1, 2, 3];
        int[] news = [1, 2, 3];

        //Act
        var diff = DiffList.FindDifferences(exist, news);

        //Assert
        Assert.Empty(diff.ToAdd);
        Assert.Empty(diff.ToRemove);
        Assert.False(diff.HasChanges);
    }

    class TestableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = Guid.NewGuid().ToString();

        public TestableObject(int id)
        {
            Id = id;
        }
    }

    [Fact]
    public void FindDifferencesBy_ObjectsByKeySelectorInsteadEqualent_Success()
    {
        //Arrange
        List<TestableObject> exist = [new(1), new(2), new(3)];
        List<TestableObject> news = [new(2), new(3), new(4)];

        //Act
        var diff = DiffList.FindDifferencesBy(exist, news, s => s.Id);

        //Assert
        Assert.Contains(news[2], diff.ToAdd);
        Assert.Contains(exist[0], diff.ToRemove);
        Assert.True(diff.HasChanges);
    }
}
