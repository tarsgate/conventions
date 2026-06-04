#!/usr/bin/env -S dotnet fsi

open System
open System.IO

#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"

#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo

printfn "Checking base directory: %s" rootDir.FullName

let invalidFiles =
    Helpers.GetFiles rootDir "*.fsx"
    |> Seq.filter(fun fileInfo ->
        printfn "Checking file: %s" fileInfo.FullName
        not(FileConventions.HasCorrectShebang fileInfo)
    )

Helpers.AssertNoInvalidFiles
    invalidFiles
    "The following files should have the correct shebang:"
