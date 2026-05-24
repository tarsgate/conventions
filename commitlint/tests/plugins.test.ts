import { runCommitLintOnMsg } from "./testHelpers.js";
import { test, expect } from "vitest";

test("body-prose1", () => {
    const commitMsgWithLowercaseBodyStart = `foo: this is only a title

bla blah bla.`;

    const bodyProse1 = runCommitLintOnMsg(commitMsgWithLowercaseBodyStart);
    expect(bodyProse1.status).not.toBe(0);
});

test("body-prose2", () => {
    const commitMsgWithNumbercaseBodyStart = `foo: this is only a title

1234 bla blah bla.`;

    const bodyProse2 = runCommitLintOnMsg(commitMsgWithNumbercaseBodyStart);
    expect(bodyProse2.status).toBe(0);
});

test("body-prose3", () => {
    const commitMsgWithUrl = `foo: this is only a title

someUrl://blahblah.com`;

    const bodyProse3 = runCommitLintOnMsg(commitMsgWithUrl);

    // because URLs can bypass the rule
    expect(bodyProse3.status).toBe(0);
});

test("body-prose4", () => {
    const commitMsgWithFootnoteUrl = `foo: this is only a title

Bla blah[1] bla.

[1] someUrl://blahblah.com`;

    const bodyProse4 = runCommitLintOnMsg(commitMsgWithFootnoteUrl);

    // because URLs in footer can bypass the rule
    expect(bodyProse4.status).toBe(0);
});

test("body-prose5", () => {
    const commitMsgWithBugUrl = `foo: this is only a title

Fixes someUrl://blahblah.com`;

    const bodyProse5 = runCommitLintOnMsg(commitMsgWithBugUrl);

    // because URLs in "Fixes <URL>" sentence can bypass the rule
    expect(bodyProse5.status).toBe(0);
});

test("body-prose6", () => {
    const commitMsgWithBlock =
        "foo: this is only a title\n\nBar baz.\n\n```\nif (foo) { bar(); }\n```";
    const bodyProse6 = runCommitLintOnMsg(commitMsgWithBlock);

    // because ```blocks surrounded like this``` can bypass the rule
    expect(bodyProse6.status).toBe(0);
});

test("body-prose7", () => {
    const commitMsgWithParagraphEndingWithColon = `foo: this is only a title

Bar baz:

Blah blah.`;

    const bodyProse7 = runCommitLintOnMsg(
        commitMsgWithParagraphEndingWithColon
    );

    // because paragraphs can end with a colon
    expect(bodyProse7.status).toBe(0);
});

test("body-prose8", () => {
    const commitMsgWithCoAuthoredByTagThatIsObviouslyNotAParagraph = `foo: this is only a title

Co-authored-by: Jon Doe <jondoe@example.com>`;

    const bodyProse8 = runCommitLintOnMsg(
        commitMsgWithCoAuthoredByTagThatIsObviouslyNotAParagraph
    );

    // because Co-authored-by tags don't end with a dot
    expect(bodyProse8.status).toBe(0);
});

test("body-prose9", () => {
    const commitMsgWithCommitUrlAtTheEndOfBodyParagraph = `foo: this is only a title

Foo bar:
https://github.com/username/repo/commit/1234567891234567891234567891234567891234`;

    const bodyProse9 = runCommitLintOnMsg(
        commitMsgWithCommitUrlAtTheEndOfBodyParagraph
    );

    expect(bodyProse9.status).toBe(0);
});

test("body-prose10", () => {
    const commitMsgWithLargeBody =
        "Network,TorHandshakes: handle handshake fail\n\n" +
        "```\nThe active test run was aborted. System.Exception: Key handshake failed!\n\n" +
        "at System.Threading.ThreadPoolWorkQueue.Dispatch()\n```";

    const bodyProse10 = runCommitLintOnMsg(commitMsgWithLargeBody);
    expect(bodyProse10.status).toBe(0);
});

test("body-prose11", () => {
    const commitMsgWithLargeBody =
        "Backend/Ether: catch/retry new -32002 err code\n\n" +
        "CI on master branch caught this[1]:\n\n" +
        "```\nUnhandled Exception\n```\n\n" +
        "[1] https://github.com/nblockchain/geewallet/actions/runs/3507005645/jobs/5874411684";

    const bodyProse11 = runCommitLintOnMsg(commitMsgWithLargeBody);
    expect(bodyProse11.status).toBe(0);
});

test("body-prose12", () => {
    const commitMsgWithBugUrl = `foo: this is only a title

Closes someUrl://blahblah.com`;

    const bodyProse12 = runCommitLintOnMsg(commitMsgWithBugUrl);

    // because URLs in "Closes <URL>" sentence can bypass the rule, similar to body-prose5
    expect(bodyProse12.status).toBe(0);
});

test("body-prose13", () => {
    const commitMsgWithParagraphEndingWithQuestionMark = `foo: this is only a title

Increase verbosity, because why not?

Blah blah.`;

    const bodyProse13 = runCommitLintOnMsg(
        commitMsgWithParagraphEndingWithQuestionMark
    );

    // because paragraphs can end with a question mark
    expect(bodyProse13.status).toBe(0);
});

test("body-prose14", () => {
    const commitMsgWithParagraphEndingWithExclamationMark = `foo: this is only a title

Increase verbosity, because why not!

Blah blah.`;

    const bodyProse14 = runCommitLintOnMsg(
        commitMsgWithParagraphEndingWithExclamationMark
    );

    // because paragraphs can end with a question mark
    expect(bodyProse14.status).toBe(0);
});

test("body-prose15", () => {
    const commitMsgWithParagraphEndingInParentheses = `foo: this is only a title

Paragraph begin. (Some text inside parens.)

Paragraph begin. (Some text inside parens?)

Paragraph begin. (Some text inside parens!)

Paragraph begin. Now a smiley! :)

Blah blah.`;

    const bodyProse15 = runCommitLintOnMsg(
        commitMsgWithParagraphEndingInParentheses
    );

    // because paragraphs can end with a question mark
    expect(bodyProse15.status).toBe(0);
});

