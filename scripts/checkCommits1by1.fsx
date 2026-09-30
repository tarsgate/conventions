#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Linq
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text.RegularExpressions

#r "nuget: FSharp.Data, Version=5.0.2"
open FSharp.Data

#r "nuget: Fsdk, Version=0.9.99--date20260618-1029.git-79ec1be"

open Fsdk
open Fsdk.Process
open Fsdk.FSharpUtil

let errNotGitHubCI =
    (1, "This script is meant to be used only within a GitHubCI pipeline")

let errNoAccessToken =
    (2,
     """Please define GITHUB_TOKEN environment variable in your GitHubCI workflow:

    env:
      GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
""")

let errNoGitHubActions =
    (3,
     """
Please activate GitHub Actions in your repository. Click on the
"Actions" tab at the top of the repository page and click on the
"I understand my workflows, go ahead and enable them" button.
""")

let errNotPushedOneByOne =
    (4,
     "Please push the commits one by one to make sure every commit has a CI status; using this script is recommended:"
     + Environment.NewLine
     + "https://github.com/nblockchain/conventions/blob/master/scripts/gitPush1by1.fsx")

let errCiNotSuccessful =
    (5,
     "CI is not successful (green) for one or more commits, or it passes when a failing test is expected.")

type GithubEventType =
    JsonProvider<"""
{
    "action": "opened",
    "number": 4,
    "pull_request": {
      "_links": {
        "comments": {
          "href": "https://api.github.com/repos/realmarv/conventions/issues/4/comments"
        },
        "commits": {
          "href": "https://api.github.com/repos/realmarv/conventions/pulls/4/commits"
        },
        "html": {
          "href": "https://github.com/realmarv/conventions/pull/4"
        },
        "issue": {
          "href": "https://api.github.com/repos/realmarv/conventions/issues/4"
        },
        "review_comment": {
          "href": "https://api.github.com/repos/realmarv/conventions/pulls/comments{/number}"
        },
        "review_comments": {
          "href": "https://api.github.com/repos/realmarv/conventions/pulls/4/comments"
        },
        "self": {
          "href": "https://api.github.com/repos/realmarv/conventions/pulls/4"
        },
        "statuses": {
          "href": "https://api.github.com/repos/realmarv/conventions/statuses/c5eb50ea6c5a72d61ab85d96ffc66cc4741d3133"
        }
      },
      "active_lock_reason": null,
      "additions": 21,
      "assignee": null,
      "assignees": [],
      "author_association": "OWNER",
      "auto_merge": null,
      "base": {
        "label": "realmarv:master",
        "ref": "master",
        "repo": {
          "allow_auto_merge": false,
          "allow_forking": true,
          "allow_merge_commit": true,
          "allow_rebase_merge": true,
          "allow_squash_merge": true,
          "allow_update_branch": false,
          "archive_url": "https://api.github.com/repos/realmarv/conventions/{archive_format}{/ref}",
          "archived": false,
          "assignees_url": "https://api.github.com/repos/realmarv/conventions/assignees{/user}",
          "blobs_url": "https://api.github.com/repos/realmarv/conventions/git/blobs{/sha}",
          "branches_url": "https://api.github.com/repos/realmarv/conventions/branches{/branch}",
          "clone_url": "https://github.com/realmarv/conventions.git",
          "collaborators_url": "https://api.github.com/repos/realmarv/conventions/collaborators{/collaborator}",
          "comments_url": "https://api.github.com/repos/realmarv/conventions/comments{/number}",
          "commits_url": "https://api.github.com/repos/realmarv/conventions/commits{/sha}",
          "compare_url": "https://api.github.com/repos/realmarv/conventions/compare/{base}...{head}",
          "contents_url": "https://api.github.com/repos/realmarv/conventions/contents/{+path}",
          "contributors_url": "https://api.github.com/repos/realmarv/conventions/contributors",
          "created_at": "2022-10-12T08:48:57Z",
          "default_branch": "master",
          "delete_branch_on_merge": false,
          "deployments_url": "https://api.github.com/repos/realmarv/conventions/deployments",
          "description": null,
          "disabled": false,
          "downloads_url": "https://api.github.com/repos/realmarv/conventions/downloads",
          "events_url": "https://api.github.com/repos/realmarv/conventions/events",
          "fork": true,
          "forks": 0,
          "forks_count": 0,
          "forks_url": "https://api.github.com/repos/realmarv/conventions/forks",
          "full_name": "realmarv/conventions",
          "git_commits_url": "https://api.github.com/repos/realmarv/conventions/git/commits{/sha}",
          "git_refs_url": "https://api.github.com/repos/realmarv/conventions/git/refs{/sha}",
          "git_tags_url": "https://api.github.com/repos/realmarv/conventions/git/tags{/sha}",
          "git_url": "git://github.com/realmarv/conventions.git",
          "has_discussions": false,
          "has_downloads": true,
          "has_issues": false,
          "has_pages": false,
          "has_projects": true,
          "has_wiki": true,
          "homepage": null,
          "hooks_url": "https://api.github.com/repos/realmarv/conventions/hooks",
          "html_url": "https://github.com/realmarv/conventions",
          "id": 550128772,
          "is_template": false,
          "issue_comment_url": "https://api.github.com/repos/realmarv/conventions/issues/comments{/number}",
          "issue_events_url": "https://api.github.com/repos/realmarv/conventions/issues/events{/number}",
          "issues_url": "https://api.github.com/repos/realmarv/conventions/issues{/number}",
          "keys_url": "https://api.github.com/repos/realmarv/conventions/keys{/key_id}",
          "labels_url": "https://api.github.com/repos/realmarv/conventions/labels{/name}",
          "language": "TypeScript",
          "languages_url": "https://api.github.com/repos/realmarv/conventions/languages",
          "license": {
            "key": "mit",
            "name": "MIT License",
            "node_id": "MDc6TGljZW5zZTEz",
            "spdx_id": "MIT",
            "url": "https://api.github.com/licenses/mit"
          },
          "merge_commit_message": "PR_TITLE",
          "merge_commit_title": "MERGE_MESSAGE",
          "merges_url": "https://api.github.com/repos/realmarv/conventions/merges",
          "milestones_url": "https://api.github.com/repos/realmarv/conventions/milestones{/number}",
          "mirror_url": null,
          "name": "conventions",
          "node_id": "R_kgDOIMpMhA",
          "notifications_url": "https://api.github.com/repos/realmarv/conventions/notifications{?since,all,participating}",
          "open_issues": 4,
          "open_issues_count": 4,
          "owner": {
            "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
            "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
            "followers_url": "https://api.github.com/users/realmarv/followers",
            "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
            "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
            "gravatar_id": "",
            "html_url": "https://github.com/realmarv",
            "id": 50144546,
            "login": "realmarv",
            "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
            "organizations_url": "https://api.github.com/users/realmarv/orgs",
            "received_events_url": "https://api.github.com/users/realmarv/received_events",
            "repos_url": "https://api.github.com/users/realmarv/repos",
            "site_admin": false,
            "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
            "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
            "type": "User",
            "url": "https://api.github.com/users/realmarv"
          },
          "private": false,
          "pulls_url": "https://api.github.com/repos/realmarv/conventions/pulls{/number}",
          "pushed_at": "2023-03-23T09:00:26Z",
          "releases_url": "https://api.github.com/repos/realmarv/conventions/releases{/id}",
          "size": 2395,
          "squash_merge_commit_message": "COMMIT_MESSAGES",
          "squash_merge_commit_title": "COMMIT_OR_PR_TITLE",
          "ssh_url": "git@github.com:realmarv/conventions.git",
          "stargazers_count": 0,
          "stargazers_url": "https://api.github.com/repos/realmarv/conventions/stargazers",
          "statuses_url": "https://api.github.com/repos/realmarv/conventions/statuses/{sha}",
          "subscribers_url": "https://api.github.com/repos/realmarv/conventions/subscribers",
          "subscription_url": "https://api.github.com/repos/realmarv/conventions/subscription",
          "svn_url": "https://github.com/realmarv/conventions",
          "tags_url": "https://api.github.com/repos/realmarv/conventions/tags",
          "teams_url": "https://api.github.com/repos/realmarv/conventions/teams",
          "topics": [],
          "trees_url": "https://api.github.com/repos/realmarv/conventions/git/trees{/sha}",
          "updated_at": "2022-10-13T08:53:39Z",
          "url": "https://api.github.com/repos/realmarv/conventions",
          "use_squash_pr_title_as_default": false,
          "visibility": "public",
          "watchers": 0,
          "watchers_count": 0,
          "web_commit_signoff_required": false
        },
        "sha": "0be2f2d608cdf893e146db1296dee750a2a54cfc",
        "user": {
          "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
          "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
          "followers_url": "https://api.github.com/users/realmarv/followers",
          "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
          "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
          "gravatar_id": "",
          "html_url": "https://github.com/realmarv",
          "id": 50144546,
          "login": "realmarv",
          "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
          "organizations_url": "https://api.github.com/users/realmarv/orgs",
          "received_events_url": "https://api.github.com/users/realmarv/received_events",
          "repos_url": "https://api.github.com/users/realmarv/repos",
          "site_admin": false,
          "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
          "type": "User",
          "url": "https://api.github.com/users/realmarv"
        }
      },
      "body": null,
      "changed_files": 2,
      "closed_at": null,
      "comments": 0,
      "comments_url": "https://api.github.com/repos/realmarv/conventions/issues/4/comments",
      "commits": 2,
      "commits_url": "https://api.github.com/repos/realmarv/conventions/pulls/4/commits",
      "created_at": "2023-03-23T09:00:26Z",
      "deletions": 0,
      "diff_url": "https://github.com/realmarv/conventions/pull/4.diff",
      "draft": false,
      "head": {
        "label": "realmarv:fixGitPush1by1Check",
        "ref": "fixGitPush1by1Check",
        "repo": {
          "allow_auto_merge": false,
          "allow_forking": true,
          "allow_merge_commit": true,
          "allow_rebase_merge": true,
          "allow_squash_merge": true,
          "allow_update_branch": false,
          "archive_url": "https://api.github.com/repos/realmarv/conventions/{archive_format}{/ref}",
          "archived": false,
          "assignees_url": "https://api.github.com/repos/realmarv/conventions/assignees{/user}",
          "blobs_url": "https://api.github.com/repos/realmarv/conventions/git/blobs{/sha}",
          "branches_url": "https://api.github.com/repos/realmarv/conventions/branches{/branch}",
          "clone_url": "https://github.com/realmarv/conventions.git",
          "collaborators_url": "https://api.github.com/repos/realmarv/conventions/collaborators{/collaborator}",
          "comments_url": "https://api.github.com/repos/realmarv/conventions/comments{/number}",
          "commits_url": "https://api.github.com/repos/realmarv/conventions/commits{/sha}",
          "compare_url": "https://api.github.com/repos/realmarv/conventions/compare/{base}...{head}",
          "contents_url": "https://api.github.com/repos/realmarv/conventions/contents/{+path}",
          "contributors_url": "https://api.github.com/repos/realmarv/conventions/contributors",
          "created_at": "2022-10-12T08:48:57Z",
          "default_branch": "master",
          "delete_branch_on_merge": false,
          "deployments_url": "https://api.github.com/repos/realmarv/conventions/deployments",
          "description": null,
          "disabled": false,
          "downloads_url": "https://api.github.com/repos/realmarv/conventions/downloads",
          "events_url": "https://api.github.com/repos/realmarv/conventions/events",
          "fork": true,
          "forks": 0,
          "forks_count": 0,
          "forks_url": "https://api.github.com/repos/realmarv/conventions/forks",
          "full_name": "realmarv/conventions",
          "git_commits_url": "https://api.github.com/repos/realmarv/conventions/git/commits{/sha}",
          "git_refs_url": "https://api.github.com/repos/realmarv/conventions/git/refs{/sha}",
          "git_tags_url": "https://api.github.com/repos/realmarv/conventions/git/tags{/sha}",
          "git_url": "git://github.com/realmarv/conventions.git",
          "has_discussions": false,
          "has_downloads": true,
          "has_issues": false,
          "has_pages": false,
          "has_projects": true,
          "has_wiki": true,
          "homepage": null,
          "hooks_url": "https://api.github.com/repos/realmarv/conventions/hooks",
          "html_url": "https://github.com/realmarv/conventions",
          "id": 550128772,
          "is_template": false,
          "issue_comment_url": "https://api.github.com/repos/realmarv/conventions/issues/comments{/number}",
          "issue_events_url": "https://api.github.com/repos/realmarv/conventions/issues/events{/number}",
          "issues_url": "https://api.github.com/repos/realmarv/conventions/issues{/number}",
          "keys_url": "https://api.github.com/repos/realmarv/conventions/keys{/key_id}",
          "labels_url": "https://api.github.com/repos/realmarv/conventions/labels{/name}",
          "language": "TypeScript",
          "languages_url": "https://api.github.com/repos/realmarv/conventions/languages",
          "license": {
            "key": "mit",
            "name": "MIT License",
            "node_id": "MDc6TGljZW5zZTEz",
            "spdx_id": "MIT",
            "url": "https://api.github.com/licenses/mit"
          },
          "merge_commit_message": "PR_TITLE",
          "merge_commit_title": "MERGE_MESSAGE",
          "merges_url": "https://api.github.com/repos/realmarv/conventions/merges",
          "milestones_url": "https://api.github.com/repos/realmarv/conventions/milestones{/number}",
          "mirror_url": null,
          "name": "conventions",
          "node_id": "R_kgDOIMpMhA",
          "notifications_url": "https://api.github.com/repos/realmarv/conventions/notifications{?since,all,participating}",
          "open_issues": 4,
          "open_issues_count": 4,
          "owner": {
            "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
            "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
            "followers_url": "https://api.github.com/users/realmarv/followers",
            "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
            "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
            "gravatar_id": "",
            "html_url": "https://github.com/realmarv",
            "id": 50144546,
            "login": "realmarv",
            "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
            "organizations_url": "https://api.github.com/users/realmarv/orgs",
            "received_events_url": "https://api.github.com/users/realmarv/received_events",
            "repos_url": "https://api.github.com/users/realmarv/repos",
            "site_admin": false,
            "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
            "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
            "type": "User",
            "url": "https://api.github.com/users/realmarv"
          },
          "private": false,
          "pulls_url": "https://api.github.com/repos/realmarv/conventions/pulls{/number}",
          "pushed_at": "2023-03-23T09:00:26Z",
          "releases_url": "https://api.github.com/repos/realmarv/conventions/releases{/id}",
          "size": 2395,
          "squash_merge_commit_message": "COMMIT_MESSAGES",
          "squash_merge_commit_title": "COMMIT_OR_PR_TITLE",
          "ssh_url": "git@github.com:realmarv/conventions.git",
          "stargazers_count": 0,
          "stargazers_url": "https://api.github.com/repos/realmarv/conventions/stargazers",
          "statuses_url": "https://api.github.com/repos/realmarv/conventions/statuses/{sha}",
          "subscribers_url": "https://api.github.com/repos/realmarv/conventions/subscribers",
          "subscription_url": "https://api.github.com/repos/realmarv/conventions/subscription",
          "svn_url": "https://github.com/realmarv/conventions",
          "tags_url": "https://api.github.com/repos/realmarv/conventions/tags",
          "teams_url": "https://api.github.com/repos/realmarv/conventions/teams",
          "topics": [],
          "trees_url": "https://api.github.com/repos/realmarv/conventions/git/trees{/sha}",
          "updated_at": "2022-10-13T08:53:39Z",
          "url": "https://api.github.com/repos/realmarv/conventions",
          "use_squash_pr_title_as_default": false,
          "visibility": "public",
          "watchers": 0,
          "watchers_count": 0,
          "web_commit_signoff_required": false
        },
        "sha": "c5eb50ea6c5a72d61ab85d96ffc66cc4741d3133",
        "user": {
          "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
          "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
          "followers_url": "https://api.github.com/users/realmarv/followers",
          "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
          "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
          "gravatar_id": "",
          "html_url": "https://github.com/realmarv",
          "id": 50144546,
          "login": "realmarv",
          "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
          "organizations_url": "https://api.github.com/users/realmarv/orgs",
          "received_events_url": "https://api.github.com/users/realmarv/received_events",
          "repos_url": "https://api.github.com/users/realmarv/repos",
          "site_admin": false,
          "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
          "type": "User",
          "url": "https://api.github.com/users/realmarv"
        }
      },
      "html_url": "https://github.com/realmarv/conventions/pull/4",
      "id": 1287033164,
      "issue_url": "https://api.github.com/repos/realmarv/conventions/issues/4",
      "labels": [],
      "locked": false,
      "maintainer_can_modify": false,
      "merge_commit_sha": null,
      "mergeable": null,
      "mergeable_state": "unknown",
      "merged": false,
      "merged_at": null,
      "merged_by": null,
      "milestone": null,
      "node_id": "PR_kwDOIMpMhM5MtpFM",
      "number": 4,
      "patch_url": "https://github.com/realmarv/conventions/pull/4.patch",
      "rebaseable": null,
      "requested_reviewers": [],
      "requested_teams": [],
      "review_comment_url": "https://api.github.com/repos/realmarv/conventions/pulls/comments{/number}",
      "review_comments": 0,
      "review_comments_url": "https://api.github.com/repos/realmarv/conventions/pulls/4/comments",
      "state": "open",
      "statuses_url": "https://api.github.com/repos/realmarv/conventions/statuses/c5eb50ea6c5a72d61ab85d96ffc66cc4741d3133",
      "title": "Fix git push1by1 check",
      "updated_at": "2023-03-23T09:00:26Z",
      "url": "https://api.github.com/repos/realmarv/conventions/pulls/4",
      "user": {
        "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
        "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
        "followers_url": "https://api.github.com/users/realmarv/followers",
        "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
        "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
        "gravatar_id": "",
        "html_url": "https://github.com/realmarv",
        "id": 50144546,
        "login": "realmarv",
        "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
        "organizations_url": "https://api.github.com/users/realmarv/orgs",
        "received_events_url": "https://api.github.com/users/realmarv/received_events",
        "repos_url": "https://api.github.com/users/realmarv/repos",
        "site_admin": false,
        "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
        "type": "User",
        "url": "https://api.github.com/users/realmarv"
      }
    },
    "repository": {
      "allow_forking": true,
      "archive_url": "https://api.github.com/repos/realmarv/conventions/{archive_format}{/ref}",
      "archived": false,
      "assignees_url": "https://api.github.com/repos/realmarv/conventions/assignees{/user}",
      "blobs_url": "https://api.github.com/repos/realmarv/conventions/git/blobs{/sha}",
      "branches_url": "https://api.github.com/repos/realmarv/conventions/branches{/branch}",
      "clone_url": "https://github.com/realmarv/conventions.git",
      "collaborators_url": "https://api.github.com/repos/realmarv/conventions/collaborators{/collaborator}",
      "comments_url": "https://api.github.com/repos/realmarv/conventions/comments{/number}",
      "commits_url": "https://api.github.com/repos/realmarv/conventions/commits{/sha}",
      "compare_url": "https://api.github.com/repos/realmarv/conventions/compare/{base}...{head}",
      "contents_url": "https://api.github.com/repos/realmarv/conventions/contents/{+path}",
      "contributors_url": "https://api.github.com/repos/realmarv/conventions/contributors",
      "created_at": "2022-10-12T08:48:57Z",
      "default_branch": "master",
      "deployments_url": "https://api.github.com/repos/realmarv/conventions/deployments",
      "description": null,
      "disabled": false,
      "downloads_url": "https://api.github.com/repos/realmarv/conventions/downloads",
      "events_url": "https://api.github.com/repos/realmarv/conventions/events",
      "fork": true,
      "forks": 0,
      "forks_count": 0,
      "forks_url": "https://api.github.com/repos/realmarv/conventions/forks",
      "full_name": "realmarv/conventions",
      "git_commits_url": "https://api.github.com/repos/realmarv/conventions/git/commits{/sha}",
      "git_refs_url": "https://api.github.com/repos/realmarv/conventions/git/refs{/sha}",
      "git_tags_url": "https://api.github.com/repos/realmarv/conventions/git/tags{/sha}",
      "git_url": "git://github.com/realmarv/conventions.git",
      "has_discussions": false,
      "has_downloads": true,
      "has_issues": false,
      "has_pages": false,
      "has_projects": true,
      "has_wiki": true,
      "homepage": null,
      "hooks_url": "https://api.github.com/repos/realmarv/conventions/hooks",
      "html_url": "https://github.com/realmarv/conventions",
      "id": 550128772,
      "is_template": false,
      "issue_comment_url": "https://api.github.com/repos/realmarv/conventions/issues/comments{/number}",
      "issue_events_url": "https://api.github.com/repos/realmarv/conventions/issues/events{/number}",
      "issues_url": "https://api.github.com/repos/realmarv/conventions/issues{/number}",
      "keys_url": "https://api.github.com/repos/realmarv/conventions/keys{/key_id}",
      "labels_url": "https://api.github.com/repos/realmarv/conventions/labels{/name}",
      "language": "TypeScript",
      "languages_url": "https://api.github.com/repos/realmarv/conventions/languages",
      "license": {
        "key": "mit",
        "name": "MIT License",
        "node_id": "MDc6TGljZW5zZTEz",
        "spdx_id": "MIT",
        "url": "https://api.github.com/licenses/mit"
      },
      "merges_url": "https://api.github.com/repos/realmarv/conventions/merges",
      "milestones_url": "https://api.github.com/repos/realmarv/conventions/milestones{/number}",
      "mirror_url": null,
      "name": "conventions",
      "node_id": "R_kgDOIMpMhA",
      "notifications_url": "https://api.github.com/repos/realmarv/conventions/notifications{?since,all,participating}",
      "open_issues": 4,
      "open_issues_count": 4,
      "owner": {
        "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
        "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
        "followers_url": "https://api.github.com/users/realmarv/followers",
        "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
        "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
        "gravatar_id": "",
        "html_url": "https://github.com/realmarv",
        "id": 50144546,
        "login": "realmarv",
        "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
        "organizations_url": "https://api.github.com/users/realmarv/orgs",
        "received_events_url": "https://api.github.com/users/realmarv/received_events",
        "repos_url": "https://api.github.com/users/realmarv/repos",
        "site_admin": false,
        "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
        "type": "User",
        "url": "https://api.github.com/users/realmarv"
      },
      "private": false,
      "pulls_url": "https://api.github.com/repos/realmarv/conventions/pulls{/number}",
      "pushed_at": "2023-03-23T09:00:26Z",
      "releases_url": "https://api.github.com/repos/realmarv/conventions/releases{/id}",
      "size": 2395,
      "ssh_url": "git@github.com:realmarv/conventions.git",
      "stargazers_count": 0,
      "stargazers_url": "https://api.github.com/repos/realmarv/conventions/stargazers",
      "statuses_url": "https://api.github.com/repos/realmarv/conventions/statuses/{sha}",
      "subscribers_url": "https://api.github.com/repos/realmarv/conventions/subscribers",
      "subscription_url": "https://api.github.com/repos/realmarv/conventions/subscription",
      "svn_url": "https://github.com/realmarv/conventions",
      "tags_url": "https://api.github.com/repos/realmarv/conventions/tags",
      "teams_url": "https://api.github.com/repos/realmarv/conventions/teams",
      "topics": [],
      "trees_url": "https://api.github.com/repos/realmarv/conventions/git/trees{/sha}",
      "updated_at": "2022-10-13T08:53:39Z",
      "url": "https://api.github.com/repos/realmarv/conventions",
      "visibility": "public",
      "watchers": 0,
      "watchers_count": 0,
      "web_commit_signoff_required": false
    },
    "sender": {
      "avatar_url": "https://avatars.githubusercontent.com/u/50144546?v=4",
      "events_url": "https://api.github.com/users/realmarv/events{/privacy}",
      "followers_url": "https://api.github.com/users/realmarv/followers",
      "following_url": "https://api.github.com/users/realmarv/following{/other_user}",
      "gists_url": "https://api.github.com/users/realmarv/gists{/gist_id}",
      "gravatar_id": "",
      "html_url": "https://github.com/realmarv",
      "id": 50144546,
      "login": "realmarv",
      "node_id": "MDQ6VXNlcjUwMTQ0NTQ2",
      "organizations_url": "https://api.github.com/users/realmarv/orgs",
      "received_events_url": "https://api.github.com/users/realmarv/received_events",
      "repos_url": "https://api.github.com/users/realmarv/repos",
      "site_admin": false,
      "starred_url": "https://api.github.com/users/realmarv/starred{/owner}{/repo}",
      "subscriptions_url": "https://api.github.com/users/realmarv/subscriptions",
      "type": "User",
      "url": "https://api.github.com/users/realmarv"
    }
  }
""">

