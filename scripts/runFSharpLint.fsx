#!/usr/bin/env -S dotnet fsi

open System.IO
open System.Linq

#r "nuget: Fsdk, Version=0.9.99--date20260615-1007.git-0e932e5"

open Fsdk

#load "../src/FileConventions/Helpers.fs"

let version = "0.27.1--date20260628-0821.git-91fd00b"

let targetSol =
    let args = Fsdk.Misc.FsxOnlyArguments()

    match args with
    | [] -> None
    | head :: [] -> Some head
    | _head :: _tail -> failwithf "Too many arguments: %A" args

let rootDir = Path.Combine(__SOURCE_DIRECTORY__, "..") |> DirectoryInfo
let currentDir = Directory.GetCurrentDirectory() |> DirectoryInfo

let targetDir, fallbackDir =
    Helpers.PreferLessDeeplyNestedDir currentDir rootDir

let targetSolution =
    match targetSol with
    | Some solFilename ->
        let solFile = Path.Combine(targetDir.FullName, solFilename) |> FileInfo

        if not solFile.Exists then
            let solFile =
                Path.Combine(fallbackDir.FullName, solFilename) |> FileInfo

            if not solFile.Exists then
                failwithf
                    "Solution file %s not found in %s or %s"
                    solFilename
                    targetDir.FullName
                    fallbackDir.FullName
            else
                printfn
                    "Using solution file %s from fallback directory %s"
                    solFilename
                    fallbackDir.FullName

                solFile
        else
            printfn
                "Using solution file %s from target directory %s"
                solFilename
                targetDir.FullName

            solFile
    | None ->
        let solFiles = Directory.GetFiles(targetDir.FullName, "*.sln")

        let getMultipleSolutionsFoundErrorMessage dirName =
            sprintf
                "Multiple solution files found in %s; please specify one as an argument"
                dirName

        if solFiles.Length = 0 then
            let solFiles = Directory.GetFiles(fallbackDir.FullName, "*.sln")

            if solFiles.Length = 0 then
                failwithf
                    "No solution file found in %s or %s"
                    targetDir.FullName
                    fallbackDir.FullName
            elif solFiles.Length > 1 then
                failwith
                <| getMultipleSolutionsFoundErrorMessage fallbackDir.FullName
            else
                let solFile = FileInfo solFiles.[0]

                printfn
                    "Using solution file %s from target directory %s"
                    solFile.Name
                    fallbackDir.FullName

                solFile
        elif solFiles.Length > 1 then
            failwith <| getMultipleSolutionsFoundErrorMessage targetDir.FullName
        else
            let solFile = FileInfo solFiles.[0]

            printfn
                "Using solution file %s from target directory %s"
                solFile.Name
                targetDir.FullName

            solFile

let fsharpLintConfigFileName = "fsharplint.json"

let fsharpLintDefaultConfigFile =
    Path.Combine(rootDir.FullName, fsharpLintConfigFileName) |> FileInfo

let fsharpLintConfigFile =
    Path.Combine(targetSolution.Directory.FullName, fsharpLintConfigFileName)
    |> FileInfo

if fsharpLintDefaultConfigFile.Exists && not fsharpLintConfigFile.Exists then
    fsharpLintDefaultConfigFile.CopyTo(fsharpLintConfigFile.FullName, false)
    |> ignore<FileInfo>

Process
    .ExecDefault("dotnet new tool-manifest --force")
    .UnwrapDefault()
|> ignore<string>

Process
    .ExecDefault(
        sprintf "dotnet tool install dotnet-fsharplint --version %s" version
    )
    .UnwrapDefault()
|> ignore<string>

Process
    .ExecDefault(sprintf "dotnet build %s" targetSolution.FullName)
    .UnwrapDefault()
|> ignore<string>

let RunFSharpLint(target: FileInfo) =
    Process.ExecDefault(
        sprintf "dotnet dotnet-fsharplint lint %s" target.FullName
    )

RunFSharpLint(targetSolution).UnwrapDefault() |> ignore<string>

// check .fsx files in target dir
let allFiles = Fsdk.Misc.GetAllFilesRecursively targetDir |> Seq.map FileInfo

let fsxFiles =
    allFiles |> Seq.filter(fun filename -> filename.Extension = ".fsx")

let results = fsxFiles |> Seq.map RunFSharpLint

let violationsExist =
    results
    |> Seq.exists(fun result ->
        match result.Result with
        | Process.Success _ -> false
        | _ -> true
    )

if violationsExist then
    failwith "One or more fsharplint runs on .fsx files failed"
