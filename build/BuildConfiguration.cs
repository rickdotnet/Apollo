namespace build;

public class BuildConfiguration
{
    public string RootDirectory { get; }
    public string SolutionFile => Path.Combine(RootDirectory, "src", "Apollo.sln");
    public string PackOutput => Path.Combine(RootDirectory, "dist");
    public string TestSearchPattern => Path.Combine(".", "tests", "*");
    public string ProjectsSearchPattern => "Apollo*.csproj";
    
    public string[] TestDirectories => Directory.GetDirectories(RootDirectory, TestSearchPattern, SearchOption.TopDirectoryOnly);
    public string[] ProjectFiles => Directory.GetFiles(Path.Combine(RootDirectory, "src"), ProjectsSearchPattern, SearchOption.AllDirectories);
    public string[] NugetPackages => Directory.GetFiles(PackOutput, "*.nupkg", SearchOption.AllDirectories);
    
    public string NuGetSource => "https://api.nuget.org/v3/index.json";

    public string? NuGetApiKey { get; }
    
    public BuildConfiguration(string? rootDirectory = null, string? nuGetApiKey = null)
    {
        RootDirectory = rootDirectory ?? Directory.GetCurrentDirectory();
        NuGetApiKey = nuGetApiKey ?? Environment.GetEnvironmentVariable("NUGET_API_KEY");
    }
}