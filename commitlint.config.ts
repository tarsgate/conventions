/* eslint @typescript-eslint/no-explicit-any: "off" */

import { Helpers } from "./commitlint/helpers.js";
import { Plugins } from "./commitlint/plugins.js";
import { RuleConfigSeverity } from "@commitlint/types";

const bodyMaxLineLength = 64;
const headerMaxLineLength = 50;
const footerMaxLineLength = 150;

function notStringErrorMessage(variableName: string): string {
    return `This is unexpected because ${variableName} should have been a string`;
}

function extractStringFromCommitlintParam(
    paramName: string,
    variable: any
): string {
    const str = Helpers.assertNotNone(
        Helpers.convertAnyToString(variable),
        notStringErrorMessage(paramName)
    );
    return str;
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
        "footer-leading-blank": [RuleConfigSeverity.Warning, "always"],
        "footer-max-line-length": [
            RuleConfigSeverity.Error,
            "always",
            footerMaxLineLength,
        ],
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
        "reject-em-dash": [RuleConfigSeverity.Error, "always"],
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

                    return Plugins.bodyProse(rawStr);
                },

                "commit-hash-alone": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.commitHashAlone(rawStr);
                },

                "reject-em-dash": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.rejectEmDash(rawStr);
                },

                "empty-wip": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.emptyWip(headerStr);
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

                    return Plugins.headerMaxLengthWithSuggestions(
                        headerStr,
                        maxLineLength
                    );
                },

                "footer-notes-misplacement": ({ body }: { body: any }) => {
                    const maybeBody = Helpers.convertAnyToString(body);
                    return Plugins.footerNotesMisplacement(maybeBody);
                },

                "footer-refs-validity": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.footerRefsValidity(rawStr);
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

                    return Plugins.preferSlashOverBackslash(headerStr);
                },

                "proper-issue-refs": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.properIssueRefs(rawStr);
                },

                "title-uppercase": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.titleUppercase(headerStr);
                },

                "too-many-spaces": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.tooManySpaces(rawStr);
                },

                "type-space-after-colon": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.typeSpaceAfterColon(headerStr);
                },

                "type-with-square-brackets": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.typeWithSquareBrackets(headerStr);
                },

                // NOTE: we use 'header' instead of 'subject' as a workaround to this bug: https://github.com/conventional-changelog/commitlint/issues/3404
                "subject-lowercase": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.subjectLowercase(headerStr);
                },

                "type-space-after-comma": ({ header }: { header: any }) => {
                    const headerStr = Helpers.assertNotNone(
                        Helpers.convertAnyToString(header),
                        notStringErrorMessage("header")
                    );

                    return Plugins.typeSpaceAfterComma(headerStr);
                },

                "body-soft-max-line-length": (
                    { body }: { body: any },
                    _: any,
                    maxLineLength: number
                ) => {
                    const maybeBody = Helpers.convertAnyToString(body);
                    return Plugins.bodySoftMaxLineLength(
                        maybeBody,
                        maxLineLength
                    );
                },

                "body-paragraph-line-min-length": ({ body }: { body: any }) => {
                    const maybeBody = Helpers.convertAnyToString(body);
                    return Plugins.bodyParagraphLineMinLength(
                        maybeBody,
                        headerMaxLineLength,
                        bodyMaxLineLength
                    );
                },

                "trailing-whitespace": ({ raw }: { raw: any }) => {
                    const rawStr = extractStringFromCommitlintParam("raw", raw);

                    return Plugins.trailingWhitespace(rawStr);
                },

                "type-space-before-paren": ({ header }: { header: any }) => {
                    const headerStr = extractStringFromCommitlintParam(
                        "header",
                        header
                    );

                    return Plugins.typeSpaceBeforeParen(headerStr);
                },
            },
        },
    ],
};