test("body-prose16", () => {
    const commitMsgWithUrlAtTheEndOfParagraph = `Frontend.XF.Android: switch to SDK-style

Strangely enough when opening the new gwallet.android.sln in
VS4Mac, it asks the user to install the wasm-tools-net6.0
workload, even though CI works without it.

Original PR: https://github.com/nblockchain/geewallet/pull/190`;

    const bodyProse16 = runCommitLintOnMsg(commitMsgWithUrlAtTheEndOfParagraph);
    expect(bodyProse16.status).toBe(0);
});

test("body-prose17", () => {
    const commitMsgWithWindowsEOL =
        "title: this is only title\r\n\r\n" +
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit\r\n" +
        "lorem ipsum dolor sit amet, consectetur porttitor jidga\r\n" +
        "nam sed porttitor turpis, vitae erat curae.";
    const bodyProse17 = runCommitLintOnMsg(commitMsgWithWindowsEOL);
    expect(bodyProse17.status).toBe(0);
});

test("body-max-line-length1", () => {
    const tenChars = "1234 67890";
    const sixtyChars =
        tenChars + tenChars + tenChars + tenChars + tenChars + tenChars;
    const commitMsgWithOnlySixtyFourCharsInBody =
        "foo: this is only a title" + "\n\n" + sixtyChars + "123.";
    const bodyMaxLineLength1 = runCommitLintOnMsg(
        commitMsgWithOnlySixtyFourCharsInBody
    );
    expect(bodyMaxLineLength1.status).toBe(0);
});

test("body-max-line-length2", () => {
    const tenChars = "1234 67890";
    const sixtyChars =
        tenChars + tenChars + tenChars + tenChars + tenChars + tenChars;
    const commitMsgWithOnlySixtyFiveCharsInBody =
        "foo: this is only a title" + "\n\n" + sixtyChars + "1234.";
    const bodyMaxLineLength2 = runCommitLintOnMsg(
        commitMsgWithOnlySixtyFiveCharsInBody
    );
    expect(bodyMaxLineLength2.status).not.toBe(0);
});

test("body-max-line-length3", () => {
    const tenDigits = "1234567890";
    const seventyChars =
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits;
    const commitMsgWithUrlThatExceedsBodyMaxLineLength =
        "foo: this is only a title" + "\n\n" + "someUrl://" + seventyChars;
    const bodyMaxLineLength3 = runCommitLintOnMsg(
        commitMsgWithUrlThatExceedsBodyMaxLineLength
    );

    // because URLs can bypass the limit
    expect(bodyMaxLineLength3.status).toBe(0);
});

test("body-max-line-length4", () => {
    const tenDigits = "1234567890";
    const seventyChars =
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits;
    const commitMsgWithUrlThatExceedsBodyMaxLineLength =
        "foo: this is only a title" +
        "\n\n" +
        "Bla blah[1] bla.\n\n[1] someUrl://" +
        seventyChars;
    const bodyMaxLineLength4 = runCommitLintOnMsg(
        commitMsgWithUrlThatExceedsBodyMaxLineLength
    );

    // because URLs in footer can bypass the limit
    expect(bodyMaxLineLength4.status).toBe(0);
});

test("body-max-line-length5", () => {
    const tenDigits = "1234567890";
    const seventyChars =
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits +
        tenDigits;
    const commitMsgWithUrlThatExceedsBodyMaxLineLength =
        "foo: this is only a title" +
        "\n\n" +
        "Fixes someUrl://" +
        seventyChars;
    const bodyMaxLineLength5 = runCommitLintOnMsg(
        commitMsgWithUrlThatExceedsBodyMaxLineLength
    );

    // because URLs in "Fixes <URL>" sentence can bypass the limit
    expect(bodyMaxLineLength5.status).toBe(0);
});

test("body-max-line-length6", () => {
    const tenChars = "1234 67890";
    const seventyChars =
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars;
    const commitMsgWithUrlThatExceedsBodyMaxLineLength =
        "foo: this is only a title" +
        "\n\n" +
        "Bar baz.\n```\n" +
        seventyChars +
        "\n```";
    const bodyMaxLineLength6 = runCommitLintOnMsg(
        commitMsgWithUrlThatExceedsBodyMaxLineLength
    );

    // because ```blocks surrounded like this``` can bypass the limit
    expect(bodyMaxLineLength6.status).toBe(0);
});

test("body-max-line-length7", () => {
    const tenChars = "1234567890";
    const seventyChars =
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars +
        tenChars;
    const commitMsgWithCoAuthoredByTagThatExceedsBodyMaxLineLength =
        "foo: this is only a title" +
        "\n\n" +
        "Co-authored-by: Jon Doe <" +
        seventyChars +
        "@example.com>";
    const bodyMaxLineLength7 = runCommitLintOnMsg(
        commitMsgWithCoAuthoredByTagThatExceedsBodyMaxLineLength
    );

    // because Co-authored-by tags can bypass the limit
    expect(bodyMaxLineLength7.status).toBe(0);
});

test("body-max-line-length8", () => {
    const commitMsgWithLargeBody = `Network,TorHandshakes: handle handshake fail

--- Line between dashes ---
A very long line. A very long line. A very long line. A very long line. A very long line. A very long line.`;

    const bodyMaxLineLength8 = runCommitLintOnMsg(commitMsgWithLargeBody);
    expect(bodyMaxLineLength8.status).toBe(1);
});

test("body-max-line-length9", () => {
    // see https://github.com/nblockchain/conventions/issues/124
    const commitMsgWithLargeBody = `GrpcService: fix some logging nits

These mistakes were made in 45faeca2f0e7c9c5545f54fb3fcc815f52b8a7cf.`;

    const bodyMaxLineLength9 = runCommitLintOnMsg(commitMsgWithLargeBody);
    expect(bodyMaxLineLength9.status).toBe(0);
});

test("body-max-line-length10", () => {
    // see https://github.com/nblockchain/conventions/issues/124
    const commitMsgWithLargeBody = `GrpcService: fix some logging nits

These mistakes were made in this GrpcService's RunIntoMeService commit: 45faeca2f0e7c9c5545f54fb3fcc815f52b8a7cf.`;

    const bodyMaxLineLength10 = runCommitLintOnMsg(commitMsgWithLargeBody);
    expect(bodyMaxLineLength10.status).toBe(1);
});