type PRCommitsType =
    JsonProvider<"""
[
    {
      "sha": "cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
      "node_id": "C_kwDOIEdkNdoAKGNkYmM3ODgyYzI2YjFjNThiYjE3NWYyNWNjY2VmNGMwODNmMWY3MGI",
      "commit": {
        "author": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:15:25Z"
        },
        "committer": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:42:29Z"
        },
        "message": "plugins.tests: trailing space in code block test",
        "tree": {
          "sha": "0985ccb9302fc6623847f913d76a5ce12e58185b",
          "url": "https://api.github.com/repos/nblockchain/conventions/git/trees/0985ccb9302fc6623847f913d76a5ce12e58185b"
        },
        "url": "https://api.github.com/repos/nblockchain/conventions/git/commits/cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
        "comment_count": 0,
        "verification": {
          "verified": false,
          "reason": "unsigned",
          "signature": null,
          "payload": null
        }
      },
      "url": "https://api.github.com/repos/nblockchain/conventions/commits/cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
      "html_url": "https://github.com/nblockchain/conventions/commit/cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
      "comments_url": "https://api.github.com/repos/nblockchain/conventions/commits/cdbc7882c26b1c58bb175f25cccef4c083f1f70b/comments",
      "author": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "committer": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "parents": [
        {
          "sha": "4aebfb80985d2ee15425390bb6e2b531cfd7e756",
          "url": "https://api.github.com/repos/nblockchain/conventions/commits/4aebfb80985d2ee15425390bb6e2b531cfd7e756",
          "html_url": "https://github.com/nblockchain/conventions/commit/4aebfb80985d2ee15425390bb6e2b531cfd7e756"
        }
      ]
    },
    {
      "sha": "e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
      "node_id": "C_kwDOIEdkNdoAKGUwNWU1YTllNDczMDczOGVhOWEyOWNkM2NmYmNhZmFmN2Q1YThjMTg",
      "commit": {
        "author": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:29:15Z"
        },
        "committer": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:42:29Z"
        },
        "message": "commitlint.cfg: trailing space in big block (pass)",
        "tree": {
          "sha": "034862986f5d081d689ac1fb3cb2924d760b8511",
          "url": "https://api.github.com/repos/nblockchain/conventions/git/trees/034862986f5d081d689ac1fb3cb2924d760b8511"
        },
        "url": "https://api.github.com/repos/nblockchain/conventions/git/commits/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
        "comment_count": 0,
        "verification": {
          "verified": false,
          "reason": "unsigned",
          "signature": null,
          "payload": null
        }
      },
      "url": "https://api.github.com/repos/nblockchain/conventions/commits/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
      "html_url": "https://github.com/nblockchain/conventions/commit/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
      "comments_url": "https://api.github.com/repos/nblockchain/conventions/commits/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18/comments",
      "author": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "committer": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "parents": [
        {
          "sha": "cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
          "url": "https://api.github.com/repos/nblockchain/conventions/commits/cdbc7882c26b1c58bb175f25cccef4c083f1f70b",
          "html_url": "https://github.com/nblockchain/conventions/commit/cdbc7882c26b1c58bb175f25cccef4c083f1f70b"
        }
      ]
    },
    {
      "sha": "e53f17ca737909aa1646079ce0c07ad3d36bdf58",
      "node_id": "C_kwDOIEdkNdoAKGU1M2YxN2NhNzM3OTA5YWExNjQ2MDc5Y2UwYzA3YWQzZDM2YmRmNTg",
      "commit": {
        "author": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:47:34Z"
        },
        "committer": {
          "name": "Andres G. Aragoneses",
          "email": "knocte@gmail.com",
          "date": "2022-10-10T06:55:06Z"
        },
        "message": "commitlint.cfg: recommend a wrap text tool",
        "tree": {
          "sha": "d362f09e066c1c413a102d3704dc4791ac25882a",
          "url": "https://api.github.com/repos/nblockchain/conventions/git/trees/d362f09e066c1c413a102d3704dc4791ac25882a"
        },
        "url": "https://api.github.com/repos/nblockchain/conventions/git/commits/e53f17ca737909aa1646079ce0c07ad3d36bdf58",
        "comment_count": 0,
        "verification": {
          "verified": false,
          "reason": "unsigned",
          "signature": null,
          "payload": null
        }
      },
      "url": "https://api.github.com/repos/nblockchain/conventions/commits/e53f17ca737909aa1646079ce0c07ad3d36bdf58",
      "html_url": "https://github.com/nblockchain/conventions/commit/e53f17ca737909aa1646079ce0c07ad3d36bdf58",
      "comments_url": "https://api.github.com/repos/nblockchain/conventions/commits/e53f17ca737909aa1646079ce0c07ad3d36bdf58/comments",
      "author": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "committer": {
        "login": "knocte",
        "id": 331303,
        "node_id": "MDQ6VXNlcjMzMTMwMw==",
        "avatar_url": "https://avatars.githubusercontent.com/u/331303?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/knocte",
        "html_url": "https://github.com/knocte",
        "followers_url": "https://api.github.com/users/knocte/followers",
        "following_url": "https://api.github.com/users/knocte/following{/other_user}",
        "gists_url": "https://api.github.com/users/knocte/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/knocte/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/knocte/subscriptions",
        "organizations_url": "https://api.github.com/users/knocte/orgs",
        "repos_url": "https://api.github.com/users/knocte/repos",
        "events_url": "https://api.github.com/users/knocte/events{/privacy}",
        "received_events_url": "https://api.github.com/users/knocte/received_events",
        "type": "User",
        "site_admin": false
      },
      "parents": [
        {
          "sha": "e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
          "url": "https://api.github.com/repos/nblockchain/conventions/commits/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18",
          "html_url": "https://github.com/nblockchain/conventions/commit/e05e5a9e4730738ea9a29cd3cfbcafaf7d5a8c18"
        }
      ]
    }
  ]
""">

