namespace Mars.Integration.Tests.Attributes;

public class ExternalIntegrationFactAttribute : FactAttribute
{
    public ExternalIntegrationFactAttribute()
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("SKIP_EXTERNAL_INTEGRATION_TESTS"), out bool skip) && skip)
        {
            Skip = "env SKIP_EXTERNAL_INTEGRATION_TESTS exist";
        }
    }
}
