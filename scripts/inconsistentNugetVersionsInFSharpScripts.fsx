#!/usr/bin/env -S dotnet fsi

open System.IO
open System.Linq

#r "nuget: FSharpx.Collections, Version=3.1.0"
#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"
#load "../src/FileConventions/Library.fs"
#load "../src/FileConventions/Helpers.fs"

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo
let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let targetDir, _ = Helpers.PreferLessDeeplyNestedDir currentDir rootDir

let inconsistentVersionsInFsharpScripts =
    Helpers.GetFiles targetDir "*.fsx"
    |> Seq.filter(fun file ->
        targetDir = rootDir
        || not(file.Directory.FullName.StartsWith rootDir.FullName)
    )
    |> FileConventions.DetectInconsistentVersionsInNugetRefsInFSharpScripts

if inconsistentVersionsInFsharpScripts then
    failwith
        "You shouldn't use inconsistent versions in nuget package references of F# scripts."
