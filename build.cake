#tool "nuget:?package=NUnit.Runners&version=2.6.4"
#tool "nuget:?package=ilmerge"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var slnPath = "./GroBuf/GroBuf.sln";

Task("Default")
    .Does(() =>
{
    Information(@"Supported cake targets:
        Rebuild - rebuild grobuf to ./Assemblies/
        Build-And-Merge - rebuild and il-merge all required assemblies to ./Output/GroBuf.dll
        Run-Unit-Tests - rebuild and run unit tests
    ");
});

Task("Rebuild")
    .Does(() => 
{
    MSBuild(slnPath, settings => settings.SetConfiguration(configuration)
                                         .WithTarget("Rebuild")
                                         .UseToolVersion(MSBuildToolVersion.VS2017));
});

Task("Run-Unit-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    NUnit("./GroBuf/Tests/**/bin/" + configuration + "/*.Tests.dll", new NUnitSettings { NoResults = true });
});

Task("Build-And-Merge")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    CleanDirectory("./Output/");
    ILMerge(
        "./Output/GroBuf.dll",
        "./Assemblies/GroBuf.dll",
        new FilePath[] {
            "./Assemblies/GrEmit.dll",
            "./Assemblies/Mono.Reflection.dll",
        },
        new ILMergeSettings { Internalize = true });
});

RunTarget(target);