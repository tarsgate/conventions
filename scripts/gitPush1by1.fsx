#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Linq
open System.Threading

#r "System.Configuration"
open System.Configuration

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process

let errTooManyArgs =
    (1,
     $"Usage: dotnet fsi {__SOURCE_FILE__} [remoteName] [numberOfCommits] [-f|--force]")

let errSecondArgShouldBeIntHigherThanZero =
    (2, "Second argument should be an integer higher than zero")

let errSecondArgShouldBeInteger = (3, "Second argument should be an integer")

let ErrRemoteNotFound remoteNameProvided =
    (4, sprintf "Remote '%s' not found" remoteNameProvided)

let ErrChooseRemoteNameToAvoidPushingNonWipBranchToCanonicalRemote
    currentBranch
    =
    (5,
     sprintf
         "You're creating a non-wip branch '%s' on a non-fork repo, please supply the remoteName to avoid pushing non-wip branches to the canonical upstream."
         currentBranch)

let errNoRemotesFound = (6, "No remotes found, please add one first.")

let errMultipleRemotesFoundPleaseSupplyRemoteNameParam =
    (7, "Multiple remotes found, please supply remoteName as first argument.")

let ErrCurrentBranchIsUpToDateSoMaybeForcePush currentBranch remote =
    (8,
     sprintf
         "Current branch '%s' in remote '%s' is already up to date. Force push by specifying number of commits as 2nd argument?"
         currentBranch
         remote)

// mimic https://stackoverflow.com/a/3230241/544947
let GitSpecificPush
    (remoteName: string)
    (commitSha: string)
    (remoteBranchName: string)
    (force: bool)
    =
    let forceFlag =
        if force then
            "--force"
        else
            "--force-with-lease"

    let gitPush =
        {
            Command = "git"
            Arguments =
                sprintf
                    "push %s %s:refs/heads/%s %s"
                    remoteName
                    commitSha
                    remoteBranchName
                    forceFlag
        }

    let pushProc = Process.Execute(gitPush, Echo.OutputOnly)

    match pushProc.Result with
    | Error _ -> failwith "Push failed ^"
    | WarningsOrAmbiguous _
    | Success _ -> ()

let GitFetch(remoteOpt: Option<string>) =
    let remoteArg =
        match remoteOpt with
        | None -> "--all"
        | Some remote -> remote

    let gitFetch =
        {
            Command = "git"
            Arguments = sprintf "fetch %s" remoteArg
        }

    let fetchProc = Process.Execute(gitFetch, Echo.OutputOnly)

    match fetchProc.Result with
    | Error _ -> failwith "Fetch failed ^"
    | WarningsOrAmbiguous _
    | Success _ -> ()

let GetLastNthCommitFromRemoteBranch
    (remoteName: string)
    (remoteBranch: string)
    (commitNumber: uint32)
    =
    let gitShow =
        {
            Command = "git"
            Arguments =
                sprintf
                    "show %s/%s~%i --no-patch"
                    remoteName
                    remoteBranch
                    commitNumber
        }

    let gitShowProcOutput = Process.Execute(gitShow, Echo.Off).UnwrapDefault()

    let firstLine =
        (Misc.CrossPlatformStringSplitInLines gitShowProcOutput)
            .First()

    // split this line: commit 938634a3e7d4dc7e6dd357927a16165120bbea68 (HEAD -> master, origin/master, origin/HEAD)
    let commitHash = firstLine.Split([| " " |], StringSplitOptions.None).[1]
    commitHash

let FindUnpushedCommits (remoteName: string) (remoteBranch: string) =
    let rec findUnpushedCommits
        localCommitsWalkedSoFar
        currentSkipCount
        remoteCommits
        =
        let rec findIntersection localCommits (remoteCommits: List<string>) =
            match localCommits with
            | [] -> None
            | head :: tail ->
                if remoteCommits.Contains head then
                    Some tail
                else
                    findIntersection tail remoteCommits

        Console.WriteLine "Walking tree..."

        let currentHash =
            Process
                .Execute(
                    {
                        Command = "git"
                        Arguments =
                            sprintf
                                "log -1 --skip=%i --format=format:%%H"
                                currentSkipCount
                    },
                    Echo.Off
                )
                .UnwrapDefault()
                .Trim()

        let newRemoteCommits =
            (GetLastNthCommitFromRemoteBranch
                remoteName
                remoteBranch
                currentSkipCount)
            :: remoteCommits

        let newLocalCommitsWalkedSoFar = currentHash :: localCommitsWalkedSoFar

        match findIntersection newLocalCommitsWalkedSoFar newRemoteCommits with
        | Some theCommitsToPush -> theCommitsToPush
        | None ->
            findUnpushedCommits
                newLocalCommitsWalkedSoFar
                (currentSkipCount + 1u)
                newRemoteCommits

    GitFetch(Some remoteName)
    findUnpushedCommits List.empty 0u List.empty

