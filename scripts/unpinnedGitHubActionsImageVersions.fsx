#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "nuget: FSharpx.Collections, Version=3.1.0"
#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"
#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo

let invalidFiles =
    Helpers.GetInvalidFiles
        rootDir
        "*.yml"
        FileConventions.DetectUnpinnedVersionsInGitHubCI

let message =
    "The following files shouldn't contain `-latest` in `runs-on:` or `container: image:` GitHubCI tags."
    + Environment.NewLine
    + "Here is a list of available runner image versions that you can use:"
    + Environment.NewLine
    + "https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#choosing-github-hosted-runners"

Helpers.AssertNoInvalidFiles invalidFiles message
