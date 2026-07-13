#!/usr/bin/env -S dotnet fsi

open System.IO

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
    let repositoryRootDir = targetDir.Parent.Parent

    let globalEnv =
        let dotEnvFile =
            Path.Join(repositoryRootDir.FullName, ".env") |> FileInfo

        if dotEnvFile.Exists then
            let lines = File.ReadAllLines dotEnvFile.FullName

            lines
            |> Seq.filter(fun line ->
                not(
                    System.String.IsNullOrWhiteSpace line || line.StartsWith '#'
                )
            )
            |> Seq.map(fun line ->
                let [| key; value |] = line.Split('=', count = 2)
                key.Trim(), value.Trim()
            )
            |> Map.ofSeq
        else
            Map.empty

    FileConventions.DetectInconsistentVersionsInGitHubCI targetDir globalEnv

if inconsistentVersionsInGitHubCI then
    failwith
        "You shouldn't use inconsistent versions in `uses: foo@bar` or `with: foo-version: bar` statements in GitHubCI files."
