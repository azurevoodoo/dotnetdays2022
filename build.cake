public static string FileReadText(this ICakeContext context, FilePath file)
    => System.IO.File.ReadAllText(file.MakeAbsolute(context.Environment).FullPath);

public string FileReadText(FilePath file)
   => Context.FileReadText(file);

public record BuildData(
    string Project,
    DirectoryPath ArtifactsPath,
    string Configuration,
    string Version
)
{
    public DotNetMSBuildSettings MSBuildSettings { get; } =
        new DotNetMSBuildSettings {
            Version = Version
        }.SetConfiguration(Configuration);
}

Setup(context => new BuildData(
    "src",
    "./artifacts",
    "Release",
        GitHubActions.IsRunningOnGitHubActions
        ? FormattableString.Invariant($"{DateTime.UtcNow:yyyy.MM.dd}.{GitHubActions.Environment.Workflow.RunNumber}")
        : "1.0.0.0"
        
));

Task("Restore")
    .Does<BuildData>(
        (context, data) => DotNetRestore(
            data.Project,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
            )
    );

Task("Build")
    .IsDependentOn("Restore")
    .Does<BuildData>(
        (context, data) => DotNetBuild(
            data.Project,
            new DotNetBuildSettings {
                MSBuildSettings = data.MSBuildSettings,
                NoRestore = true
            }
            )
    );

Task("Test")
    .IsDependentOn("Build")
    .Does<BuildData>(
        (context, data) =>DotNetTest(
            data.Project,
            new DotNetTestSettings {
                Configuration = data.Configuration,
                NoBuild = true,
                NoRestore = true
            }
            )
    );

Task("Pack")
    .IsDependentOn("Test")
    .Does<BuildData>(
        (context, data) => DotNetPack(
            data.Project,
            new DotNetPackSettings {
                MSBuildSettings = data.MSBuildSettings,
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.ArtifactsPath
            }
            )
    );

Task("Publish")
    .IsDependentOn("Pack")
    .Does<BuildData>(
        (context, data) => GitHubActions.Commands.UploadArtifact(
            data.ArtifactsPath,
          $"dotnetdays{Context.Environment.Platform.Family}"
        )
    );

Task("GitHubActions")
    .IsDependentOn("Publish");

RunTarget(Argument("target","Pack"));