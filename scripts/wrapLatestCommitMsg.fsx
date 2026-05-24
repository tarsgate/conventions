#!/usr/bin/env -S dotnet fsi

open System.IO
open System

#r "nuget: Mono.Unix, Version=7.1.0-final.1.21458.1"
#r "nuget: YamlDotNet, Version=16.1.3"

#load "../src/FileConventions/Library.fs"

let filePath =
    if fsi.CommandLineArgs.Length < 2 then
        eprintfn "Usage: dotnet fsi wrapLatestCommitMsg.fsx <commit-msg-file>"
        Environment.Exit 1
        failwith "unreachable"
    else
        fsi.CommandLineArgs.[1]

let allLines = File.ReadAllLines filePath

let messageLines =
    allLines
    |> Seq.takeWhile(fun line -> not(line.StartsWith "#"))
    |> Seq.toList

let commentLines =
    allLines
    |> Seq.skipWhile(fun line -> not(line.StartsWith "#"))
    |> Seq.toList

let commitMsg =
    String
        .Join(Environment.NewLine, messageLines)
        .Trim()

// TODO: we should maybe just rather commit-lint instead of having these ad-hoc failures
let ExitProcWithError() =
    Environment.Exit 1

if FileConventions.HasEmDash commitMsg then
    eprintfn
        "Error: em-dash character (—) detected in commit message. Please replace it with a normal dash (-) if it is meant as a bullet points or a word-union, or use parentheses if it is meant as a real em-dash."

    ExitProcWithError()

let header, maybeBody =
    let singleEolToJustSeparateLines = 1u

    let lines =
        FileConventions.SplitByEOLs commitMsg singleEolToJustSeparateLines

    if lines.Length = 1 then
        commitMsg, None
    else
        let body = String.Join(Environment.NewLine, Seq.skip 2 lines)
        lines.[0], Some body

let headerMaxLength = 50

if header.Length > headerMaxLength then
    eprintfn
        $"Error: commit message title exceeds {headerMaxLength} characters (found {header.Length})."

    eprintfn $"Title: {header}"
    ExitProcWithError()

let maxCharsPerLine = 64

let maybeWrappedBody =
    match maybeBody with
    | Some body -> Some(FileConventions.SafeWrapText body maxCharsPerLine)
    | _ -> None

let newCommitMsg =
    match maybeWrappedBody with
    | Some wrappedBody ->
        header + Environment.NewLine + Environment.NewLine + wrappedBody
    | _ -> header

let outputLines =
    if not(String.IsNullOrWhiteSpace newCommitMsg) then
        newCommitMsg.Split([| Environment.NewLine |], StringSplitOptions.None)
        |> Seq.toList
    else
        List.Empty

File.WriteAllLines(filePath, outputLines @ commentLines)
