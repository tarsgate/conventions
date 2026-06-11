#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "nuget: FSharpx.Collections, Version=3.1.0"
#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"
#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo
let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let targetDir, _ = Helpers.PreferLessDeeplyNestedDir currentDir rootDir

let invalidFiles =
    let filter fileInfo =
        not(FileConventions.IsExecutable fileInfo)

    [ "*.fsx"; "*.sh" ]
    |> Seq.collect(fun pattern ->
        Helpers.GetInvalidFiles targetDir pattern filter
    )

Helpers.AssertNoInvalidFiles
    invalidFiles
    "The following files don't have execute permission:"
