#!/usr/bin/env -S dotnet fsi

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers

#r "nuget: FSharp.Data, Version=5.0.2"
open FSharp.Data

#r "nuget: Fsdk, Version=0.9.99--date20260525-0605.git-a5cfc39"

open Fsdk
open Fsdk.Process
open Fsdk.FSharpUtil

let githubRepository = Environment.GetEnvironmentVariable "GITHUB_REPOSITORY"

if String.IsNullOrEmpty githubRepository then
    Console.Error.WriteLine
        "This script is meant to be used only within a GitHubCI pipeline."

    Environment.Exit 2

let githubToken = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

if String.IsNullOrEmpty githubToken then
    Console.Error.WriteLine
        "Please set the GITHUB_TOKEN environment variable in your GitHubCI pipeline:
    env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    "

    Environment.Exit 2

let numberOfLatestReleasesToKeep = 2

let gitTags =
    Fsdk
        .Process
        .Execute(
            {
                Command = "git"
                Arguments = "tag --sort=creatordate"
            },
            Echo.Off
        )
        .UnwrapDefault()
        .Trim()
        .Split(Environment.NewLine)

let gitTagsStr = String.Join(",", gitTags)
Console.WriteLine(sprintf "Existing tags in the repository: %s" gitTagsStr)

let gitTagsToRemove =
    gitTags
    |> Array.rev
    |> (fun gitTags -> gitTags.[numberOfLatestReleasesToKeep..])

if Seq.isEmpty gitTagsToRemove then
    Console.WriteLine "No need to remove any assets from tags."
else
    let gitTagsToRemoveStr = String.Join(",", gitTagsToRemove)

    Console.WriteLine(
        sprintf "Tags to be purged of assets: %s" gitTagsToRemoveStr
    )