type ActionsRunsType =
    JsonProvider<"""
{
  "total_count": 3,
  "workflow_runs": [
    {
      "id": 31682666157,
      "name": "CI",
      "node_id": "WFR_kwLOHsaDEc8AAAAHYG8erQ",
      "head_branch": "wip/podman",
      "head_sha": "21a6fd4f492de228cd60871f3cf37b06275fb775",
      "path": ".github/workflows/CI.yml",
      "display_title": "GithubCI: use extras Dockerfile for CI as well",
      "run_number": 5109,
      "event": "push",
      "status": "completed",
      "conclusion": "success",
      "workflow_id": 156237264,
      "check_suite_id": 85947153914,
      "check_suite_node_id": "CS_kwDOHsaDEc8AAAAUAtl9-g",
      "url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157",
      "html_url": "https://github.com/NEltd/pulumi-deploy/actions/runs/31682666157",
      "pull_requests": [
        {
          "url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls/266",
          "id": 4261018475,
          "number": 266,
          "head": {
            "ref": "wip/podman",
            "sha": "90b9da26a8a27b5219d8589517956f533d1cee76",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          },
          "base": {
            "ref": "main",
            "sha": "cd84a0468928e7c21dff921ca6075d628e1fda89",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          }
        }
      ],
      "created_at": "2026-08-13T08:34:36Z",
      "updated_at": "2026-08-13T08:45:36Z",
      "actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "run_attempt": 1,
      "referenced_workflows": [],
      "run_started_at": "2026-08-13T08:34:36Z",
      "triggering_actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "jobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157/jobs",
      "logs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157/logs",
      "check_suite_url": "https://api.github.com/repos/NEltd/pulumi-deploy/check-suites/85947153914",
      "artifacts_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157/artifacts",
      "cancel_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157/cancel",
      "rerun_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31682666157/rerun",
      "previous_attempt_url": null,
      "workflow_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/workflows/156237264",
      "head_commit": {
        "id": "21a6fd4f492de228cd60871f3cf37b06275fb775",
        "tree_id": "9bcc871553d3dc72ee04c3af42350b02bd1766e2",
        "message": "GithubCI: use extras Dockerfile for CI as well\n\nFor CI and background_agents_create workflows.",
        "timestamp": "2026-08-12T13:05:35Z",
        "author": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        },
        "committer": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        }
      },
      "repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      },
      "head_repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      }
    },
    {
      "id": 31600118731,
      "name": "CI",
      "node_id": "WFR_kwLOHsaDEc8AAAAHW4OLyw",
      "head_branch": "wip/podman",
      "head_sha": "21a6fd4f492de228cd60871f3cf37b06275fb775",
      "path": ".github/workflows/CI.yml",
      "display_title": "GithubCI: use extras Dockerfile for CI as well",
      "run_number": 5065,
      "event": "push",
      "status": "completed",
      "conclusion": "success",
      "workflow_id": 156237264,
      "check_suite_id": 85718732598,
      "check_suite_node_id": "CS_kwDOHsaDEc8AAAAT9TwPNg",
      "url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731",
      "html_url": "https://github.com/NEltd/pulumi-deploy/actions/runs/31600118731",
      "pull_requests": [
        {
          "url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls/266",
          "id": 4261018475,
          "number": 266,
          "head": {
            "ref": "wip/podman",
            "sha": "90b9da26a8a27b5219d8589517956f533d1cee76",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          },
          "base": {
            "ref": "main",
            "sha": "cd84a0468928e7c21dff921ca6075d628e1fda89",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          }
        }
      ],
      "created_at": "2026-08-12T13:11:00Z",
      "updated_at": "2026-08-12T13:20:47Z",
      "actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "run_attempt": 1,
      "referenced_workflows": [],
      "run_started_at": "2026-08-12T13:11:00Z",
      "triggering_actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "jobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731/jobs",
      "logs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731/logs",
      "check_suite_url": "https://api.github.com/repos/NEltd/pulumi-deploy/check-suites/85718732598",
      "artifacts_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731/artifacts",
      "cancel_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731/cancel",
      "rerun_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31600118731/rerun",
      "previous_attempt_url": null,
      "workflow_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/workflows/156237264",
      "head_commit": {
        "id": "21a6fd4f492de228cd60871f3cf37b06275fb775",
        "tree_id": "9bcc871553d3dc72ee04c3af42350b02bd1766e2",
        "message": "GithubCI: use extras Dockerfile for CI as well\n\nFor CI and background_agents_create workflows.",
        "timestamp": "2026-08-12T13:05:35Z",
        "author": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        },
        "committer": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        }
      },
      "repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      },
      "head_repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      }
    },
    {
      "id": 31599739944,
      "name": "CI",
      "node_id": "WFR_kwLOHsaDEc8AAAAHW33EKA",
      "head_branch": "wip/podman",
      "head_sha": "21a6fd4f492de228cd60871f3cf37b06275fb775",
      "path": ".github/workflows/CI.yml",
      "display_title": "GithubCI: use extras Dockerfile for CI as well",
      "run_number": 5021,
      "event": "push",
      "status": "completed",
      "conclusion": "failure",
      "workflow_id": 156237264,
      "check_suite_id": 85717610361,
      "check_suite_node_id": "CS_kwDOHsaDEc8AAAAT9SrveQ",
      "url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944",
      "html_url": "https://github.com/NEltd/pulumi-deploy/actions/runs/31599739944",
      "pull_requests": [
        {
          "url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls/266",
          "id": 4261018475,
          "number": 266,
          "head": {
            "ref": "wip/podman",
            "sha": "90b9da26a8a27b5219d8589517956f533d1cee76",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          },
          "base": {
            "ref": "main",
            "sha": "cd84a0468928e7c21dff921ca6075d628e1fda89",
            "repo": {
              "id": 516326161,
              "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
              "name": "pulumi-deploy"
            }
          }
        }
      ],
      "created_at": "2026-08-12T13:06:27Z",
      "updated_at": "2026-08-12T13:07:09Z",
      "actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "run_attempt": 1,
      "referenced_workflows": [],
      "run_started_at": "2026-08-12T13:06:27Z",
      "triggering_actor": {
        "login": "webwarrior-ws",
        "id": 6537039,
        "node_id": "MDQ6VXNlcjY1MzcwMzk=",
        "avatar_url": "https://avatars.githubusercontent.com/u/6537039?v=4",
        "gravatar_id": "",
        "url": "https://api.github.com/users/webwarrior-ws",
        "html_url": "https://github.com/webwarrior-ws",
        "followers_url": "https://api.github.com/users/webwarrior-ws/followers",
        "following_url": "https://api.github.com/users/webwarrior-ws/following{/other_user}",
        "gists_url": "https://api.github.com/users/webwarrior-ws/gists{/gist_id}",
        "starred_url": "https://api.github.com/users/webwarrior-ws/starred{/owner}{/repo}",
        "subscriptions_url": "https://api.github.com/users/webwarrior-ws/subscriptions",
        "organizations_url": "https://api.github.com/users/webwarrior-ws/orgs",
        "repos_url": "https://api.github.com/users/webwarrior-ws/repos",
        "events_url": "https://api.github.com/users/webwarrior-ws/events{/privacy}",
        "received_events_url": "https://api.github.com/users/webwarrior-ws/received_events",
        "type": "User",
        "user_view_type": "public",
        "site_admin": false
      },
      "jobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944/jobs",
      "logs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944/logs",
      "check_suite_url": "https://api.github.com/repos/NEltd/pulumi-deploy/check-suites/85717610361",
      "artifacts_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944/artifacts",
      "cancel_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944/cancel",
      "rerun_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/runs/31599739944/rerun",
      "previous_attempt_url": null,
      "workflow_url": "https://api.github.com/repos/NEltd/pulumi-deploy/actions/workflows/156237264",
      "head_commit": {
        "id": "21a6fd4f492de228cd60871f3cf37b06275fb775",
        "tree_id": "9bcc871553d3dc72ee04c3af42350b02bd1766e2",
        "message": "GithubCI: use extras Dockerfile for CI as well\n\nFor CI and background_agents_create workflows.",
        "timestamp": "2026-08-12T13:05:35Z",
        "author": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        },
        "committer": {
          "name": "webwarrior-ws",
          "email": "reg@webwarrior.ws"
        }
      },
      "repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      },
      "head_repository": {
        "id": 516326161,
        "node_id": "R_kgDOHsaDEQ",
        "name": "pulumi-deploy",
        "full_name": "NEltd/pulumi-deploy",
        "private": true,
        "owner": {
          "login": "NEltd",
          "id": 139421098,
          "node_id": "O_kgDOCE9lqg",
          "avatar_url": "https://avatars.githubusercontent.com/u/139421098?v=4",
          "gravatar_id": "",
          "url": "https://api.github.com/users/NEltd",
          "html_url": "https://github.com/NEltd",
          "followers_url": "https://api.github.com/users/NEltd/followers",
          "following_url": "https://api.github.com/users/NEltd/following{/other_user}",
          "gists_url": "https://api.github.com/users/NEltd/gists{/gist_id}",
          "starred_url": "https://api.github.com/users/NEltd/starred{/owner}{/repo}",
          "subscriptions_url": "https://api.github.com/users/NEltd/subscriptions",
          "organizations_url": "https://api.github.com/users/NEltd/orgs",
          "repos_url": "https://api.github.com/users/NEltd/repos",
          "events_url": "https://api.github.com/users/NEltd/events{/privacy}",
          "received_events_url": "https://api.github.com/users/NEltd/received_events",
          "type": "Organization",
          "user_view_type": "public",
          "site_admin": false
        },
        "html_url": "https://github.com/NEltd/pulumi-deploy",
        "description": null,
        "fork": false,
        "url": "https://api.github.com/repos/NEltd/pulumi-deploy",
        "forks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/forks",
        "keys_url": "https://api.github.com/repos/NEltd/pulumi-deploy/keys{/key_id}",
        "collaborators_url": "https://api.github.com/repos/NEltd/pulumi-deploy/collaborators{/collaborator}",
        "teams_url": "https://api.github.com/repos/NEltd/pulumi-deploy/teams",
        "hooks_url": "https://api.github.com/repos/NEltd/pulumi-deploy/hooks",
        "issue_events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/events{/number}",
        "events_url": "https://api.github.com/repos/NEltd/pulumi-deploy/events",
        "assignees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/assignees{/user}",
        "branches_url": "https://api.github.com/repos/NEltd/pulumi-deploy/branches{/branch}",
        "tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/tags",
        "blobs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/blobs{/sha}",
        "git_tags_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/tags{/sha}",
        "git_refs_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/refs{/sha}",
        "trees_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/trees{/sha}",
        "statuses_url": "https://api.github.com/repos/NEltd/pulumi-deploy/statuses/{sha}",
        "languages_url": "https://api.github.com/repos/NEltd/pulumi-deploy/languages",
        "stargazers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/stargazers",
        "contributors_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contributors",
        "subscribers_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscribers",
        "subscription_url": "https://api.github.com/repos/NEltd/pulumi-deploy/subscription",
        "commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/commits{/sha}",
        "git_commits_url": "https://api.github.com/repos/NEltd/pulumi-deploy/git/commits{/sha}",
        "comments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/comments{/number}",
        "issue_comment_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues/comments{/number}",
        "contents_url": "https://api.github.com/repos/NEltd/pulumi-deploy/contents/{+path}",
        "compare_url": "https://api.github.com/repos/NEltd/pulumi-deploy/compare/{base}...{head}",
        "merges_url": "https://api.github.com/repos/NEltd/pulumi-deploy/merges",
        "archive_url": "https://api.github.com/repos/NEltd/pulumi-deploy/{archive_format}{/ref}",
        "downloads_url": "https://api.github.com/repos/NEltd/pulumi-deploy/downloads",
        "issues_url": "https://api.github.com/repos/NEltd/pulumi-deploy/issues{/number}",
        "pulls_url": "https://api.github.com/repos/NEltd/pulumi-deploy/pulls{/number}",
        "milestones_url": "https://api.github.com/repos/NEltd/pulumi-deploy/milestones{/number}",
        "notifications_url": "https://api.github.com/repos/NEltd/pulumi-deploy/notifications{?since,all,participating}",
        "labels_url": "https://api.github.com/repos/NEltd/pulumi-deploy/labels{/name}",
        "releases_url": "https://api.github.com/repos/NEltd/pulumi-deploy/releases{/id}",
        "deployments_url": "https://api.github.com/repos/NEltd/pulumi-deploy/deployments"
      }
    }
  ]
}
""">