test("body-paragraph-line-min-length1", () => {
    const tenChars = "1234 67890";
    const fortyChars = tenChars + tenChars + tenChars + tenChars;
    const sixtyChars = fortyChars + tenChars + tenChars;
    const commitMsgWithFortyCharsInBody =
        "foo: this is only a title\n\n" + fortyChars + ".\n" + sixtyChars + ".";
    const bodyParagraphLineMinLength1 = runCommitLintOnMsg(
        commitMsgWithFortyCharsInBody
    );
    expect(bodyParagraphLineMinLength1.status).not.toBe(0);
});

test("body-paragraph-line-min-length2", () => {
    const commitMsgWithCommitUrlAtTheEndOfBodyParagraph = `foo: this is only a title

Foo bar:
https://github.com/username/repo/commit/1234567891234567891234567891234567891234`;

    const bodyParagraphLineMinLength2 = runCommitLintOnMsg(
        commitMsgWithCommitUrlAtTheEndOfBodyParagraph
    );

    expect(bodyParagraphLineMinLength2.status).toBe(0);
});

test("body-paragraph-line-min-length3", () => {
    const tenChars = "1234 67890";
    const fortyChars = tenChars + tenChars + tenChars + tenChars;
    const commitMsgWithFortyCharsInTheLastLineOfParagraph =
        "foo: this is only a title\n\n" + fortyChars + ".";
    const bodyParagraphLineMinLength3 = runCommitLintOnMsg(
        commitMsgWithFortyCharsInTheLastLineOfParagraph
    );
    expect(bodyParagraphLineMinLength3.status).toBe(0);
});

test("body-paragraph-line-min-length4", () => {
    const tenChars = "1234 67890";
    const fortyChars = tenChars + tenChars + tenChars + tenChars;
    const sixtyChars = fortyChars + tenChars + tenChars;
    const commitMsgWithCodeBlockThatSubceedsBodyMinLineLength =
        "foo: this is only a title" +
        "\n\n" +
        "Bar baz.\n```\n" +
        fortyChars +
        ".\n" +
        sixtyChars +
        "." +
        "\n```";

    const bodyParagraphLineMinLength4 = runCommitLintOnMsg(
        commitMsgWithCodeBlockThatSubceedsBodyMinLineLength
    );

    // because ```blocks surrounded like this``` can bypass the limit
    expect(bodyParagraphLineMinLength4.status).toBe(0);
});

test("body-paragraph-line-min-length5", () => {
    const commitMsgWithCoAuthoredByTagThatSubceedsBodyMinLineLength = `foo: this is only a title

Co-authored-by: Jon Doe <shortmail@example.com>
Co-authored-by: Jon Doe <JonDoeEmailAddress@example.com>`;

    const bodyParagraphLineMinLength5 = runCommitLintOnMsg(
        commitMsgWithCoAuthoredByTagThatSubceedsBodyMinLineLength
    );

    // because Co-authored-by tags can bypass the limit
    expect(bodyParagraphLineMinLength5.status).toBe(0);
});

test("body-paragraph-line-min-length6", () => {
    const commitMsgThatSubceedsBodyMinLineLength = `Some title of less than 50 chars

This is a paragraph whose 2nd line is less than 50 chars
but should not make commitlint complain because
TheNextWordInThe3rdLineIsTooLongToBePlacedIn2ndLine.`;

    const bodyParagraphLineMinLength6 = runCommitLintOnMsg(
        commitMsgThatSubceedsBodyMinLineLength
    );

    expect(bodyParagraphLineMinLength6.status).toBe(0);
});

test("body-paragraph-line-min-length7", () => {
    const commitMsgThatSubceedsBodyMinLineLengthButIsLegit = `Fixed bug (a title of less than 50 chars)

These were the steps to reproduce:
Do foo.

Current results:
Bar happens.

Expected results:
Baz happens.`;

    const bodyParagraphLineMinLength7 = runCommitLintOnMsg(
        commitMsgThatSubceedsBodyMinLineLengthButIsLegit
    );

    expect(bodyParagraphLineMinLength7.status).toBe(0);
});

test("body-paragraph-line-min-length8", () => {
    const commitMsgWithCodeBlockThatSubceedsBodyMinLineLength =
        "foo: this is only a title\n\n" +
        "Body with a link [1]:\n" +
        "```\n" +
        "some code block\n" +
        "```\n\n" +
        "[1] https://foo.com/bar";

    const bodyParagraphLineMinLength8 = runCommitLintOnMsg(
        commitMsgWithCodeBlockThatSubceedsBodyMinLineLength
    );

    expect(bodyParagraphLineMinLength8.status).toBe(0);
});

test("body-paragraph-line-min-length9", () => {
    const commitMsgThatHasAsteriskBullets = `Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
* Foo.
* Bar.`;

    const bodyParagraphLineMinLength9 = runCommitLintOnMsg(
        commitMsgThatHasAsteriskBullets
    );

    expect(bodyParagraphLineMinLength9.status).toBe(0);

    const commitMsgThatHasDashBullets = `Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
- Foo.
- Bar.`;

    const bodyParagraphLineMinLength9Prime = runCommitLintOnMsg(
        commitMsgThatHasDashBullets
    );

    expect(bodyParagraphLineMinLength9Prime.status).toBe(0);

    const commitMsgThatOnlyHasAsteriskBullets = `Fixed bug (a title of less than 50 chars)

* Foo.
* Bar.`;

    const bodyParagraphLineMinLength9DoublePrime = runCommitLintOnMsg(
        commitMsgThatOnlyHasAsteriskBullets
    );

    expect(bodyParagraphLineMinLength9DoublePrime.status).toBe(0);

    const commitMsgThatOnlyHasDashBullets = `Fixed bug (a title of less than 50 chars)

- Foo.
- Bar.`;

    const bodyParagraphLineMinLength9TriplePrime = runCommitLintOnMsg(
        commitMsgThatOnlyHasDashBullets
    );

    expect(bodyParagraphLineMinLength9TriplePrime.status).toBe(0);
});

