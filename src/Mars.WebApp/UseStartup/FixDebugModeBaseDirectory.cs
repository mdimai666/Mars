namespace Mars.UseStartup;

public static class FixDebugModeBaseDirectory
{
    public static void SetBaseDirectory()
    {
        //FIX for NET7 AppAdmin serve WebAssembly files
        if (Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1")
        {
            Console.WriteLine("DOTNET_WATCH");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }

        //Debugger.IsAttached
        if (!MarsStartupInfo.IsRunUnderVisualStudio)
        {
            Directory.SetCurrentDirectory(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        }
    }
}