let githubEventPath = Environment.GetEnvironmentVariable "GITHUB_EVENT_PATH"

if String.IsNullOrEmpty githubEventPath then
    let exitCode, errMsg = errNotGitHubCI
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode

let accessTokenName, accessToken =
    let personalAccessToken = Environment.GetEnvironmentVariable "ACCESS_TOKEN"

    if not(String.IsNullOrEmpty personalAccessToken) then
        "ACCESS_TOKEN", personalAccessToken
    else
        let githubToken = Environment.GetEnvironmentVariable "GITHUB_TOKEN"

        if not(String.IsNullOrEmpty githubToken) then
            "GITHUB_TOKEN", (Environment.GetEnvironmentVariable "GITHUB_TOKEN")
        else
            let exitCode, errMsg = errNoAccessToken
            Console.Error.WriteLine errMsg
            Environment.Exit exitCode
            failwith <| "Unreachable because of: " + errMsg

printfn "Using access token %s" accessTokenName
Console.WriteLine()

let jsonString = File.ReadAllText githubEventPath
let parsedJsonObj = GithubEventType.Parse jsonString

let gitForkUser = parsedJsonObj.PullRequest.Head.User.Login
let gitForkRepo = parsedJsonObj.PullRequest.Head.Repo.Name
let gitRepo = $"{gitForkUser}/{gitForkRepo}"