type GitHubAssetType =
    JsonProvider<"""{
  "url": "https://api.github.com/repos/aaarani/RunIntoMe/releases/100938445",
  "assets_url": "https://api.github.com/repos/aaarani/RunIntoMe/releases/100938445/assets",
  "upload_url": "https://uploads.github.com/repos/aaarani/RunIntoMe/releases/100938445/assets{?name,label}",
  "html_url": "https://github.com/aaarani/RunIntoMe/releases/tag/0.19",
  "id": 100938445,
  "author": {
    "login": "github-actions[bot]",
    "id": 41898282,
    "node_id": "MDM6Qm90NDE4OTgyODI=",
    "avatar_url": "https://avatars.githubusercontent.com/in/15368?v=4",
    "gravatar_id": "",
    "url": "https://api.github.com/users/github-actions%5Bbot%5D",
    "html_url": "https://github.com/apps/github-actions",
    "followers_url": "https://api.github.com/users/github-actions%5Bbot%5D/followers",
    "following_url": "https://api.github.com/users/github-actions%5Bbot%5D/following{/other_user}",
    "gists_url": "https://api.github.com/users/github-actions%5Bbot%5D/gists{/gist_id}",
    "starred_url": "https://api.github.com/users/github-actions%5Bbot%5D/starred{/owner}{/repo}",
    "subscriptions_url": "https://api.github.com/users/github-actions%5Bbot%5D/subscriptions",
    "organizations_url": "https://api.github.com/users/github-actions%5Bbot%5D/orgs",
    "repos_url": "https://api.github.com/users/github-actions%5Bbot%5D/repos",
    "events_url": "https://api.github.com/users/github-actions%5Bbot%5D/events{/privacy}",
    "received_events_url": "https://api.github.com/users/github-actions%5Bbot%5D/received_events",
    "type": "Bot",
    "site_admin": false
  },
  "node_id": "RE_kwDOJbCwLc4GBDLN",
  "tag_name": "0.19",
  "target_commitish": "18f22bbfc8e7d9987518b41c6c19f93493e1967a",
  "name": "Release 0.19",
  "draft": false,
  "prerelease": false,
  "created_at": "2023-04-26T10:10:01Z",
  "published_at": "2023-04-26T11:22:47Z",
  "assets": [
    {
      "url": "https://api.github.com/repos/aaarani/RunIntoMe/releases/assets/105368692",
      "id": 105368692,
      "node_id": "RA_kwDOJbCwLc4GR8x0",
      "name": "com.companyname.runintome-Signed.apk",
      "label": "",
      "uploader": {
        "login": "github-actions[bot]",
        "id": 41898282,
        "node_id": "MDM6Qm90NDE4OTgyODI=",
        "avatar_url": "https://avatars.githubusercontent.com/in/15368?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/github-actions%5Bbot%5D",
        "html_url": "https://github.com/apps/github-actions",
        "followers_url": "https://api.github.com/users/github-actions%5Bbot%5D/followers",
        "following_url": "https://api.github.com/users/github-actions%5Bbot%5D/following{/other_user}",
        "gists_url": "https://api.github.com/users/github-actions%5Bbot%5D/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/github-actions%5Bbot%5D/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/github-actions%5Bbot%5D/subscriptions",
        "organizations_url": "https://api.github.com/users/github-actions%5Bbot%5D/orgs",
        "repos_url": "https://api.github.com/users/github-actions%5Bbot%5D/repos",
        "events_url": "https://api.github.com/users/github-actions%5Bbot%5D/events{/privacy}",
        "received_events_url": "https://api.github.com/users/github-actions%5Bbot%5D/received_events",
        "type": "Bot",
        "site_admin": false
      },
      "content_type": "application/vnd.android.package-archive",
      "state": "uploaded",
      "size": 48707293,
      "download_count": 0,
      "created_at": "2023-04-26T11:34:22Z",
      "updated_at": "2023-04-26T11:34:23Z",
      "browser_download_url": "https://github.com/aaarani/RunIntoMe/releases/download/0.19/com.companyname.runintome-Signed.apk"
    },
    {
      "url": "https://api.github.com/repos/aaarani/RunIntoMe/releases/assets/105366781",
      "id": 105366781,
      "node_id": "RA_kwDOJbCwLc4GR8T9",
      "name": "runIntoMe-db.zip",
      "label": "",
      "uploader": {
        "login": "github-actions[bot]",
        "id": 41898282,
        "node_id": "MDM6Qm90NDE4OTgyODI=",
        "avatar_url": "https://avatars.githubusercontent.com/in/15368?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/github-actions%5Bbot%5D",
        "html_url": "https://github.com/apps/github-actions",
        "followers_url": "https://api.github.com/users/github-actions%5Bbot%5D/followers",
        "following_url": "https://api.github.com/users/github-actions%5Bbot%5D/following{/other_user}",
        "gists_url": "https://api.github.com/users/github-actions%5Bbot%5D/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/github-actions%5Bbot%5D/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/github-actions%5Bbot%5D/subscriptions",
        "organizations_url": "https://api.github.com/users/github-actions%5Bbot%5D/orgs",
        "repos_url": "https://api.github.com/users/github-actions%5Bbot%5D/repos",
        "events_url": "https://api.github.com/users/github-actions%5Bbot%5D/events{/privacy}",
        "received_events_url": "https://api.github.com/users/github-actions%5Bbot%5D/received_events",
        "type": "Bot",
        "site_admin": false
      },
      "content_type": "application/zip",
      "state": "uploaded",
      "size": 369,
      "download_count": 0,
      "created_at": "2023-04-26T11:22:50Z",
      "updated_at": "2023-04-26T11:22:50Z",
      "browser_download_url": "https://github.com/aaarani/RunIntoMe/releases/download/0.19/runIntoMe-db.zip"
    },
    {
      "url": "https://api.github.com/repos/aaarani/RunIntoMe/releases/assets/105366778",
      "id": 105366778,
      "node_id": "RA_kwDOJbCwLc4GR8T6",
      "name": "runIntoMe-server-linux-amd64.zip",
      "label": "",
      "uploader": {
        "login": "github-actions[bot]",
        "id": 41898282,
        "node_id": "MDM6Qm90NDE4OTgyODI=",
        "avatar_url": "https://avatars.githubusercontent.com/in/15368?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/github-actions%5Bbot%5D",
        "html_url": "https://github.com/apps/github-actions",
        "followers_url": "https://api.github.com/users/github-actions%5Bbot%5D/followers",
        "following_url": "https://api.github.com/users/github-actions%5Bbot%5D/following{/other_user}",
        "gists_url": "https://api.github.com/users/github-actions%5Bbot%5D/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/github-actions%5Bbot%5D/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/github-actions%5Bbot%5D/subscriptions",
        "organizations_url": "https://api.github.com/users/github-actions%5Bbot%5D/orgs",
        "repos_url": "https://api.github.com/users/github-actions%5Bbot%5D/repos",
        "events_url": "https://api.github.com/users/github-actions%5Bbot%5D/events{/privacy}",
        "received_events_url": "https://api.github.com/users/github-actions%5Bbot%5D/received_events",
        "type": "Bot",
        "site_admin": false
      },
      "content_type": "application/zip",
      "state": "uploaded",
      "size": 42104852,
      "download_count": 0,
      "created_at": "2023-04-26T11:22:48Z",
      "updated_at": "2023-04-26T11:22:49Z",
      "browser_download_url": "https://github.com/aaarani/RunIntoMe/releases/download/0.19/runIntoMe-server-linux-amd64.zip"
    }
  ],
  "tarball_url": "https://api.github.com/repos/aaarani/RunIntoMe/tarball/0.19",
  "zipball_url": "https://api.github.com/repos/aaarani/RunIntoMe/zipball/0.19",
  "body": "Backend"
}""">

