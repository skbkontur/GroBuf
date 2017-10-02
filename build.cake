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
    Information(@"Rebuilding grobuf solution. Configuration : {0}", configuration);
    MSBuild(slnPath, settings => settings.WithTarget("Rebuild")
                                         .SetVerbosity(Verbosity.Minimal)
                                         .SetConfiguration(configuration)
                                         .UseToolVersion(MSBuildToolVersion.VS2017));
});

Task("Run-Unit-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    RunTests(excludeLongRunning : true);
});

Task("Run-All-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    RunTests(excludeLongRunning : false);
});

Task("Build-And-Merge")
    .IsDependentOn("Rebuild")
    .IsDependentOn("Run-Unit-Tests")
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

private void RunTests(bool excludeLongRunning){
    var settings = new NUnitSettings
    {
        NoResults = true,
        OutputFile = "./.tests-output.txt"
    };
    if(excludeLongRunning)
        settings.Exclude = "LongRunning";
    NUnit("./GroBuf/Tests/**/bin/" + configuration + "/*.Tests.dll", settings);
}