using BenchmarkDotNet.Attributes;
using Mars.Integration.Tests.Common;

public abstract class BenchmarkBase
{
    protected static ApplicationFixture AppFixture = null!;

    //[GlobalSetup]
    public async Task GlobalSetup()
    {
        if (AppFixture != null)
            return;

        AppFixture = new ApplicationFixture();
        await AppFixture.InitializeAsync();
    }

    //[GlobalCleanup]
    public async Task GlobalCleanup()
    {
        // ничего не делаем
        // fixture живёт до конца процесса
    }
}
