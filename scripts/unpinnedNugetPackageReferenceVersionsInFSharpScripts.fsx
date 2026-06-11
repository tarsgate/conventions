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
        "*.fsx"
        FileConventions.DetectMissingVersionsInNugetPackageReferences

let message =
    "The following files shouldn't miss the version number in `#r \"nuget: ` refs of F# scripts."
    + Environment.NewLine
    + "Please use the exact version of the package instead."

Helpers.AssertNoInvalidFiles invalidFiles message
