#!/usr/bin/env -S dotnet fsi

#r "nuget: Fsdk, Version=0.9.99--date20260618-1029.git-79ec1be"

open System
open System.IO

open Fsdk
open Fsdk.Process

let usage =
    $"Usage: dotnet fsi {__SOURCE_FILE__} <provider name> <provider version>"

let errorUsage = 1, usage

let ErrorWget wgetExitCode =
    2, $"wget command failed with exit code %i{wgetExitCode}"

let InstallProvider (name: string) (version: string) =
    let providerName = $"pulumi-{name}"

    let providersDir =
        Directory.CreateDirectory
        <| Path.Combine("/usr", "local", "pulumi-providers")

    let providerDir =
        Directory.CreateDirectory
        <| Path.Combine(providersDir.FullName, providerName)

    let providerZipFile =
        FileInfo <| Path.Combine(providerDir.FullName, $"{providerName}.zip")

    let wgetCommandResult =
        Process.Execute(
            {
                Command = "wget"
                Arguments =
                    $"--output-document={providerZipFile.FullName} https://github.com/nodeeffect/{providerName}/releases/download/{version}/{providerName}.zip"
            },
            Echo.All
        )

    match wgetCommandResult.Result with
    | Success _
    | WarningsOrAmbiguous _ -> ()
    | Error(wgetExitCode, _) ->
        let exitCode, errMsg = ErrorWget wgetExitCode
        printfn "%s" errMsg
        exit exitCode

    Process
        .ExecDefault(
            "unzip {providerZipFile.FullName} -d {providerDir}",
            Echo.All
        )
        .UnwrapDefault()
    |> ignore<string>

    providerZipFile.Delete()

    match name with
    | "bitlaunch" ->
        let bitlaunchBinaryName = "pulumi-resource-bitlaunch"

        File.Copy(
            Path.Join(providerDir.FullName, "bin", bitlaunchBinaryName),
            Path.Join("/usr/bin", bitlaunchBinaryName)
        )
    | _ -> ()

let args = Misc.FsxOnlyArguments()

match args with
| [ providerName; version ] -> InstallProvider providerName version
| _ ->
    let exitCode, errMsg = errorUsage
    printfn "%s" errMsg
    exit exitCode