Console.WriteLine $"Fork repository is: {gitRepo}"

let GitHubApiCall(url: string) =
    let userAgent = ".NET App"
    let xGitHubApiVersion = "2022-11-28"

    let mediaTypeWithQuality =
        MediaTypeWithQualityHeaderValue "application/vnd.github+json"

    use client = new HttpClient()
    client.DefaultRequestHeaders.Accept.Clear()

    client.DefaultRequestHeaders.Accept.Add(mediaTypeWithQuality)

    client.DefaultRequestHeaders.Add("User-Agent", userAgent)
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", xGitHubApiVersion)

    if not(String.IsNullOrEmpty accessToken) then
        client.DefaultRequestHeaders.Add(
            "Authorization",
            $"token {accessToken}"
        )

    let result =
        Console.WriteLine(sprintf "About to query %s ..." url)

        try
            Async.AwaitTask(client.GetStringAsync url) |> Async.RunSynchronously

        with
        | ex ->
            match FindException<HttpRequestException> ex with
            | Some httpRequestException ->
                let accessTokenErrorMsg =
                    """Please create a PAT from your GitHub account (Settings->Developer Settings->PATs->Tokens(classic) and set it as GitHubActions repo secret ACCESS_TOKEN; then define the environment variable in your GitHubCI workflow:

```
    env:
      ACCESS_TOKEN: ${{ secrets.ACCESS_TOKEN }}
```
"""

                match Option.ofNullable httpRequestException.StatusCode with
                | Some statusCode when statusCode = HttpStatusCode.NotFound ->
                    if accessTokenName <> "ACCESS_TOKEN" then
                        failwith accessTokenErrorMsg
                    else
                        failwith
                            "Unexpected 404 received from GitHub API, using ACCESS_TOKEN properly"

                | Some statusCode when statusCode = HttpStatusCode.Forbidden ->
                    let permissionsErrMsg =
                        sprintf
                            """%s passed doesn't seem to have enough permissions.
To modify the permissions of your token, navigate to the Settings section of your
repository or organization and click on Actions button, then select General. From
'Workflow permissions' section on that page, choose 'Read and write permissions'
(which grants access to content and the ability to make changes)."""
                            accessTokenName

                    if accessTokenName = "ACCESS_TOKEN" then
                        failwith permissionsErrMsg
                    else
                        let msg =
                            "Or maybe you have to use an ACCESS_TOKEN instead: "

                        failwith(
                            permissionsErrMsg
                            + Environment.NewLine
                            + msg
                            + accessTokenErrorMsg
                        )

                | _ -> reraise()

            | _ -> reraise()

    result

