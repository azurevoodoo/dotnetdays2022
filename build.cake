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
    FormattableString.Invariant(
        $"{DateTime.UtcNow:yyyy.MM.dd}.0"
        )
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

RunTarget(Argument("target","Pack"));