let GetLastCommits(count: UInt32) =
    let rec getLastCommits commitsFoundSoFar currentSkipCount currentCount =
        if currentCount = 0u then
            commitsFoundSoFar
        else
            let currentHash =
                Process
                    .Execute(
                        {
                            Command = "git"
                            Arguments =
                                sprintf
                                    "log -1 --skip=%i --format=format:%%H"
                                    currentSkipCount
                        },
                        Echo.Off
                    )
                    .UnwrapDefault()
                    .Trim()

            getLastCommits
                (currentHash :: commitsFoundSoFar)
                (currentSkipCount + 1u)
                (currentCount - 1u)

    getLastCommits List.empty 0u count

let remotes = Git.GetRemotes()

let rawArgs = Misc.FsxOnlyArguments()

let forceFlag =
    {|
        LongName = "--force"
        ShortName = "-f"
    |}

let forceEnabled =
    rawArgs
    |> List.exists(fun arg ->
        arg = forceFlag.ShortName || arg = forceFlag.LongName
    )

let nonFlagArgs =
    rawArgs
    |> List.filter(fun arg ->
        arg <> forceFlag.ShortName && arg <> forceFlag.LongName
    )

if nonFlagArgs.Length > 2 then
    let exitCode, errMsg = errTooManyArgs
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode

let maybeRemote, maybeNumberOfCommits =
    if nonFlagArgs.Length > 1 then
        match UInt32.TryParse nonFlagArgs.[1] with
        | true, 0u ->
            let exitCode, errMsg = errSecondArgShouldBeIntHigherThanZero
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode

            failwith <| "Unreachable because of: " + errMsg
        | true, num ->
            let numberOfCommits = Some num
            let remote = Some nonFlagArgs.[0]

            remote, numberOfCommits
        | _ ->
            let exitCode, errMsg = errSecondArgShouldBeInteger
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode

            failwith <| "Unreachable because of: " + errMsg
    elif nonFlagArgs.Length = 0 then
        None, None
    else // if nonFlagArgs.Length = 1 then
        match UInt32.TryParse nonFlagArgs.[0] with
        | true, 0u ->
            let exitCode, errMsg = errSecondArgShouldBeIntHigherThanZero
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode

            failwith <| "Unreachable because of: " + errMsg
        | true, num ->
            let numberOfCommits = Some num
            let remote = None
            remote, numberOfCommits
        | _ ->
            let numberOfCommits = None
            let remote = Some(nonFlagArgs.[0])
            remote, numberOfCommits

let currentBranch = Git.GetCurrentBranch()

let remote, remoteUrl =
    match maybeRemote with
    | Some remoteProvided ->
        match
            Seq.tryFind
                (fun (currentRemote, _) -> currentRemote = remoteProvided)
                remotes
            with
        | None ->
            let exitCode, errMsg = ErrRemoteNotFound remoteProvided
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg
        | Some remote -> remote
    | None ->
        match Seq.toList remotes with
        | (onlyRemote, onlyRemoteUrl) :: [] ->
            if onlyRemote <> "origin" && onlyRemote <> "upstream" then
                onlyRemote, onlyRemoteUrl
            else if currentBranch.StartsWith "wip" then
                onlyRemote, onlyRemoteUrl
            else
                let exitCode, errMsg =
                    ErrChooseRemoteNameToAvoidPushingNonWipBranchToCanonicalRemote
                        currentBranch

                Console.Error.WriteLine errMsg
                Environment.Exit exitCode
                failwith <| "Unreachable because of: " + errMsg
        | [] ->
            let exitCode, errMsg = errNoRemotesFound
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg
        | _ ->
            let exitCode, errMsg =
                errMultipleRemotesFoundPleaseSupplyRemoteNameParam

            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

let commitsToBePushed =
    match maybeNumberOfCommits with
    | None ->
        let commitsToPush = FindUnpushedCommits remote currentBranch

        if commitsToPush.Length = 0 then
            let exitCode, errMsg =
                ErrCurrentBranchIsUpToDateSoMaybeForcePush currentBranch remote

            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg
        elif commitsToPush.Length = 1 then
            // no need to ask for confirmation since 1 commit doesn't need to be separated from other commits
            // (one by one doesn't apply to a length of one)
            commitsToPush
        else //if commitsToPush.Length > 1 then
            Console.WriteLine(
                sprintf
                    "Detected a delta of %i commits between local branch '%s' and the one in remote '%s', to be pushed one by one. Press any key to continue or CTRL+C to abort."
                    commitsToPush.Length
                    currentBranch
                    remote
            )

            Console.ReadKey true |> ignore<ConsoleKeyInfo>
            Console.WriteLine "Pushing..."
            commitsToPush
    | Some numberOfCommits -> GetLastCommits numberOfCommits

let numberOfCommitsToPush = commitsToBePushed.Length

for commit in commitsToBePushed do
    GitSpecificPush remote commit currentBranch forceEnabled

    // sleep a bit between pushes, otherwise CI statuses for a PR would just be
    // collapsed into a single CI status at the last commit only
    2.0 |> TimeSpan.FromSeconds |> System.Threading.Thread.Sleep

if numberOfCommitsToPush > 1 && remoteUrl.Contains "gitlab" then
    Console.WriteLine
        "NOTE: if you have issues with pipelines being canceled, visit https://gitlab.com/yourUserNameOrOrgName/yourRepoName/-/settings/ci_cd"

    Console.WriteLine
        "then click 'Expand' on 'General Pipelines', and uncheck 'Auto-cancel redundant pipelines'"