test("body-paragraph-line-min-length10", () => {
    const commitMsgThatHasNumberedBullets = `Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
1. Foo.
2. Bar.`;

    const bodyParagraphLineMinLength10 = runCommitLintOnMsg(
        commitMsgThatHasNumberedBullets
    );

    expect(bodyParagraphLineMinLength10.status).toBe(0);

    const commitMsgThatOnlyHasNumberedBullets = `Fixed bug (a title of less than 50 chars)

1. Foo.
2. Bar.`;

    const bodyParagraphLineMinLength10Prime = runCommitLintOnMsg(
        commitMsgThatOnlyHasNumberedBullets
    );

    expect(bodyParagraphLineMinLength10Prime.status).toBe(0);
});

test("body-paragraph-line-min-length11", () => {
    const commitMsgThatHasMultiLineBullets = `Fixed bug (a title of less than 50 chars)

This is a bullet list of things:
* Foo bar baz foo bar baz foo bar baz foo bar baz foo bar baz
foo bar baz.
* Baz bar foo.`;

    const bodyParagraphLineMinLength11 = runCommitLintOnMsg(
        commitMsgThatHasMultiLineBullets
    );

    expect(bodyParagraphLineMinLength11.status).toBe(0);

    const commitMsgThatOnlyHasMultiLineBullets = `Fixed bug (a title of less than 50 chars)

* Foo bar baz foo bar baz foo bar baz foo bar baz foo bar baz
foo bar baz.
* Baz bar foo.`;

    const bodyParagraphLineMinLength11Prime = runCommitLintOnMsg(
        commitMsgThatOnlyHasMultiLineBullets
    );

    expect(bodyParagraphLineMinLength11Prime.status).toBe(0);
});

test("commit-hash-alone1", () => {
    const commitMsgWithCommitUrl = `foo: this is only a title

https://github.com/${process.env["GITHUB_REPOSITORY"]}/commit/3ee07243edc30604088a4b04ca525204ea440710`;

    const commitHashAlone1 = runCommitLintOnMsg(commitMsgWithCommitUrl);
    expect(commitHashAlone1.status).not.toBe(0);
});

test("commit-hash-alone2", () => {
    const commitMsgWithCommitHash = `foo: this is only a title

This is referring to [1] commit hash.

[1] 3ee07243edc30604088a4b04ca525204ea440710`;

    const commitHashAlone2 = runCommitLintOnMsg(commitMsgWithCommitHash);
    expect(commitHashAlone2.status).toBe(0);
});

test("commit-hash-alone3", () => {
    const commitMsgWithExternalCommitUrl = `foo: this is only a title

https://github.com/anotherOrg/anotherRepo/commit/3ee07243edc30604088a4b04ca525204ea440710`;

    const commitHashAlone3 = runCommitLintOnMsg(commitMsgWithExternalCommitUrl);
    expect(commitHashAlone3.status).toBe(0);
});

test("empty-wip-1", () => {
    const commitMsgWithEpmtyWIP = "WIP";
    const emptyWIP1 = runCommitLintOnMsg(commitMsgWithEpmtyWIP);
    expect(emptyWIP1.status).not.toBe(0);
});

test("empty-wip-2", () => {
    const commitMsgWithDescriptionAfterWIP = "WIP: bla bla blah";
    const emptyWIP2 = runCommitLintOnMsg(commitMsgWithDescriptionAfterWIP);
    expect(emptyWIP2.status).toBe(0);
});

test("empty-wip-3", () => {
    const commitMsgWithNumberAfterWIP = "WIP1";
    const emptyWIP3 = runCommitLintOnMsg(commitMsgWithNumberAfterWIP);
    expect(emptyWIP3.status).toBe(0);
});

test("footer-notes-misplacement-1", () => {
    const commitMsgWithRightFooter = `foo: this is only a title

Bla bla blah[1].

Fixes https://some/issue

[1] http://foo.bar/baz`;

    const footerNotesMisplacement1 = runCommitLintOnMsg(
        commitMsgWithRightFooter
    );
    expect(footerNotesMisplacement1.status).toBe(0);
});

test("footer-notes-misplacement-2", () => {
    const commitMsgWithWrongFooter = `foo: this is only a title

Fixes https://some/issue

Bla bla blah[1].

[1] http://foo.bar/baz`;

    const footerNotesMisplacement2 = runCommitLintOnMsg(
        commitMsgWithWrongFooter
    );
    expect(footerNotesMisplacement2.status).not.toBe(0);
});

test("footer-notes-misplacement-3", () => {
    const commitMsgWithWrongFooter = `foo: this is only a title

Bla bla blah[1]

[1] http://foo.bar/baz

Some other bla bla blah.

Fixes https://some/issue`;

    const footerNotesMisplacement3 = runCommitLintOnMsg(
        commitMsgWithWrongFooter
    );
    expect(footerNotesMisplacement3.status).not.toBe(0);
});

test("footer-notes-misplacement-4", () => {
    const commitMsgWithWrongFooter =
        "foo: this is only a title\n\n" +
        "Bla bla blah[1]:\n\n" +
        "```\nUnhandled Exception:\n--- Something between dashes ---\n```\n\n" +
        "[1] http://foo.bar/baz\n\n" +
        "Some other bla bla blah.\n\n" +
        "Fixes https://some/issue";

    const footerNotesMisplacement4 = runCommitLintOnMsg(
        commitMsgWithWrongFooter
    );
    expect(footerNotesMisplacement4.status).not.toBe(0);
});

test("footer-notes-misplacement-5", () => {
    const commitMsgWithRightFooter =
        "foo: this is only a title\n\n" +
        "Bla bla blah[1]:\n\n" +
        "```\nSome error message with a [] in the first of its line\n[warn] some warning\n```\n\n" +
        "[1] http://foo.bar/baz";
    const footerNotesMisplacement5 = runCommitLintOnMsg(
        commitMsgWithRightFooter
    );
    console.log(footerNotesMisplacement5.stdout.toString());
    expect(footerNotesMisplacement5.status).toBe(0);
});

