var target          = Argument("target", "Default");
var configuration   = Argument<string>("configuration", "Release");
var ciMode          = Argument<bool>("ciMode", false);
var buildNumber     = Argument<string>("buildNumber", "");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var isLocalBuild        = !AppVeyor.IsRunningOnAppVeyor;
var buildArtifacts      = Directory("./artifacts/packages");
var sourceRoot          = "./src";
var testsRoot           = "./test";
var sourceProjects      = sourceRoot + "/**/project.json";
var testProjects        = testsRoot + "/**/project.json";
var nugetConfigFile     = new FilePath("NuGet.config");


 Information("CI MODE");
 Information(ciMode.ToString());
 Information("Build Number");
 Information(buildNumber);

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
	var projects = GetFiles(sourceProjects);

	foreach(var project in projects)
	{
        var settings = new DotNetCoreBuildSettings 
        {
            Configuration = configuration
        };

        DotNetCoreBuild(project.GetDirectory().FullPath, settings); 
    }
});

Task("RunTests")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var projects = GetFiles(testProjects);

    foreach(var project in projects)
	{
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration
        };

        if (!IsRunningOnWindows())
        {
            Information("Not running on Windows - skipping tests for full .NET Framework");
            settings.Framework = "netcoreapp1.1";
        }

        DotNetCoreTest(project.GetDirectory().FullPath, settings);
    }
});

Task("Pack")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var projects = GetFiles(sourceProjects);

    Information("ALL PROJECTS");
    foreach (var project in projects) {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = buildArtifacts,
        };

        // add build suffix for CI builds
        if(!isLocalBuild)
        {
            settings.VersionSuffix = "build" + AppVeyor.Environment.Build.Number.ToString().PadLeft(5,'0');
        } else if (ciMode) {
            settings.VersionSuffix = buildNumber;
        }

        DotNetCorePack(project.GetDirectory().FullPath, settings);
    }   
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { buildArtifacts });
});

Task("Restore")
    .Does(() =>
{
    DotNetCoreRestoreSettings settings = new DotNetCoreRestoreSettings();

    if (FileExists(nugetConfigFile)) 
    {
        settings.ConfigFile = nugetConfigFile;
    } else {
        settings.Sources = new [] { 
            "https://api.nuget.org/v3/index.json",
            "https://www.myget.org/F/aspnet-contrib/api/v3/index.json",
            "https://dotnet.myget.org/F/aspnetcore-ci-dev/api/v3/index.json" 
        };
    };    

    DotNetCoreRestore(sourceRoot, settings);
    DotNetCoreRestore(testsRoot, settings);
});

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(target);