let prHeadCommitHash = parsedJsonObj.PullRequest.Head.Sha

let prCommits =
    /// see https://docs.github.com/en/rest/pulls/pulls?apiVersion=2026-03-10#list-commits-on-a-pull-request
    let maxCommitsPerPage = 100

    if parsedJsonObj.PullRequest.Commits > maxCommitsPerPage then
        failwith
            $"More than {maxCommitsPerPage} commits in PR. This script doesn't support pagination yet."

    let url =
        $"{parsedJsonObj.PullRequest.Links.Commits.Href}?per_page={maxCommitsPerPage}"

    let prCommitsJsonString = GitHubApiCall url

    let parsedPrCommitsJsonObj = PRCommitsType.Parse prCommitsJsonString

    match
        parsedPrCommitsJsonObj
        |> Seq.tryFindIndex(fun commit -> commit.Sha = prHeadCommitHash)
        with
    | Some headCommitIndex ->
        // Don't include commits after this PR head.
        parsedPrCommitsJsonObj
        |> Seq.take(headCommitIndex + 1)
        |> Seq.map(fun commit -> (commit.Sha, commit.Commit.Message))
    | None ->
        failwithf
            "PR commits must include head commit. Must be a bug in %s."
            __SOURCE_FILE__

Console.WriteLine "Pull request commits are:"