let userAgent = ".NET App"
let xGitHubApiVersion = "2022-11-28"

let githubApiCallForbiddenErrorMsg =
    """GITHUB_TOKEN passed doesn't seem to have enough permissions.
To modify the permissions of your token, navigate to the Settings section of your 
repository or organization and click on Actions button, then select General. From 
'Workflow permissions' section on that page, choose 'Read and write permissions' 
(which grants access to content and the ability to make changes).
"""

gitTagsToRemove
|> Seq.iter(fun gitTagToRemove ->
    let maybeReleaseJsonString: Option<string> =
        let mediaTypeWithQuality =
            MediaTypeWithQualityHeaderValue "application/vnd.github+json"

        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Clear()
        client.DefaultRequestHeaders.Accept.Add mediaTypeWithQuality
        client.DefaultRequestHeaders.Add("User-Agent", userAgent)

        client.DefaultRequestHeaders.Add(
            "X-GitHub-Api-Version",
            xGitHubApiVersion
        )

        if not(String.IsNullOrEmpty githubToken) then
            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {githubToken}"
            )

        let url =
            $"https://api.github.com/repos/{githubRepository}/releases/tags/{gitTagToRemove}"

        try
            let task = client.GetStringAsync url
            let result = Async.AwaitTask task |> Async.RunSynchronously
            Some result
        with
        | ex ->
            match FindException<HttpRequestException> ex with
            | Some httpRequestException ->
                match Option.ofNullable httpRequestException.StatusCode with
                | Some statusCode when statusCode = HttpStatusCode.Forbidden ->

                    failwith githubApiCallForbiddenErrorMsg

                | Some statusCode when statusCode = HttpStatusCode.NotFound ->
                    None

                | _ -> reraise()

            | _ -> reraise()

    let deleteAsset (url: string) (accessToken: string) =
        use client = new HttpClient()
        client.DefaultRequestHeaders.Accept.Clear()
        client.DefaultRequestHeaders.Add("User-Agent", userAgent)

        client.DefaultRequestHeaders.Add(
            "X-GitHub-Api-Version",
            xGitHubApiVersion
        )

        if not(String.IsNullOrEmpty accessToken) then
            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {accessToken}"
            )

        try
            let task = client.DeleteAsync url

            Async.AwaitTask task
            |> Async.RunSynchronously
            |> ignore<HttpResponseMessage>

        with
        | ex ->
            match FindException<HttpRequestException> ex with
            | Some httpRequestException ->
                match Option.ofNullable httpRequestException.StatusCode with
                | Some statusCode when statusCode = HttpStatusCode.Forbidden ->
                    failwith githubApiCallForbiddenErrorMsg
                | _ -> reraise()
            | _ -> reraise()

    match maybeReleaseJsonString with
    | Some releaseJsonString ->
        let parsedJsonObj = GitHubAssetType.Parse releaseJsonString

        parsedJsonObj.Assets
        |> Seq.iter(fun asset -> deleteAsset asset.Url githubToken)
    | _ -> ()
)