test("footer-refs-validity1", () => {
    const commmitMsgWithCorrectFooter = `foo: this is only a title

Bla bla blah[1].

[1] http://foo.bar/baz`;

    const footerRefsValidity1 = runCommitLintOnMsg(commmitMsgWithCorrectFooter);
    expect(footerRefsValidity1.status).toBe(0);
});

test("footer-refs-validity2", () => {
    const commmitMsgWithWrongFooter = `foo: this is only a title

Bla bla blah.

[1] http://foo.bar/baz`;

    const footerRefsValidity2 = runCommitLintOnMsg(commmitMsgWithWrongFooter);
    expect(footerRefsValidity2.status).not.toBe(0);
});

test("footer-refs-validity3", () => {
    const commmitMsgWithWrongFooter = `foo: this is only a title

Bla bla blah[1], and after that [2], then [3].

[1] http://foo.bar/baz
[2] http://foo.bar/baz`;

    const footerRefsValidity3 = runCommitLintOnMsg(commmitMsgWithWrongFooter);
    expect(footerRefsValidity3.status).not.toBe(0);
});

test("footer-refs-validity4", () => {
    const commmitMsgWithFooter =
        "Backend/Ether: catch/retry new -32002 err code\n\n" +
        "CI on master branch caught this[1]:\n" +
        "```\nUnhandled Exception:\n" +
        "--- Something between dashes ---\n```\n" +
        "The end of the paragraph.\n\n" +
        "[1] https://github.com/nblockchain/geewallet/actions/runs/3507005645/jobs/5874411684";

    const footerRefsValidity4 = runCommitLintOnMsg(commmitMsgWithFooter);
    expect(footerRefsValidity4.status).toBe(0);
});

test("footer-refs-validity5", () => {
    const commmitMsgWithEOLFooter = `foo: this is only a title

Bla bla blah[1].

[1]
http://foo.bar/baz`;

    const footerRefsValidity5 = runCommitLintOnMsg(commmitMsgWithEOLFooter);
    const output1 = footerRefsValidity5.output[1];
    expect(output1).not.toBeNull();
    if (output1 === null) {
        // redundant, but TypeScript compiler produces error otherwise
        return;
    }
    expect(output1.toString().includes("EOL")).toBe(true);
});

test("reject-em-dash1", () => {
    const commitMsgWithEmDashAsBullet = `foo: this is only a title

This is a bullet list of things:
— Foo.
— Bar.`;

    const rejectEmDash1 = runCommitLintOnMsg(commitMsgWithEmDashAsBullet);
    expect(rejectEmDash1.status).not.toBe(0);

    const commitMsgWithEmDashAsWordUnion = `foo: this is only a title

Foo — bar baz.`;

    const rejectEmDash1Prime = runCommitLintOnMsg(
        commitMsgWithEmDashAsWordUnion
    );
    expect(rejectEmDash1Prime.status).not.toBe(0);

    const commitMsgWithEmDashInTitle = `foo: this is — only a title

Bla blah bla.`;

    const rejectEmDash1DoublePrime = runCommitLintOnMsg(
        commitMsgWithEmDashInTitle
    );
    expect(rejectEmDash1DoublePrime.status).not.toBe(0);
});

test("reject-em-dash2", () => {
    const commitMsgWithoutAnyDash = `foo: this is only a title

Bla blah bla.`;

    const rejectEmDash2 = runCommitLintOnMsg(commitMsgWithoutAnyDash);
    expect(rejectEmDash2.status).toBe(0);

    const commitMsgWithNormalDash = `foo: this is only a title

Foo - bar baz.`;

    const rejectEmDash2Prime = runCommitLintOnMsg(commitMsgWithNormalDash);
    expect(rejectEmDash2Prime.status).toBe(0);
});

test("footer-refs-validity6", () => {
    const commitMsgWithUrlContainingAnchor = `foo: blah blah

Blah blah blah[1].

[1] https://somehost/somePath/someRes#7-some-numbered-anchor`;

    const footerRefsValidity6 = runCommitLintOnMsg(
        commitMsgWithUrlContainingAnchor
    );
    expect(footerRefsValidity6.status).toBe(0);
});

// This test reflects this issue: https://github.com/nblockchain/conventions/issues/125
test("footer-refs-validity7", () => {
    const commitMsgWithWithoutFooter = "foo: blah blah" + "\n\n" + "```[1]```";

    const footerRefsValidity7 = runCommitLintOnMsg(commitMsgWithWithoutFooter);
    expect(footerRefsValidity7.status).toBe(0);
});

// This test reflects this issue: https://github.com/nblockchain/conventions/issues/146
test("footer-refs-validity8", () => {
    const commitMsgWithCodeBlockAtFooterRef =
        "foo: blah blah" +
        "\n\n" +
        "Blah blah blah[1]." +
        "\n\n" +
        "[1]:\n" +
        "```\n" +
        "someCodeBlock\n" +
        "```";
    const footerRefsValidity8 = runCommitLintOnMsg(
        commitMsgWithCodeBlockAtFooterRef
    );
    expect(footerRefsValidity8.status).toBe(0);
});

// This test reflects this issue: https://github.com/nblockchain/conventions/issues/148
test("footer-refs-validity9", () => {
    const commitMsgWithTwoCodeBlocksAtBodyWithRef =
        "foo: blah blah" +
        "\n\n" +
        "Blah blah [1]:" +
        "\n\n" +
        "```\nsomeCodeBlock\n```" +
        "\n\n" +
        "[1] Stack trace:" +
        "\n\n" +
        "```\nsomeCodeBlock\n```";
    const footerRefsValidity9 = runCommitLintOnMsg(
        commitMsgWithTwoCodeBlocksAtBodyWithRef
    );
    expect(footerRefsValidity9.status).toBe(0);
});

test("prefer-slash-over-backslash1", () => {
    const commitMsgWithBackslash = "foo\\bar: bla bla bla";
    const preferSlashOverBackslash1 = runCommitLintOnMsg(
        commitMsgWithBackslash
    );
    expect(preferSlashOverBackslash1.status).not.toBe(0);
});

