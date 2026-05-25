#!/usr/bin/env -S dotnet fsi

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process

let gitRemote =
    {
        Command = "git"
        Arguments = "remote -v"
    }

let gitRemoteOutput =
    Process
        .Execute(gitRemote, Echo.All)
        .UnwrapDefault()
