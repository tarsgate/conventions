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

    // Avoid error: Access to the path '/home/runner/work/pulumi-deploy/pulumi-deploy/pulumi-bitlaunch/sdk/dotnet/obj/d3c3f3c5-9946-497b-8a7e-17f0b6501f6f.tmp' is denied. [/home/runner/work/pulumi-deploy/pulumi-deploy/GithubRunner/GithubRunner.fsproj]
    Process
        .Execute(
            {
                Command = "chmod"
                Arguments = $"--recursive 0777 ./sdk/dotnet"
            },
            Echo.All
        )
        .UnwrapDefault()
    |> ignore<string>

    match name with
    | "bitlaunch" ->
        Process
            .Execute(
                {
                    Command = "sudo"
                    Arguments =
                        $"cp {providerDir.FullName}/bin/pulumi-resource-bitlaunch /usr/bin"
                },
                Echo.All
            )
            .UnwrapDefault()
        |> ignore<string>
    | _ -> ()

let args = Misc.FsxOnlyArguments()

match args with
| [ providerName; version ] -> InstallProvider providerName version
| _ ->
    let exitCode, errMsg = errorUsage
    printfn "%s" errMsg
    exit exitCode
