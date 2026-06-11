#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"

#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo
let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let targetDir, _ = Helpers.PreferLessDeeplyNestedDir currentDir rootDir

let invalidFiles =
    Helpers.GetInvalidFiles
        targetDir
        "*.yml"
        FileConventions.DetectUnpinnedDotnetToolInstallVersions

let message =
    "Please define the package version number in the `dotnet tool install` commands."

Helpers.AssertNoInvalidFiles invalidFiles message
