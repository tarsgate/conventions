This is a repository that contains several useful things that other [tarsgate](https://github.com/tarsgate/) and `nblockchain` repos use, such as:

- [Config file](commitlint.config.ts) and [plugins](commitlint/plugins.ts) for commitlint.
- [A reference .editorconfig file](.editorconfig).
- [Development Guidelines](docs/).
- Scripts that aid maintainability:
    * [Detection of .fsx scripts without shebang](scripts/shebangConvention.fsx).
    * [Detection of .fsx files without +x attrib](scripts/executableConvention.fsx).
    * [F# scripts compilation](scripts/compileFSharpScripts.fsx).
    * [EOF without EOL detection](scripts/eofConvention.fsx).
    * [Mixed line-endings detection](scripts/mixedLineEndings.fsx).
    * [Auto-wrap commit message body and enforce title max length](scripts/wrapLatestCommitMsg.fsx).
    * [Detect non-verbose flags (e.g. `dotnet build -c Debug` instead of `dotnet build --configuration Debug`) being used in scripts or YML CI files (there are exceptions, e.g. `env -S`)](scripts/nonVerboseFlagsInGitHubCIAndScripts.fsx).
    * Use of unpinned versions:
        * [Use of `-latest` suffix in `runs-on:` GitHubCI tags](scripts/unpinnedGitHubActionsImageVersions.fsx).
        * [Use of asterisk (*) in `PackageReference` items of .NET projects](scripts/unpinnedNugetPackageReferenceVersionsInProjects.fsx).
        * [Missing the version number in `#r "nuget: ` refs of F# scripts](scripts/unpinnedNugetPackageReferenceVersionsInFSharpScripts.fsx).
        * [Missing the `--version` flag in `dotnet tool install foo` invocations](scripts/unpinnedDotnetToolInstallVersions.fsx).
    * Use of inconsistent versions:
        * [Use of inconsistent version numbers in `uses:` and `with:` GitHubCI tags](scripts/inconsistentVersionsInGitHubCI.fsx).
        * [Use of inconsistent version numbers in `#r "nuget: ` refs of F# scripts](scripts/inconsistentNugetVersionsInFSharpScripts.fsx)
        * [Use of inconsistent version numbers in nuget packages of .NET projects](scripts/inconsistentNugetVersionsInDotNetProjects.fsx).
        * [Use of inconsistent version numbers in nuget packages of .NET projects and `#r "nuget: ` refs of F# scripts](scripts/inconsistentNugetVersionsInDotNetProjectsAndFSharpScripts.fsx).

All in all, this is mainly documentation, and some tooling to detect bad practices.

More things to come:
- Detect old versions of fantomas/fantomless being used.
- Detect old versions of .editorconfig or Directory.Build.props being used.
- Detect GitHubCI bad practices, such as:
    * Missing important triggers such as push or pull_request, workflow_dispatch, schedule.
    * Branch filtering on push trigger (only acceptable one is '**', otherwise '*' doesn't match to branch names with slashes in them).
- GitHub comment auto-responder? E.g. to answer to comments that end with "not working" or "doesn't work" or "does not work", asking for more details.
