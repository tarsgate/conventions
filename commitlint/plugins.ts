import { Option, Some, None, Nothing, OptionHelpers } from "fp-sdk";
import { abbr } from "./abbreviations.js";
import { Helpers } from "./helpers.js";

export abstract class Plugins {
    public static bodyProse(rawStr: string) {
        function paragraphHasValidEnding(paragraph: string): boolean {
            const paragraphWords = paragraph.split(" ");
            const lastWordInParagraph =
                paragraphWords[paragraphWords.length - 1];
            const isParagraphEndingWithUrl =
                Helpers.isValidUrl(lastWordInParagraph);
            if (isParagraphEndingWithUrl) {
                return true;
            }

            const endingChar = paragraph[paragraph.length - 1];
            if (
                endingChar === "." ||
                endingChar === ":" ||
                endingChar === "!" ||
                endingChar === "?"
            ) {
                return true;
            }

            if (
                endingChar === ")" &&
                paragraph.length > 1 &&
                paragraphHasValidEnding(paragraph[paragraph.length - 2])
            ) {
                return true;
            }

            return false;
        }

        let offence = false;

        rawStr = rawStr.trim();
        const lineBreakIndex = rawStr.indexOf("\n");

        if (lineBreakIndex >= 0) {
            // Extracting bodyStr from rawStr rather than using body directly is a
            // workaround for https://github.com/conventional-changelog/commitlint/issues/3412
            let bodyStr = rawStr.substring(lineBreakIndex).trim();

            bodyStr = Helpers.removeAllCodeBlocks(bodyStr).trim();

            if (bodyStr !== "") {
                for (let paragraph of Helpers.splitByEOLs(bodyStr, 2)) {
                    paragraph = paragraph.trim();

                    if (paragraph === "") {
                        continue;
                    }

                    const startWithLowerCase = Helpers.isLowerCase(
                        paragraph[0]
                    );

                    const validParagraphEnd =
                        paragraphHasValidEnding(paragraph);

                    const lines = Helpers.splitByEOLs(paragraph, 1);

                    if (startWithLowerCase) {
                        if (
                            !(lines.length == 1 && Helpers.isValidUrl(lines[0]))
                        ) {
                            offence = true;
                        }
                    }

                    if (
                        !validParagraphEnd &&
                        !Helpers.isValidUrl(lines[lines.length - 1]) &&
                        !Helpers.isFooterNote(lines[lines.length - 1])
                    ) {
                        offence = true;
                    }
                }
            }
        }

        return [
            !offence,
            `The body of the commit message (as opposed to the commit message title) is composed of paragraphs. Please begin each paragraph with an uppercase letter and end it with a dot (or other valid character to finish a paragraph).` +
                Helpers.errMessageSuffix,
        ];
    }

