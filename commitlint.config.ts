/* eslint @typescript-eslint/no-explicit-any: "off" */

import { Helpers } from "./commitlint/helpers.js";
import { Plugins } from "./commitlint/plugins.js";
import { Dyn, Option, Some } from "fp-sdk";
import { RuleConfigSeverity } from "@commitlint/types";

const bodyMaxLineLength = 64;
const headerMaxLineLength = 50;
/* see comment below in footer-max-line-length rule to see why this is disabled
const footerMaxLineLength = 150;
*/

function notStringErrorMessage(variableName: string): string {
    return `This is unexpected because ${variableName} should have been a string`;
}

function extractStringFromCommitlintParam(
    paramName: string,
    variable: any
): string {
    const str = Helpers.assertNotNone(
        Dyn.tryCast(variable).to(String),
        notStringErrorMessage(paramName)
    );
    return str;
}

// FIXME: maybe propose PR to upstream commitlint to not have such a loosely typed API?
function convertToCommitlintResponse(
    result: Option<string>
): Array<string | boolean> {
    if (result instanceof Some) {
        return [false, result.value];
    }
    return [true];
}

export default {
    ignores: [(commit: string) => commit.startsWith("Merge ")],
    parserPreset: "conventional-changelog-conventionalcommits",
    rules: {
        "body-leading-blank": [RuleConfigSeverity.Error, "always"],
        "body-soft-max-line-length": [
            RuleConfigSeverity.Error,
            "always",
            bodyMaxLineLength,
        ],
        "body-paragraph-line-min-length": [RuleConfigSeverity.Error, "always"],
        "empty-wip": [RuleConfigSeverity.Error, "always"],
        /* disabling these 2 rules above as a workaround for commitlint-parser detecting trailers
   inside blocks as already part of the footer, see https://github.com/tarsgate/conventions/pull/389
        "footer-leading-blank": [RuleConfigSeverity.Warning, "always"],
        "footer-max-line-length": [
            RuleConfigSeverity.Error,
            "always",
            footerMaxLineLength,
        ],
*/
        "footer-notes-misplacement": [RuleConfigSeverity.Error, "always"],
        "footer-refs-validity": [RuleConfigSeverity.Error, "always"],
        "header-max-length-with-suggestions": [
            RuleConfigSeverity.Error,
            "always",
            headerMaxLineLength,
        ],
        "subject-full-stop": [RuleConfigSeverity.Error, "never", "."],
        "type-space-after-colon": [RuleConfigSeverity.Error, "always"],
        "subject-lowercase": [RuleConfigSeverity.Error, "always"],
        "body-prose": [RuleConfigSeverity.Error, "always"],
        "type-space-after-comma": [RuleConfigSeverity.Error, "always"],
        "trailing-whitespace": [RuleConfigSeverity.Error, "always"],
        "prefer-slash-over-backslash": [RuleConfigSeverity.Error, "always"],
        "type-space-before-paren": [RuleConfigSeverity.Error, "always"],
        "type-with-square-brackets": [RuleConfigSeverity.Error, "always"],
        "proper-issue-refs": [RuleConfigSeverity.Error, "always"],
        "too-many-spaces": [RuleConfigSeverity.Error, "always"],
        "commit-hash-alone": [RuleConfigSeverity.Error, "always"],
        "title-uppercase": [RuleConfigSeverity.Error, "always"],

        // disabled because most of the time it doesn't work, due to https://github.com/conventional-changelog/commitlint/issues/3404
        // and anyway we were using this rule only as a warning, not an error (because a scope is not required, e.g. when too broad)
        "type-empty": [RuleConfigSeverity.Disabled, "never"],
    },
    plugins: [
        // TODO (ideas for more rules):
        // * Detect reverts which have not been elaborated.
        // * Reject some stupid obvious words: change, update, modify (if first word after colon, error; otherwise warning).
        // * Think of how to reject this shitty commit message: https://github.com/nblockchain/NOnion/pull/34/commits/9ffcb373a1147ed1c729e8aca4ffd30467255594
        // * Workflow: detect if wip commit in a branch not named "wip/*" or whose name contains "squashed".
        // * Detect if commit hash mention in commit msg actually exists in repo.
        // * Detect scope(sub-scope) in the title that doesn't include scope part (e.g., writing (bar) instead of foo(bar))

        {
            rules: {
                "body-prose": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.bodyProse(rawStr)
                    );
                },

                "commit-hash-alone": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.commitHashAlone(rawStr)
                    );
                },

                "empty-wip": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.emptyWip(headerStr)
                    );
                },

                "header-max-length-with-suggestions": (
                    { header }: { header: any },
                    _: any,
                    maxLineLength: number
                ) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.headerMaxLengthWithSuggestions(
                            headerStr,
                            maxLineLength
                        )
                    );
                },

                "footer-notes-misplacement": ({ raw }: { raw: any }) => {
                    const maybeBody = Helpers.extractBodyFromCommitMsg(
                        extractStringFromCommitlintParam("raw", raw)
                    );
                    return convertToCommitlintResponse(
                        Plugins.footerNotesMisplacement(maybeBody)
                    );
                },

                "footer-refs-validity": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.footerRefsValidity(rawStr)
                    );
                },

                "prefer-slash-over-backslash": ({
                    header,
                }: {
                    header: any;
                }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.preferSlashOverBackslash(headerStr)
                    );
                },

                "proper-issue-refs": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.properIssueRefs(rawStr)
                    );
                },

                "title-uppercase": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.titleUppercase(headerStr)
                    );
                },

                "too-many-spaces": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.tooManySpaces(rawStr)
                    );
                },

                "type-space-after-colon": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.typeSpaceAfterColon(headerStr)
                    );
                },

                "type-with-square-brackets": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.typeWithSquareBrackets(headerStr)
                    );
                },

                // NOTE: we use 'header' instead of 'subject' as a workaround to this bug: https://github.com/conventional-changelog/commitlint/issues/3404
                "subject-lowercase": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.subjectLowercase(headerStr)
                    );
                },

                "type-space-after-comma": ({ header }: { header: any }) => {
                    const headerStr = Helpers.assertNotNone(
                        Dyn.tryCast(header).to(String),
                        notStringErrorMessage("header")
                    );

                    return convertToCommitlintResponse(
                        Plugins.typeSpaceAfterComma(headerStr)
                    );
                },

                "body-soft-max-line-length": (
                    { raw }: { raw: any },
                    _: any,
                    maxLineLength: number
                ) => {
                    const maybeBody = Helpers.extractBodyFromCommitMsg(
                        extractStringFromCommitlintParam("raw", raw)
                    );
                    return convertToCommitlintResponse(
                        Plugins.bodySoftMaxLineLength(maybeBody, maxLineLength)
                    );
                },

                "body-paragraph-line-min-length": ({ raw }: { raw: any }) => {
                    const maybeBody = Helpers.extractBodyFromCommitMsg(
                        extractStringFromCommitlintParam("raw", raw)
                    );

                    return convertToCommitlintResponse(
                        Plugins.bodyParagraphLineMinLength(
                            maybeBody,
                            headerMaxLineLength,
                            bodyMaxLineLength
                        )
                    );
                },

                "trailing-whitespace": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return convertToCommitlintResponse(
                        Plugins.trailingWhitespace(rawStr)
                    );
                },

                "type-space-before-paren": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return convertToCommitlintResponse(
                        Plugins.typeSpaceBeforeParen(headerStr)
                    );
                },
            },
        },
    ],
};
