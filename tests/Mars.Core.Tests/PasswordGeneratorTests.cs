using Mars.Core.Utils;

namespace Test.Mars.Core;

public class PasswordGeneratorTests
{

    [Theory]
    [InlineData(10, 0)]
    [InlineData(16, 6)]
    [InlineData(5, 5)]
    [InlineData(6, 2)]
    public void PasswordGenerator(int length, int numberOfNonAlphanumericCharacters)
    {
        var result = Password.Generate(length, numberOfNonAlphanumericCharacters);

        Assert.Equal(length, result.Length);
    }

}
