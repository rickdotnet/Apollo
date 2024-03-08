﻿using static SimpleExec.Command;

namespace build;

public static class BuildHelper
{
    public static Task BuildSolution(string solutionFile)
        => RunAsync("dotnet", $"build {solutionFile} -c Release --nologo");

    public static Task RunTests(string project)
        => RunAsync("dotnet", $"test {project} --configuration Release --no-build --nologo --verbosity quiet");

    public static void CleanFolder(string folder)
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, true);
    }

    public static Task PackProjects(BuildConfiguration config)
        => Task.WhenAll(
            config.ProjectFiles.Select(
                project => RunAsync("dotnet",
                    $"pack {project} -c Release -o \"{config.PackOutput}\" --no-build --nologo")
            ));

    public static Task PublishPackage(BuildConfiguration config, string package) =>
        string.IsNullOrWhiteSpace(config.NuGetApiKey)
            ? throw new Exception("No NuGet API key found")
            : RunAsync("dotnet",
                $"nuget push {package} -s {config.NuGetSource} -k {config.NuGetApiKey} --skip-duplicate");
}