for (commitHash, _commitMsg) in prCommits do
    Console.WriteLine commitHash

if Seq.length prCommits = 1 then
    printfn
        "Pull request has only 1 commit. Skipping 1-by-1 check because that commit will always have CI status"

    exit 0

// FIXME: there is an edge case not covered here: last commit of PR having
// "no ci" string, causing PR's CI not to trigger. Solution: move the GHA
// 'if' attrib (`if: github.event_name == 'pull_request'`) from CI.yml to
// this F# script, and in case it's not PR, check branch of current commit
// and if branch name is different than master/main, and last commit of it
// has "no ci", fail!!
let ShouldHaveCiStatus(commitMsg: string) =
    // NOTE: surprisingly enough, this needs to be case-sensitive, which means
    // that if commit has "[no CI]", GitHubActions will still spawn a job
    not <| commitMsg.Contains "[no ci]"

let ciStatuses =
    prCommits
    |> Seq.map(fun (commitHash: string, commitMsg: string) ->

        let url =
            sprintf
                "https://api.github.com/repos/%s/commits/%s/check-suites"
                gitRepo
                commitHash

        let json = GitHubApiCall url

        let hasCiStatus = not(json.Contains "\"check_suites\":[]")

        let shouldHaveCiStatus = ShouldHaveCiStatus commitMsg

        hasCiStatus, shouldHaveCiStatus
    )

