namespace Mars.Integration.Tests.Attributes;

public class IntegrationTheoryAttribute : TheoryAttribute
{
    public IntegrationTheoryAttribute()
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("SKIP_INTEGRATION_TESTS"), out bool skip) && skip)
        {
            Skip = "env SKIP_INTEGRATION_TESTS exist";
        }
    }
}