    public static commitHashAlone(rawStr: string) {
        let offence = false;

        const urls = Helpers.findUrls(rawStr);

        const gitRepo = OptionHelpers.OfObj(process.env["GITHUB_REPOSITORY"]);
        if (gitRepo instanceof Some) {
            if (urls instanceof Some) {
                for (const url of urls.value.entries()) {
                    const urlStr = url[1].toString();
                    if (
                        Helpers.isCommitUrl(urlStr) &&
                        urlStr.includes(gitRepo.value)
                    ) {
                        offence = true;
                        break;
                    }
                }
            }
        }

        return [
            !offence,
            `Please use the commit hash instead of the commit full URL.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static emptyWip(headerStr: string) {
        const offence = headerStr.toLowerCase() === "wip";
        return [
            !offence,
            `Please add a number or description after the WIP prefix.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static headerMaxLengthWithSuggestions(
        headerStr: string,
        maxLineLength: number
    ) {
        let offence = false;

        const headerLength = headerStr.length;
        let message = `Please do not exceed ${maxLineLength} characters in title (found ${headerLength}).`;
        if (headerLength > maxLineLength) {
            offence = true;

            const colonIndex = headerStr.indexOf(":");

            let titleWithoutScope = headerStr;
            if (colonIndex > 0) {
                titleWithoutScope = headerStr.substring(colonIndex);
            }

            const numRecomendations = 0;
            const lowerCaseTitleWithoutScope = titleWithoutScope.toLowerCase();
            Object.entries(abbr).forEach(([key, value]) => {
                const pattern = new RegExp("\\b(" + key.toString() + ")\\b");
                if (pattern.test(lowerCaseTitleWithoutScope)) {
                    if (numRecomendations === 0) {
                        message =
                            message +
                            " The following replacement(s) in your commit title are recommended:\n";
                    }

                    message = message + `"${key}" -> "${value}"\n`;
                }
            });
        }

        return [!offence, message + Helpers.errMessageSuffix];
    }

    public static footerNotesMisplacement(body: Option<string>) {
        let offence = false;

        if (body instanceof Some) {
            let bodyStr = body.value;
            bodyStr = Helpers.removeAllCodeBlocks(bodyStr).trim();
            let seenBody = false;
            let seenFooter = false;
            const lines = Helpers.splitByEOLs(bodyStr, 1);
            for (const line of lines) {
                if (line.length === 0) {
                    continue;
                }
                seenBody = seenBody || !Helpers.isFooterNote(line);
                seenFooter = seenFooter || Helpers.isFooterNote(line);
                if (seenFooter && !Helpers.isFooterNote(line)) {
                    offence = true;
                    break;
                }
            }
        }

        return [
            !offence,
            `Footer messages must be placed after body paragraphs, please move any message that starts with "Fixes", "Closes" or "[i]" to the end of the commmit message.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static footerRefsValidity(rawStr: string) {
        let offence = false;
        let hasEmptyFooter = false;

        rawStr = rawStr.trim();
        const lineBreakIndex = rawStr.indexOf("\n");

        if (lineBreakIndex >= 0) {
            // Extracting bodyStr from rawStr rather than using body directly is a
            // workaround for https://github.com/conventional-changelog/conventional-changelog/issues/1016
            let bodyStr = rawStr.substring(lineBreakIndex).trim();
            bodyStr = Helpers.removeAllCodeBlocks(bodyStr);

            const lines = Helpers.splitByEOLs(bodyStr, 1);
            const bodyReferences = new Set();
            const references = new Set();
            for (const line of lines) {
                const matches = line.match(/(?<=\[)([0-9]+)(?=\])/g);
                if (matches === null) {
                    continue;
                }
                for (const match of matches) {
                    if (Helpers.isEmptyFooterReference(line)) {
                        offence = true;
                        hasEmptyFooter = true;
                    } else if (Helpers.isFooterReference(line)) {
                        references.add(match);
                    } else {
                        bodyReferences.add(match);
                    }
                }
            }
            for (const ref of bodyReferences) {
                if (!references.has(ref)) {
                    offence = true;
                    break;
                }
            }
            for (const ref of references) {
                if (!bodyReferences.has(ref)) {
                    offence = true;
                    break;
                }
            }
        }

        let errorMessage =
            "All references in the body must be mentioned in the footer, and vice versa.";

        if (hasEmptyFooter) {
            errorMessage =
                "A footer reference can not be empty, please make sure that you've provided the reference and there is no EOL between the reference number and the reference.";
        }

        return [!offence, errorMessage + Helpers.errMessageSuffix];
    }

    public static preferSlashOverBackslash(headerStr: string) {
        let offence = false;

        const colonIndex = headerStr.indexOf(":");
        if (colonIndex >= 0) {
            const scope = headerStr.substring(0, colonIndex);
            if (scope.includes("\\")) {
                offence = true;
            }
        }

        return [
            !offence,
            `Please use slash instead of backslash in the scope/sub-scope section of the title.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static properIssueRefs(rawStr: string) {
        let offence = false;

        rawStr = rawStr.trim();
        const lineBreakIndex = rawStr.indexOf("\n");

        if (lineBreakIndex >= 0) {
            // Extracting bodyStr from rawStr rather than using body directly is a
            // workaround for https://github.com/conventional-changelog/commitlint/issues/3412
            let bodyStr = rawStr.substring(lineBreakIndex).trim();

            bodyStr = Helpers.removeAllCodeBlocks(bodyStr);
            offence = Helpers.includesHashtagRef(bodyStr);
        }

        return [
            !offence,
            `Please use full URLs instead of #XYZ refs.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static titleUppercase(headerStr: string) {
        const firstWord = headerStr.split(" ")[0];
        const offence =
            headerStr.indexOf(":") < 0 &&
            !Helpers.wordIsStartOfSentence(firstWord) &&
            !Helpers.isProperNoun(firstWord);
        return [
            !offence,
            `Please start the title with an uppercase letter if you haven't specified any scope.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static tooManySpaces(rawStr: string) {
        rawStr = Helpers.removeAllCodeBlocks(rawStr);
        const offence = rawStr.match(`[^.]  `) !== null;

        return [
            !offence,
            `Please watch out for too many whitespaces in the text.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static typeSpaceAfterColon(headerStr: string) {
        const colonFirstIndex = headerStr.indexOf(":");

        let offence = false;
        if (colonFirstIndex > 0 && headerStr.length > colonFirstIndex) {
            if (headerStr[colonFirstIndex + 1] != " ") {
                offence = true;
            }
        }

        return [
            !offence,
            `Please place a space after the first colon character in your commit message title` +
                Helpers.errMessageSuffix,
        ];
    }

    public static typeWithSquareBrackets(headerStr: string) {
        const offence = headerStr.match(`^\\[.*\\]`) !== null;

        return [
            !offence,
            `Please use "scope/sub-scope: subject" or "scope(scope): subject" style instead of wrapping the scope details under square brackets in your commit message title` +
                Helpers.errMessageSuffix,
        ];
    }

    public static subjectLowercase(headerStr: string) {
        let offence = false;

        const colonFirstIndex = headerStr.indexOf(":");

        let firstWord = "";
        if (colonFirstIndex > 0 && headerStr.length > colonFirstIndex) {
            const subject = headerStr.substring(colonFirstIndex + 1).trim();
            if (subject != null && subject.length > 1) {
                firstWord = subject.trim().split(" ")[0];
                offence = Helpers.wordIsStartOfSentence(firstWord);
            }
        }

        return [
            !offence,
            `Please use lowercase as the first letter for your subject, i.e. the text after your scope (note: there is a chance that this rule is yielding a false positive in case the word '${firstWord}' is a name and must be capitalized; in which case please just reword the subject to make this word not be the first, sorry).` +
                Helpers.errMessageSuffix,
        ];
    }

    public static typeSpaceAfterComma(headerStr: string) {
        let offence = false;

        const colonIndex = headerStr.indexOf(":");

        if (colonIndex >= 0) {
            let scope = headerStr.substring(0, colonIndex);
            let commaIndex = scope.indexOf(",");
            while (commaIndex >= 0) {
                if (scope[commaIndex + 1] === " ") {
                    offence = true;
                }
                scope = scope.substring(commaIndex + 1);
                commaIndex = scope.indexOf(",");
            }
        }

        return [
            !offence,
            `No need to use space after comma in the scope (so that commit title can be shorter).` +
                Helpers.errMessageSuffix,
        ];
    }

    public static bodySoftMaxLineLength(
        body: Option<string>,
        bodyMaxLineLength: number
    ) {
        let offence = false;

        if (body instanceof Some) {
            let bodyStr = body.value;
            bodyStr = bodyStr.trim();
            bodyStr = Helpers.removeAllCodeBlocks(bodyStr).trim();

            if (bodyStr !== "") {
                const lines = Helpers.splitByEOLs(bodyStr, 1);
                let inCodeBlock = false;
                for (const line of lines) {
                    if (Helpers.isCodeBlockDelimiter(line)) {
                        inCodeBlock = !inCodeBlock;
                        continue;
                    }
                    if (inCodeBlock) {
                        continue;
                    }
                    if (line.length > bodyMaxLineLength) {
                        const isUrl = Helpers.isValidUrl(line);

                        const lineIsFooterNote = Helpers.isFooterNote(line);

                        const commitHashPattern = `([0-9a-f]{40})`;
                        /* eslint no-useless-escape: "off" -- escapes needed because this string is used as part of Regex */
                        const anySinglePunctuationCharOrNothing = `[\.\,\:\;\?\!]?`;
                        const index = line.search(
                            commitHashPattern +
                                anySinglePunctuationCharOrNothing +
                                `$`
                        );
                        const endsWithCommitHashButRestIsNotTooLong =
                            index != -1 && index < bodyMaxLineLength;

                        if (
                            !isUrl &&
                            !lineIsFooterNote &&
                            !endsWithCommitHashButRestIsNotTooLong
                        ) {
                            offence = true;
                            break;
                        }
                    }
                }
            }
        }

        return [
            !offence,
            `Please do not exceed ${bodyMaxLineLength} characters in the lines of the commit message's body; we recommend this script (for editing the last commit message): \n` +
                "https://github.com/nblockchain/conventions/blob/master/scripts/wrapLatestCommitMsg.fsx" +
                Helpers.errMessageSuffix,
        ];
    }

    public static bodyParagraphLineMinLength(
        body: Option<string>,
        paragraphLineMinLength: number,
        paragraphLineMaxLength: number
    ) {
        let offence: Option<string> = Nothing;

        if (body instanceof Some) {
            let bodyStr = body.value;
            bodyStr = Helpers.removeAllCodeBlocks(bodyStr).trim();

            const paragraphs = Helpers.splitByEOLs(bodyStr, 2);
            for (const paragraph of paragraphs) {
                const lines = Helpers.splitByEOLs(paragraph, 1);

                let bulletsAllowedNow = false;
                let alwaysBulletsSoFar = false;

                // NOTE: we don't iterate over the last line, on purpose
                for (let i = 0; i < lines.length - 1; i++) {
                    const line = lines[i];

                    if (line.length == 0) {
                        continue;
                    }

                    if (
                        !bulletsAllowedNow &&
                        ((i == 0 && Helpers.lineStartsWithBullet(line)) ||
                            line.endsWith(":"))
                    ) {
                        bulletsAllowedNow = true;
                        alwaysBulletsSoFar = true;
                    } else if (bulletsAllowedNow) {
                        alwaysBulletsSoFar =
                            alwaysBulletsSoFar &&
                            Helpers.lineStartsWithBullet(line);
                    }

                    if (line.length < paragraphLineMinLength) {
                        // this ref doesn't go out of bounds because we didn't iter on last line
                        const nextLine = lines[i + 1];

                        const isUrl =
                            Helpers.isValidUrl(line) ||
                            Helpers.isValidUrl(nextLine);

                        const lineIsFooterNote = Helpers.isFooterNote(line);

                        const nextWordLength =
                            lines[i + 1].split(" ")[0].length;
                        const isNextWordTooLong =
                            nextWordLength + line.length + 1 >
                            paragraphLineMaxLength;

                        const isLastCharAColonBreak =
                            line[line.length - 1] === ":" &&
                            nextLine[0].toUpperCase() == nextLine[0];

                        const isLastLineBeforeNextBullet =
                            bulletsAllowedNow &&
                            line[line.length - 1] === "." &&
                            Helpers.lineStartsWithBullet(nextLine);

                        if (
                            !alwaysBulletsSoFar &&
                            !isUrl &&
                            !lineIsFooterNote &&
                            !isNextWordTooLong &&
                            !isLastLineBeforeNextBullet &&
                            !isLastCharAColonBreak
                        ) {
                            offence = new Some(line);
                            break;
                        }
                    }
                }
            }
        }

        if (offence instanceof None) {
            return [
                true,
                "bodyParagraphLineMinLength's bug, this text should not be shown if offence was false",
            ];
        }
        return [
            false,
            `Please do not subceed ${paragraphLineMinLength} characters in the lines of the commit message's body paragraphs. Offending line has this text: "${offence.value}"; we recommend this script (for editing the last commit message): \n` +
                "https://github.com/nblockchain/conventions/blob/master/scripts/wrapLatestCommitMsg.fsx" +
                Helpers.errMessageSuffix,
        ];
    }

    public static rejectEmDash(rawStr: string) {
        const offence = rawStr.includes("—");

        return [
            !offence,
            `Please replace em-dashes (—) with a normal dash (-) if they are meant as bullet points or word-unions, or use parentheses if they are meant as a real em-dash.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static trailingWhitespace(rawStr: string) {
        let offence = false;

        const lines = Helpers.splitByEOLs(rawStr, 1);
        let inCodeBlock = false;
        for (const line of lines) {
            if (Helpers.isCodeBlockDelimiter(line)) {
                inCodeBlock = !inCodeBlock;
                continue;
            }
            if (inCodeBlock) {
                continue;
            }

            if (line[0] == " " || line[0] == "\t") {
                offence = true;
                break;
            }

            if (line.length > 0) {
                const lastChar = line[line.length - 1];
                if (lastChar == " " || lastChar == "\t") {
                    offence = true;
                    break;
                }
            }
        }

        return [
            !offence,
            `Please watch out for leading or ending trailing whitespace.` +
                Helpers.errMessageSuffix,
        ];
    }

    public static typeSpaceBeforeParen(headerStr: string) {
        let offence = false;

        const colonIndex = headerStr.indexOf(":");
        if (colonIndex >= 0) {
            const scope = headerStr.substring(0, colonIndex);
            const parenIndex = scope.indexOf("(");
            if (parenIndex >= 1) {
                if (headerStr[parenIndex - 1] === " ") {
                    offence = true;
                }
            }
        }

        return [
            !offence,
            `No need to use space before parentheses in the scope/sub-scope section of the title.` +
                Helpers.errMessageSuffix,
        ];
    }
}
