module FileConventions.Test.WrapTextTests

open System

open NUnit.Framework

open FileConventions

// because WrapText is marked as deprecated as opposed to SafeWrapText
#nowarn "0044"

[<Test>]
let WrapTextTest1() =
    let characterCount = 64

    let paragraph =
        "This is a very very very very long line with more than 64 characters."

    let expectedResult =
        "This is a very very very very long line with more than 64"
        + Environment.NewLine
        + "characters."

    Assert.That(WrapText paragraph characterCount, Is.EqualTo expectedResult)

[<Test>]
let WrapTextTest2() =
    let characterCount = 64

    let paragraph =
        "This is short line."
        + Environment.NewLine
        + "```"
        + Environment.NewLine
        + "This is a very very very very long line with more than 64 characters inside a code block."
        + Environment.NewLine
        + "```"

    let expectedResult = paragraph

    Assert.That(WrapText paragraph characterCount, Is.EqualTo expectedResult)

[<Test>]
let WrapTextTest3() =
    let characterCount = 64
    let tenDigits = "1234567890"

    let seventyChars =
        tenDigits
        + tenDigits
        + tenDigits
        + tenDigits
        + tenDigits
        + tenDigits
        + tenDigits

    let paragraph =
        "This is short line referring to [1]."
        + Environment.NewLine
        + "[1] someUrl://"
        + seventyChars

    let expectedResult = paragraph

    Assert.That(WrapText paragraph characterCount, Is.EqualTo expectedResult)


[<Test>]
let WrapTextTest4() =
    let characterCount = 64

    let text =
        "This is short line."
        + Environment.NewLine
        + Environment.NewLine
        + "This is a very very very very very long line with more than 64 characters."

    let expectedResult =
        "This is short line."
        + Environment.NewLine
        + Environment.NewLine
        + "This is a very very very very very long line with more than 64"
        + Environment.NewLine
        + "characters."

    Assert.That(WrapText text characterCount, Is.EqualTo expectedResult)

[<Test>]
let WrapTextTest5() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)"
        + Environment.NewLine
        + Environment.NewLine
        + "These were the steps to reproduce:"
        + Environment.NewLine
        + "Do foo."
        + Environment.NewLine
        + Environment.NewLine
        + "Current results:"
        + Environment.NewLine
        + "Bar happens."
        + Environment.NewLine
        + Environment.NewLine
        + "Expected results:"
        + Environment.NewLine
        + "Baz happens."

    let expectedResult = text

    Assert.That(WrapText text characterCount, Is.EqualTo expectedResult)

[<Test>]
let WrapTextTest6() =
    let characterCount = 64

    let commitMsg =
        "foo: this is a header"
        + Environment.NewLine
        + Environment.NewLine
        + "This is a body:"
        + Environment.NewLine
        + Environment.NewLine
        + "```"
        + Environment.NewLine
        + "A code block that has two conditions, it has a very long line that exceeds the limit."
        + Environment.NewLine
        + Environment.NewLine
        + "It also has multiple paragraphs."
        + Environment.NewLine
        + "```"

    Assert.That(WrapText commitMsg characterCount, Is.EqualTo commitMsg)

[<Test>]
let WrapTextTest7() =
    let characterCount = 64
    let text = "foo:  bar"
    let fixedText = "foo: bar"

    Assert.That(WrapText text characterCount, Is.EqualTo fixedText)

[<Test>]
let WrapTextTest8() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
* Foo.
* Bar."

    Assert.That(WrapText text characterCount, Is.EqualTo text)

[<Test>]
let WrapTextTest9() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
- Foo.
- Bar."

    Assert.That(WrapText text characterCount, Is.EqualTo text)

[<Test>]
let WrapTextTest10() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This is some text in **BOLD** that shouldn't be wrapped."

    Assert.That(WrapText text characterCount, Is.EqualTo text)

[<Test>]
let WrapTextTest11() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This is some text in
**BOLD** that should be wrapped."

    let expectedText =
        "Fixed bug (a title of less than 50 chars)

This is some text in **BOLD** that should be wrapped."

    Assert.That(WrapText text characterCount, Is.EqualTo expectedText)

[<Test>]
let WrapTextTest12() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This text's for a multiplication 2
* 4 equals 8."

    let expectedText =
        "Fixed bug (a title of less than 50 chars)

