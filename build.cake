#tool "nuget:?package=NUnit.Runners&version=2.6.4"
#tool "nuget:?package=ilmerge"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var slnPath = "./GroBuf/GroBuf.sln";

Task("Default")
    .Does(() =>
{
    Information(@"Supported cake targets:
        Restore-NuGet-Packages : Runs NuGet to restore all required packages.
        Rebuild : Rebuild grobuf to ./Assemblies/
        Build-And-Merge : Rebuild and il-merge all required assemblies to ./Output/GroBuf.dll
        Run-Unit-Tests : Rebuild and run unit tests excluding 'LongRunning' category.
        Run-All-Tests : Rebuild and run all unit tests.
    ");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    var solutions = GetFiles("./GroBuf/**/*.sln");
    foreach(var solution in solutions)
    {
        Information("Restoring {0}", solution);
        NuGetRestore(solution);
    }
});

Task("Rebuild")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => 
{
    Information("Cleaning output directory.");
    CleanDirectory("./Output/");
    
    Information(@"Rebuilding grobuf solution. Configuration : {0}", configuration);
    MSBuild(slnPath, settings => settings.WithTarget("Rebuild")
                                         .SetVerbosity(Verbosity.Minimal)
                                         .SetConfiguration(configuration)
                                         .UseToolVersion(MSBuildToolVersion.VS2017));

    Information("Copying build results to output directory");
    EnsureDirectoryExists("./Output/bin/");
    CopyDirectory($"./GroBuf/GroBuf/bin/{configuration}/", "./Output/bin/");
});

Task("Run-Unit-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    Information("Running unit tests excluding 'LongRunning' category.");
    RunTests(excludeLongRunning : true);
});

Task("Run-All-Tests")
    .IsDependentOn("Rebuild")
    .Does(() =>
{
    Information("Running all unit tests.");
    RunTests(excludeLongRunning : false);
});

Task("Build-And-Merge")
    .IsDependentOn("Rebuild")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    var assembliesToMerge = GetFiles("./Output/bin/*.dll").Where(file => file.GetFilename().ToString() != "GroBuf.dll");
    Information("Merging {0} into the GroBuf.dll", string.Join(", ", assembliesToMerge.Select(x => x.GetFilename())));
    ILMerge(
        "./Output/GroBuf.dll",
        "./Output/bin/GroBuf.dll",
        assembliesToMerge,
        new ILMergeSettings { Internalize = true });
});

RunTarget(target);

private void RunTests(bool excludeLongRunning){
    EnsureDirectoryExists("./Output/test/");
    var settings = new NUnitSettings
    {
        NoResults = true,
        OutputFile = "./Output/test/tests-output.txt"
    };
    if(excludeLongRunning)
        settings.Exclude = "LongRunning";
    NUnit("./GroBuf/Tests/**/bin/" + configuration + "/*.Tests.dll", settings);
}