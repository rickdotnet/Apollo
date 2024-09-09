using build;
using static Bullseye.Targets;

var configuration = new BuildConfiguration();

Target(Targets.Build, () => BuildHelper.BuildSolution(configuration.SolutionFile));

Target(Targets.Test, DependsOn(Targets.Build), ForEach(configuration.TestDirectories), BuildHelper.RunTests);

Target(Targets.CleanPackOutput, () => BuildHelper.CleanFolder(configuration.PackOutput));

Target(Targets.Pack, DependsOn(Targets.Test, Targets.CleanPackOutput),
    async () => await BuildHelper.PackProjects(configuration));

Target(Targets.Publish, DependsOn(Targets.Pack), () => BuildHelper.PublishPackage(configuration));

Target(Targets.GenerateDocs, DependsOn(Targets.Build), async () => await BuildHelper.GenerateDocs(configuration));

//Target("default", DependsOn(Targets.Pack));
Target("default", DependsOn(Targets.GenerateDocs));

await RunTargetsAndExitAsync(args);