This text's for a multiplication 2 * 4 equals 8."

    Assert.That(WrapText text characterCount, Is.EqualTo expectedText)

[<Test>]
let WrapTextTest13() =
    let characterCount = 64

    let text =
        "Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
1. Foo.
2. Bar."

    Assert.That(WrapText text characterCount, Is.EqualTo text)

[<Test>]
let WrapTextTest14() =
    let characterCount = 64

    let text =
        "change wrapLastCommMsg postCommit->commitMsg hook

New .husky/commit-msg hook:
- Replaces the old .husky/post-commit hook.
- Receives the commit message file path ($1) and passes it to
the F# script.
- Because it's a commit-msg hook, if the script
exits with a non-zero code, Git aborts the commit (unlike
post-commit, which runs too late).

Updated scripts/wrapLatestCommitMsg.fsx:
- Reads the commit message from the file path passed as an
argument (instead of git log -1 --format=%B).
- Strips Git
comment lines (# ...) before processing, then preserves them
when writing back.
- Validates the title length against the same
limit as your commitlint policy (headerMaxLineLength = 50). If
the title is too long, it prints an error to stderr and exits
with code 1, blocking the commit.
- Still wraps body paragraphs
to 64 chars using the existing FileConventions.SafeWrapText
logic.
- Writes the result directly back to the commit message
file, so no git commit --amend loop is needed."

    let expectedText =
        "change wrapLastCommMsg postCommit->commitMsg hook

New .husky/commit-msg hook:
- Replaces the old .husky/post-commit hook.
- Receives the commit message file path ($1) and passes it to
the F# script.
- Because it's a commit-msg hook, if the script exits with a
non-zero code, Git aborts the commit (unlike post-commit, which
runs too late).

Updated scripts/wrapLatestCommitMsg.fsx:
- Reads the commit message from the file path passed as an
argument (instead of git log -1 --format=%B).
- Strips Git comment lines (# ...) before processing, then
preserves them when writing back.
- Validates the title length against the same limit as your
commitlint policy (headerMaxLineLength = 50). If the title is
too long, it prints an error to stderr and exits with code 1,
blocking the commit.
- Still wraps body paragraphs to 64 chars using the existing
FileConventions.SafeWrapText logic.
- Writes the result directly back to the commit message file, so
no git commit --amend loop is needed."

    Assert.That(WrapText text characterCount, Is.EqualTo expectedText)

[<Test>]
let WrapTextTest15() =
    let characterCount = 64

    let textWithAsterisksAndNoColon =
        "Fixed bug (a title of less than 50 chars)

* Foo.
* Bar."

    Assert.That(
        WrapText textWithAsterisksAndNoColon characterCount,
        Is.EqualTo textWithAsterisksAndNoColon
    )

    let textWithDashesAndNoColon =
        "Fixed bug (a title of less than 50 chars)

- Foo.
- Bar."

    Assert.That(
        WrapText textWithDashesAndNoColon characterCount,
        Is.EqualTo textWithDashesAndNoColon
    )

    let textWithNumberBulletsAndNoColon =
        "Fixed bug (a title of less than 50 chars)

1. Foo.
2. Bar."

    Assert.That(
        WrapText textWithNumberBulletsAndNoColon characterCount,
        Is.EqualTo textWithNumberBulletsAndNoColon
    )

#warnon "0044"

[<Test>]
let HasEmDashTest1() =
    let textWithEmDash = "This — is a test"
    Assert.That(HasEmDash textWithEmDash, Is.True)

[<Test>]
let HasEmDashTest2() =
    let textWithoutEmDash = "This is a test"
    Assert.That(HasEmDash textWithoutEmDash, Is.False)

[<Test>]
let HasEmDashTest3() =
    let textWithNormalDash = "This - is a test"
    Assert.That(HasEmDash textWithNormalDash, Is.False)

[<Test>]
let RemoveAllWhitespaceTest() =
    let text =
        "  Hello -world\t\r\n"
        + "Foo   \n* bar\r"
        + Environment.NewLine
        + "baz  "

    Assert.That(RemoveAllWhitespace text, Is.EqualTo "Hello-worldFoo*barbaz")