let gitHubActionsEnabled = ciStatuses.Any(fun (hasCiStatus, _) -> hasCiStatus)

if not gitHubActionsEnabled then
    let exitCode, errMsg = errNoGitHubActions
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode
    failwith <| "Unreachable because of: " + errMsg

let notUsedGitPush1by1 =
    gitHubActionsEnabled
    && ciStatuses.Any(fun (hasCiStatus, shouldHaveCiStatus) ->
        (not hasCiStatus) && shouldHaveCiStatus
    )

if notUsedGitPush1by1 then
    let exitCode, errMsg = errNotPushedOneByOne
    Console.WriteLine()
    Console.Error.WriteLine errMsg
    Environment.Exit exitCode
    failwith <| "Unreachable because of: " + errMsg

let expectedFailureSubstrings = [ "failing test"; "failing CI" ]

let expectedFailureSubstringRegexes: List<Regex> =
    List.map
        (fun (str: string) ->
            Regex(
                str.Replace(" ", "\\s+"),
                RegexOptions.Compiled ||| RegexOptions.IgnoreCase
            )
        )
        expectedFailureSubstrings

let maxTimeToWaitForCiStatus = TimeSpan.FromMinutes 20.0
let timeBetweenCiStatusCheckRetries = TimeSpan.FromMinutes 1.0

[<TailCall>]
let rec CheckCiStatus
    (commitHash: string)
    (commitMessage: string)
    (timeRemaining: TimeSpan)
    =
    async {
        let url =
            sprintf
                "https://api.github.com/repos/%s/actions/runs?head_sha=%s&event=push"
                gitRepo
                commitHash

        let actionsRuns = GitHubApiCall url

        let actionsRunsParsedJson = ActionsRunsType.Parse actionsRuns

        if not(ShouldHaveCiStatus commitMessage) then
            ()
        elif expectedFailureSubstringRegexes
             |> List.exists(fun substringRegex ->
                 substringRegex.IsMatch commitMessage
             ) then
            ()
        else
            let latestRun =
                actionsRunsParsedJson.WorkflowRuns
                |> Seq.sortByDescending(fun run -> run.CreatedAt)
                |> Seq.tryHead

            // see https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks#check-statuses-and-conclusions
            let incompleteRunStatuses =
                [
                    "expected"
                    "queued"
                    "in_progress"
                    "waiting"
                    "requested"
                    "pending"
                ]

            let incompleteInformation, hasFailure =
                match latestRun with
                | Some run ->
                    List.contains run.Status incompleteRunStatuses,
                    run.Status = "completed" && run.Conclusion = "failure"
                | None -> true, false

            if incompleteInformation then
                if timeRemaining < timeBetweenCiStatusCheckRetries then
                    return ()
                else
                    do! Async.Sleep timeBetweenCiStatusCheckRetries

                    return!
                        CheckCiStatus
                            commitHash
                            commitMessage
                            (timeRemaining - timeBetweenCiStatusCheckRetries)
            elif hasFailure then
                Console.WriteLine()

                Console.Error.WriteLine
                    $"""Thanks for pushing your commits one by one.

However, it has been detected that CI is not successful (green ✅ colour) for one or more commits, or it passes (instead of being red ❌ colour) when a failing test is added (or failing CI is expected), as determined by its commit message.

See commit '{commitHash}' for the first occurrence of this issue."""

                let expectedFailureSubstringsJoined =
                    String.Join(
                        " or ",
                        expectedFailureSubstrings
                        |> Seq.map(fun str -> $"'{str}'")
                    )

                Console.Error.WriteLine
                    $"Hint: if you want to state that a commit can have red CI because of adding a failing test, please make sure that the commit message contains the {expectedFailureSubstringsJoined} term"

                let exitCode, errMsg = errCiNotSuccessful
                Console.Error.WriteLine errMsg
                Environment.Exit exitCode
                failwith <| "Unreachable because of: " + errMsg
    }

prCommits
|> Seq.mapi(fun index (commitHash, commitMessage) ->
    async {
        // stagger checkCiStatus calls in time to minimize mess in console oputput
        do! Async.Sleep(TimeSpan.FromSeconds(float index * 2.0))
        do! CheckCiStatus commitHash commitMessage maxTimeToWaitForCiStatus
    }
)
|> Async.Parallel
|> Async.RunSynchronously
|> ignore<array<unit>>
