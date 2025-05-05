using FluentAssertions;

namespace Mars.Integration.Tests.Extensions;

public static class AssertValidateExtensions
{
    public static void ValidateSatisfy(this IDictionary<string, string[]> errors, Dictionary<string, string[]> expectError)
    {
        errors.Should().HaveSameCount(expectError);
        errors.Should().AllSatisfy(x =>
        {
            var expects = expectError[x.Key];
            var errors = x.Value;

            expects.Should().AllSatisfy(expect =>
            {
                errors.Should().ContainMatch(expect);
            });
        });
    }
}