test("prefer-slash-over-backslash2", () => {
    const commitMsgWithSlash = "foo/bar: bla bla bla";
    const preferSlashOverBackslash2 = runCommitLintOnMsg(commitMsgWithSlash);
    expect(preferSlashOverBackslash2.status).toBe(0);
});

test("header-max-length-with-suggestions1", () => {
    const commitMsgWithThatExceedsHeaderMaxLength =
        "foo: this is only a title with a configuration in it that exceeds header max length";
    const headerMaxLength1 = runCommitLintOnMsg(
        commitMsgWithThatExceedsHeaderMaxLength
    );
    const expected_message = `"configuration" -> "config"`;
    expect(headerMaxLength1.status).not.toBe(0);
    expect((headerMaxLength1.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions2", () => {
    const commitMsgWithThatExceedsHeaderMaxLength =
        "foo: this is only a title with a 1 second in it that exceeds header max length";
    const headerMaxLength2 = runCommitLintOnMsg(
        commitMsgWithThatExceedsHeaderMaxLength
    );
    const expected_message = `"1 second" -> "1sec"`;
    expect(headerMaxLength2.status).not.toBe(0);
    expect((headerMaxLength2.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions3", () => {
    const commitMsgWithOnlyTwentySixCharsInTitle = "foo: this is only a title";
    const headerMaxLength3 = runCommitLintOnMsg(
        commitMsgWithOnlyTwentySixCharsInTitle
    );
    expect(headerMaxLength3.status).toBe(0);
});

test("header-max-length-with-suggestions4", () => {
    const tenChars = "1234 12345";
    const commitMsgWithOnlyFiftyCharsInTitle =
        "foo: 12345" + tenChars + tenChars + tenChars + tenChars;
    const headerMaxLength4 = runCommitLintOnMsg(
        commitMsgWithOnlyFiftyCharsInTitle
    );
    expect(headerMaxLength4.status).toBe(0);
});

test("header-max-length-with-suggestions5", () => {
    const commitMsgWithThatExceedsHeaderMaxLength =
        "Upgrade foo bla bla bla bla bla bla bla bla bla bla bla bla bla bla";
    const headerMaxLength6 = runCommitLintOnMsg(
        commitMsgWithThatExceedsHeaderMaxLength
    );
    const expected_message = `"upgrade" -> "update"`;
    expect(headerMaxLength6.status).not.toBe(0);
    expect((headerMaxLength6.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions6", () => {
    const commitMsgWithThatExceedsHeaderMaxLength =
        "configure: this is the very very very very very long title";
    const headerMaxLength7 = runCommitLintOnMsg(
        commitMsgWithThatExceedsHeaderMaxLength
    );
    const expected_message = `"configure" -> "config"`;
    expect(headerMaxLength7.status).not.toBe(0);
    expect((headerMaxLength7.stdout + "").includes(expected_message)).toEqual(
        false
    );
});

test("header-max-length-with-suggestions7", () => {
    const commitMsgThatExceedsHeaderMaxLength =
        "Fix android build because blah blah very very very very very long title";
    const headerMaxLength8 = runCommitLintOnMsg(
        commitMsgThatExceedsHeaderMaxLength
    );
    const not_expected_message = `"and" -> "&"`;
    expect(headerMaxLength8.status).not.toBe(0);
    expect(
        (headerMaxLength8.stdout + "").includes(not_expected_message)
    ).toEqual(false);
});

test("header-max-length-with-suggestions8", () => {
    const commitMsgThatExceedsHeaderMaxLength =
        "title: 1 second bla bla bla bla bla bla bla bla bla bla bla bla bla bla";
    const headerMaxLength9 = runCommitLintOnMsg(
        commitMsgThatExceedsHeaderMaxLength
    );
    const expected_message = `"1 second" -> "1sec"`;
    expect(headerMaxLength9.status).not.toBe(0);
    expect((headerMaxLength9.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions9", () => {
    const commitMsgThatExceedsHeaderMaxLength =
        "Configuration simplification bla bla bla bla bla bla bla bla bla bla bla";
    const headerMaxLength10 = runCommitLintOnMsg(
        commitMsgThatExceedsHeaderMaxLength
    );
    const expected_message = `"configuration" -> "config"`;
    expect(headerMaxLength10.status).not.toBe(0);
    expect((headerMaxLength10.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions10", () => {
    const commitMsgThatExceedsHeaderMaxLength =
        "scope: 20 characters more because blah blah very very very very long title";
    const headerMaxLength11 = runCommitLintOnMsg(
        commitMsgThatExceedsHeaderMaxLength
    );
    const expected_message = `"characters" -> "chars"`;
    expect(headerMaxLength11.status).not.toBe(0);
    expect((headerMaxLength11.stdout + "").includes(expected_message)).toEqual(
        true
    );
});

test("header-max-length-with-suggestions11", () => {
    const commitMsgThatExceedsHeaderMaxLength =
        "Split that compares better because blah blah bla very very very long title";
    const headerMaxLength12 = runCommitLintOnMsg(
        commitMsgThatExceedsHeaderMaxLength
    );
    const not_expected_message = `"compares" -> "cmps"`;
    expect(headerMaxLength12.status).not.toBe(0);
    expect(
        (headerMaxLength12.stdout + "").includes(not_expected_message)
    ).toEqual(false);
});

test("proper-issue-refs1", () => {
    const commitMsgWithHashtagRef = `foo: blah blah

Blah blah #123.`;

    const properIssueRefs1 = runCommitLintOnMsg(commitMsgWithHashtagRef);
    expect(properIssueRefs1.status).not.toBe(0);
});

test("proper-issue-refs2", () => {
    const commitMsgWithFullUrl = `foo: blah blah

Fixes someUrl://blah.blah/158`;

    const properIssueRefs2 = runCommitLintOnMsg(commitMsgWithFullUrl);
    expect(properIssueRefs2.status).toBe(0);
});

test("proper-issue-refs3", () => {
    const commitMsgWithHashtagRefInBlock =
        "foo: this is only a title" +
        "\n\n" +
        "Bar baz:\n\n```\ntype Foo = string #123\n```";
    const properIssueRefs3 = runCommitLintOnMsg(commitMsgWithHashtagRefInBlock);
    expect(properIssueRefs3.status).toBe(0);
});

test("proper-issue-refs4", () => {
    const commitMsgWithFullUrl = `foo: blah blah

Some paragraph text with a ref[1].

[1] someUrl://someHostName/someFolder/someResource#666-anchor`;

    const properIssueRefs4 = runCommitLintOnMsg(commitMsgWithFullUrl);
    expect(properIssueRefs4.status).toBe(0);
});

test("proper-issue-refs5", () => {
    const commitMsgWithHashtagRef = `foo: blah blah

#123 bug is fixed.`;

    const properIssueRefs5 = runCommitLintOnMsg(commitMsgWithHashtagRef);
    expect(properIssueRefs5.status).not.toBe(0);
});

test("subject-lowercase1", () => {
    const commitMsgWithUppercaseAfterColon = "foo: Bar baz";
    const subjectLowerCase1 = runCommitLintOnMsg(
        commitMsgWithUppercaseAfterColon
    );
    expect(subjectLowerCase1.status).not.toBe(0);
});

test("subject-lowercase2", () => {
    const commitMsgWithLowercaseAfterColon = "foo: bar baz";
    const subjectLowerCase2 = runCommitLintOnMsg(
        commitMsgWithLowercaseAfterColon
    );
    expect(subjectLowerCase2.status).toBe(0);
});

test("subject-lowercase3", () => {
    const commitMsgWithAcronymAfterColon = "foo: BAR baz";
    const subjectLowerCase3 = runCommitLintOnMsg(
        commitMsgWithAcronymAfterColon
    );
    expect(subjectLowerCase3.status).toBe(0);
});

test("subject-lowercase4", () => {
    const commitMsgWithNonAlphanumericAfterColon = "foo: 3 tests added";
    const subjectLowerCase4 = runCommitLintOnMsg(
        commitMsgWithNonAlphanumericAfterColon
    );
    expect(subjectLowerCase4.status).toBe(0);
});

test("subject-lowercase5", () => {
    const commitMsgWithRareCharInScope1 = "foo.bar: Baz";
    const subjectLowerCase5 = runCommitLintOnMsg(commitMsgWithRareCharInScope1);
    expect(subjectLowerCase5.status).not.toBe(0);
});

test("subject-lowercase6", () => {
    const commitMsgWithRareCharInScope2 = "foo-bar: Baz";
    const subjectLowerCase6 = runCommitLintOnMsg(commitMsgWithRareCharInScope2);
    expect(subjectLowerCase6.status).not.toBe(0);
});

test("subject-lowercase7", () => {
    const commitMsgWithRareCharInScope3 = "foo,bar: Baz";
    const subjectLowerCase7 = runCommitLintOnMsg(commitMsgWithRareCharInScope3);
    expect(subjectLowerCase7.status).not.toBe(0);
});

test("subject-lowercase8", () => {
    const commitMsgWithPascalCaseAfterColon =
        "End2End: TestFixtureSetup refactor";
    const subjectLowerCase8 = runCommitLintOnMsg(
        commitMsgWithPascalCaseAfterColon
    );
    expect(subjectLowerCase8.status).toBe(0);
});

test("subject-lowercase9", () => {
    const commitMsgWithCamelCaseAfterColon =
        "End2End: testFixtureSetup refactor";
    const subjectLowerCase9 = runCommitLintOnMsg(
        commitMsgWithCamelCaseAfterColon
    );
    expect(subjectLowerCase9.status).toBe(0);
});

test("subject-lowercase10", () => {
    const commitMsgWithNumber = "foo: A1 bar";
    const subjectLowerCase10 = runCommitLintOnMsg(commitMsgWithNumber);
    expect(subjectLowerCase10.status).toBe(0);
});

test("title-uppercase1", () => {
    const commitMsgWithoutScope = "remove logs";
    const titleUpperCase1 = runCommitLintOnMsg(commitMsgWithoutScope);
    expect(titleUpperCase1.status).not.toBe(0);
});

test("title-uppercase2", () => {
    const commitMsgWithoutScope = "Remove logs";
    const titleUpperCase2 = runCommitLintOnMsg(commitMsgWithoutScope);
    expect(titleUpperCase2.status).toBe(0);
});

test("title-uppercase3", () => {
    const commitMsgWithoutScope = "testFixtureSetup refactor";
    const titleUpperCase3 = runCommitLintOnMsg(commitMsgWithoutScope);
    expect(titleUpperCase3.status).toBe(0);
});

test("title-uppercase4", () => {
    const commitMsgWithLowerCaseScope = "lowercase: scope is lowercase";
    const titleUpperCase4 = runCommitLintOnMsg(commitMsgWithLowerCaseScope);
    expect(titleUpperCase4.status).toBe(0);
});

test("too-many-spaces1", () => {
    const commitMsgWithTooManySpacesInTitle = "foo: this is only a  title";
    const tooManySpaces1 = runCommitLintOnMsg(
        commitMsgWithTooManySpacesInTitle
    );
    expect(tooManySpaces1.status).not.toBe(0);
});

test("too-many-spaces2", () => {
    const commitMsgWithTooManySpacesInBody = `foo: this is only a title

Bla  blah bla.`;

    const tooManySpaces2 = runCommitLintOnMsg(commitMsgWithTooManySpacesInBody);
    expect(tooManySpaces2.status).not.toBe(0);
});

test("too-many-spaces3", () => {
    const commitMsgWithTooManySpacesInCodeBlock =
        "foo: this is only a title\n\n" +
        "Bar baz:\n\n```\ntype   Foo =\nstring\n```";
    const tooManySpaces3 = runCommitLintOnMsg(
        commitMsgWithTooManySpacesInCodeBlock
    );
    expect(tooManySpaces3.status).toBe(0);
});

test("too-many-spaces4", () => {
    const commitMsgWithTwoSpacesAfterSentence = `foo: this is only a title

Bla blah.  Blah bla.`;

    const tooManySpaces4 = runCommitLintOnMsg(
        commitMsgWithTwoSpacesAfterSentence
    );
    expect(tooManySpaces4.status).toBe(0);
});

test("too-many-spaces5", () => {
    const commitMsgWithThreeSpacesAfterSentence = `foo: this is only a title

Bla blah.   Blah bla.`;

    const tooManySpaces5 = runCommitLintOnMsg(
        commitMsgWithThreeSpacesAfterSentence
    );
    expect(tooManySpaces5.status).not.toBe(0);
});

test("trailing-whitespace1", () => {
    const commitMsgWithNoTrailingWhiteSpace = `foo: this is only a title

Bla blah bla.`;

    const trailingWhitespace1 = runCommitLintOnMsg(
        commitMsgWithNoTrailingWhiteSpace
    );
    expect(trailingWhitespace1.status).toBe(0);
});

test("trailing-whitespace2", () => {
    const commitMsgWithTrailingWhiteSpaceInTitleEnd = `foo: title 

Bla blah bla.`;

    const trailingWhitespace2 = runCommitLintOnMsg(
        commitMsgWithTrailingWhiteSpaceInTitleEnd
    );
    expect(trailingWhitespace2.status).not.toBe(0);
});

test("trailing-whitespace3", () => {
    const commitMsgWithTrailingWhiteSpaceInTitleStart = ` foo: title

Bla blah bla.`;

    const trailingWhitespace3 = runCommitLintOnMsg(
        commitMsgWithTrailingWhiteSpaceInTitleStart
    );
    expect(trailingWhitespace3.status).not.toBe(0);
});

test("trailing-whitespace4", () => {
    const commitMsgWithTrailingWhiteSpaceInBodyStart = `foo: title

 Bla blah bla.`;

    const trailingWhitespace4 = runCommitLintOnMsg(
        commitMsgWithTrailingWhiteSpaceInBodyStart
    );
    expect(trailingWhitespace4.status).not.toBe(0);
});

test("trailing-whitespace5", () => {
    const commitMsgWithTrailingWhiteSpaceInBodyEnd = `foo: title

Bla blah bla. `;

    const trailingWhitespace5 = runCommitLintOnMsg(
        commitMsgWithTrailingWhiteSpaceInBodyEnd
    );
    expect(trailingWhitespace5.status).not.toBe(0);
});

test("trailing-whitespace6", () => {
    const commitMsgWithTrailingWhiteSpaceInCodeBlock =
        "foo: this is only a title" +
        "\n\n" +
        "Bar baz:\n\n```\ntype Foo =\n    string\n```";
    const trailingWhitespace6 = runCommitLintOnMsg(
        commitMsgWithTrailingWhiteSpaceInCodeBlock
    );
    expect(trailingWhitespace6.status).toBe(0);
});

test("type-space-after-colon1", () => {
    const commitMsgWithNoSpace = "foo:bar";
    const typeSpaceAfterColon1 = runCommitLintOnMsg(commitMsgWithNoSpace);
    expect(typeSpaceAfterColon1.status).not.toBe(0);
});

test("type-space-after-colon2", () => {
    const commitMsgWithSpace = "foo: bar";
    const typeSpaceAfterColon2 = runCommitLintOnMsg(commitMsgWithSpace);
    expect(typeSpaceAfterColon2.status).toBe(0);
});

test("type-space-after-colon3", () => {
    const commitMsgWithNoSpaceBeforeColonButAtTheEnd = "foo: a tale of bar:baz";
    const typeSpaceAfterColon3 = runCommitLintOnMsg(
        commitMsgWithNoSpaceBeforeColonButAtTheEnd
    );
    expect(typeSpaceAfterColon3.status).toBe(0);
});

test("type-space-after-comma1", () => {
    const commitMsgWithSpaceAfterCommaInType = "foo, bar: bla bla blah";
    const typeSpaceAfterComma1 = runCommitLintOnMsg(
        commitMsgWithSpaceAfterCommaInType
    );
    expect(typeSpaceAfterComma1.status).not.toBe(0);
});

test("type-space-after-comma2", () => {
    const commitMsgWithNoSpaceAfterCommaInType = "foo,bar: bla bla blah";
    const typeSpaceAfterComma2 = runCommitLintOnMsg(
        commitMsgWithNoSpaceAfterCommaInType
    );
    expect(typeSpaceAfterComma2.status).toBe(0);
});

test("type-space-before-paren1", () => {
    const commitMsgWithNoSpaceBeforeParen = "foo (bar): bla bla bla";
    const typeSpaceBeforeParen1 = runCommitLintOnMsg(
        commitMsgWithNoSpaceBeforeParen
    );
    expect(typeSpaceBeforeParen1.status).not.toBe(0);
});

test("type-space-before-paren2", () => {
    const commitMsgWithNoSpaceBeforeParen = "foo(bar): bla bla bla";
    const typeSpaceBeforeParen2 = runCommitLintOnMsg(
        commitMsgWithNoSpaceBeforeParen
    );
    expect(typeSpaceBeforeParen2.status).toBe(0);
});

test("type-space-before-paren3", () => {
    const commitMsgWithNoSpaceBeforeParen = "(bar): bla bla bla";
    const typeSpaceBeforeParen3 = runCommitLintOnMsg(
        commitMsgWithNoSpaceBeforeParen
    );
    expect(typeSpaceBeforeParen3.status).toBe(0);
});

test("type-with-square-brackets1", () => {
    const commitMsgWithSquareBrackets = "[foo] this is a title";
    const typeWithSquareBrackets1 = runCommitLintOnMsg(
        commitMsgWithSquareBrackets
    );
    expect(typeWithSquareBrackets1.status).not.toBe(0);
});

test("type-with-square-brackets2", () => {
    const commitMsgWithoutSquareBrackets = "foo: this is a title";
    const typeWithSquareBrackets2 = runCommitLintOnMsg(
        commitMsgWithoutSquareBrackets
    );
    expect(typeWithSquareBrackets2.status).toBe(0);
});
