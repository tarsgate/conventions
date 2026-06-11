#!/usr/bin/env -S dotnet fsi

open System.IO
open System

#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"

#load "../src/FileConventions/Helpers.fs"
#load "../src/FileConventions/Library.fs"

open type FileConventions.EolAtEof

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo
let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let targetDir, _ = Helpers.PreferLessDeeplyNestedDir currentDir rootDir

let invalidFiles =
    Helpers.GetInvalidFiles
        targetDir
        "*.*"
        (fun fileInfo -> FileConventions.EolAtEof fileInfo = False)

Helpers.AssertNoInvalidFiles
    invalidFiles
    "The following files should end with EOL:"
