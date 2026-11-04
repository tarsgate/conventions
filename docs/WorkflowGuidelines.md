# Workflow guidelines

* When contributing a PullRequest, separate your commits in units of work (don't mix changes that have different concerns in the same commit). Don't forget to include all explanations and reasonings in the commit messages, instead of just leaving them as part of the PullRequest description.

* The most common mistake when mixing multiple concerns in the same commit is to join both refactorings and change of behaviour in the same change. As a convention, we will consider the word "refactoring" to refer to changes in the code that should not have any impact on the general behaviour of the program (especially talking about behaviour noticed by the end-user).

* Push each commit separately (instead of sending more than 1 commit in a single push), so that we can have a CI status for each commit in the PullRequest. This is a best practice because it will make sure that the build is not broken in between commits (otherwise, future developers may have a hard time when trying to bisect bugs). If you have already pushed your commits to the remote in one push, this can be re-done by using our [gitPush1by1.fsx](https://github.com/nblockchain/conventions/blob/master/scripts/gitPush1by1.fsx) script, or this technique manually: https://stackoverflow.com/a/3230241/544947

* If PR contains one commit msg only, then PR title and description has to align with git commit msg title and body (respectively), and it can be merged with "Rebase and merge" button.

* Otherwise, PR description could be a summary of all the commits, or choose the most important commit to align with (in case the other commits don't add much substance). In this case, PR has to be merged with "Create a merge commit" button.

* All information that is present in PR description should be in the commit messages as well, because this way it will be preserved in case of changing git hosting provider.

* Before PR is ready to be merged, it needs to be tested, otherwise marked as DRAFT.

* Before removing DRAFT status, the PR should be squashed, which means:
    * WIP commits removed.
    * TMP commits reverted (via `git revert`, not making them disappear).
    * Commits that increase the quality of previous commits of the same PR need to be squashed with the commits that introduced the new code (e.g. refactoring commits that improve method names; or commits that make CI style jobs pass, in order to avoid intermediary commits with broken CI; etc).
    * Here "increasing the quality" means better programming practices, better readability, better maintainability, etc. It **does not mean** fixing bugs. If a commit fixes a problem that happens at runtime which is worth noting about in a commit message, it is very likely that the commit needs to live on its own to be able to document in a commit message about the problem that was faced.
    * Commits that change behaviour **should not** be squashed. Therefore squashing rarely means "squashing into one", because there are almost always exceptions.
    * Commit messages are reviewed **again** to ensure that they have the **why**, not just the **what** (and optionally the **how**, in case it's not obvious).

* If you want to contribute a script, do not use PowerShell or Bash, but an F# script. The reason to not use PowerShell is a personal preference from the maintainer of this project (and his suspicion that it might not be 100% guaranteed to be crossplatform); and the reason not to use Bash is because it's only Unix compatible (we cannot assume WSL is installed in Windows), and in general because it's too undeterministic and old, more info here: https://news.ycombinator.com/item?id=33116310

* Our naming conventions are as follows:
    * Script names (e.g. files with `.sh`, `.bat` or `.fsx` extensions): snake_case.
    * CI job names: kebab-case.
    * Important public constants: TRAIN_CASE.
    * .NET (F# and C#) source files and projects (project names and project file names): PascalCase. An exception to this rule is project names and project file names that correspond to a console project which ends up being compiled and packaged in NuGet as a dotnet tool, for example: https://github.com/nblockchain/fsx/tree/master/fsxc
    * .NET APIs: PascalCase (see our [F# Style Guide](FSharpStyleGuide.md) for more info).
    * .NET parameters, local variables & nested functions: camelCase (again, see our [F# Style Guide](FSharpStyleGuide.md) for more info).
    * Please use verbs (in infinitive) for method/function & script names, and nouns for variables/constants/parameters/properties/fields.

* Be mindful of the way we prefer to use git:
    * When rebasing a PR, never use the command `git merge` or the GitHubUI to update branches, otherwise you might get some "Merge" commits that make your commit history unreadable.
    * Merge commits are only acceptable when caused by merging a PR that has more than 1 commit (if PR only has one commit, it's better to merge it with the "Rebase" option that GitHub provides).
    * Don't confuse the command `git rebase -i HEAD~n` (where `-i` stands for 'interactive') with the non-interactive `git rebase` operation.
        * The former is for squashing commits or changing their order. By the way, given that you need to be extremely careful when determining the number of commits `n` (if you put a number that is too high, even off by one, you might destroy previous merge commits), then we recommend to always add the `-r` flag when using the interactive mode (which is the shorthand for --rebase-merges). In conclusion: when you're about to use `git rebase -i HEAD~n`, rather always use `git rebase -ir HEAD~n`.
        * The latter is for rebasing a branch or PR against, for example, `master` or `main`, but only if there have been no recent force-pushes in the branch you want to rebase against. If there have been force-pushes, then the safest way to rebase is by using the `git cherry-pick` command.
    * When using the command `git pull`, never ever ever forget the flag `--rebase`, otherwise git will introduce merge commits for you automatically.

* Git commit messages should follow this style:

```
Scope/sub-scope: short title of what is changed (50 chars max)

Explanation of **why** (and maybe **how** as well, in case there's a part of
the change that is not self-explanatory). Don't hesitate to be very verbose
here, adding any references you may need, in this way[1], or even @nicknames of
people that helped. Manually crop your lines to not be longer than 64 chars.

Fixes https://github.com/nblockchain/geewallet/issues/45

[1] http://foo.bar/baz
```

As you can see, writing a commit message is generally like writing an e-mail: it has a title at the top which is normally a short sentence (but not ended with a period, like most titles), and a body that starts in the 3rd line and which contains one or many paragraphs (each ending with a dot, as it's text in prose). In particular, the example above would be for a commit message that fixes the issue #45. **Scope** usually refers to the project name, but without the need to include the name of the project (e.g. in geewallet, all project names start with the `GWallet.` prefix, then there's no need to specify it; so use `Backend` as scope instead of `GWallet.Backend`). The **Sub-scope** may refer to a folder or module inside what's represented as the scope, but it's not a strict mapping.

When referencing a bug/issue, as you can see above you can add a sentence at the end of the commit message which starts with `Fixes `, followed by the **full URL** of the bug/issue (full URL is preferable because it will not break when migrating to antoher git hosting service); this way, the ticket will be closed when the commit lands in the main branch (which in some repos will be called `master`, or `main`). If the commit is not really a fix for the issue, but you still want the ticket to be closed after the commit lands, then you would use the word `Closes` instead of `Fixes`.

When referencing a commit, use commit hash instead of full URL, because commit hash is independent of specific git hosting service and will not become invalid in case of migrating to antoher hosting service.

Do not use long lines (manually crop them with EOLs because git doesn't do this automatically); and a recommended way to do this properly is to get a tool that can give you vertical hints of our 50-64 char limits, for example: Visual Studio Code; see below how to configure it:

1. First make sure that you can launch VSCode in the command line by issuing a `code` command. There's info online on how to configure your OS to do this, depending on the one you're running.
2. Second, configure git globally to use VSCode as your git editor: `git config --global core.editor "code --wait"``
3. Then, configure VSCode to show you the vertical line hints, following this steps:
  * Open your global settings.json file by pressing Ctrl+Shift+P (Windows/Linux) or Cmd+Shift+P (macOS).
  * Type Preferences: Open User Settings (JSON) and select it.
  * Add the following language-specific configuration block inside your main settings object:

```
"[git-commit]": {
    "editor.rulers": [50, 64]
}
```
NOTE: if your JSON file was empty from the start, then wrap the above setting in curly braces to make it proper JSON:

```json
{
    "[git-commit]": {
        "editor.rulers": [50, 64]
    }
}
```

For more info about why well-crafted commit messages are important, PLEASE PLEASE
read this article: https://web.archive.org/web/20240201135044/https://dhwthompson.com/2019/my-favourite-git-commit

