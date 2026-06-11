#!/usr/bin/env -S dotnet fsi

open System.IO

#r "nuget: FSharpx.Collections, Version=3.1.0"
#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"
#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo

let githubWorkflowsDirFromRootDir =
    Path.Combine(rootDir.FullName, ".github", "workflows") |> DirectoryInfo

let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let githubWorkflowsDirFromCurrentDir =
    Path.Combine(currentDir.FullName, ".github", "workflows") |> DirectoryInfo

let targetDir =
    match
        githubWorkflowsDirFromRootDir.Exists,
        githubWorkflowsDirFromCurrentDir.Exists
        with
    | true, false -> githubWorkflowsDirFromRootDir
    | false, true -> githubWorkflowsDirFromCurrentDir
    | false, false -> failwith "No .github/workflows/ subfolder found"
    | true, true ->
        Helpers.PreferLessDeeplyNestedDir
            githubWorkflowsDirFromCurrentDir
            githubWorkflowsDirFromRootDir
        |> fst

let inconsistentVersionsInGitHubCI =
    FileConventions.DetectInconsistentVersionsInGitHubCI targetDir

if inconsistentVersionsInGitHubCI then
    failwith
        "You shouldn't use inconsistent versions in `uses: foo@bar` or `with: foo-version: bar` statements in GitHubCI files."
