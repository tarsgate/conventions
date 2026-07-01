#!/usr/bin/env -S dotnet fsi

#r "nuget: Fsdk, Version=0.9.99--date20260618-1029.git-79ec1be"

open System.IO

open Fsdk
open Fsdk.Process

let InstallProvider (name: string) (version: string) =
    let providerName = $"pulumi-{name}"

    let providersDir =
        Directory.CreateDirectory
        <| Path.Combine("/usr", "local", "pulumi-providers")

    let providerDir =
        Directory.CreateDirectory
        <| Path.Combine(providersDir.FullName, providerName)

    let initialCurrentDirectory = System.Environment.CurrentDirectory
    System.Environment.CurrentDirectory <- providerDir.FullName

    let wgetCommandResult =
        Process.Execute(
            {
                Command = "wget"
                Arguments =
                    $"https://github.com/nodeeffect/{providerName}/releases/download/{version}/{providerName}.zip"
            },
            Echo.All
        )

    match wgetCommandResult.Result with
    | Success _
    | WarningsOrAmbiguous _ -> ()
    | Error(exitCode, _) ->
        printfn "wget command failed with exit code %i" exitCode
        exit 3

    let zipFileName = $"{providerName}.zip"

    Process
        .Execute(
            {
                Command = "unzip"
                Arguments = zipFileName
            },
            Echo.All
        )
        .UnwrapDefault()
    |> ignore<string>

    File.Delete zipFileName

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
                    Arguments = "cp ./bin/pulumi-resource-bitlaunch /usr/bin"
                },
                Echo.All
            )
            .UnwrapDefault()
        |> ignore<string>
    | _ -> ()

    System.Environment.CurrentDirectory <- initialCurrentDirectory

let args = Misc.FsxOnlyArguments()

let usage =
    $"Usage: dotnet fsi {__SOURCE_FILE__} <provider name> <provider version>"

match args with
| [ providerName; version ] -> InstallProvider providerName version
| _ ->
    printfn "%s" usage
    exit 1
