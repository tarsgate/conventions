import { None, Option, Some, Nothing, Empty } from "fp-sdk";

export abstract class Helpers {
    public static errMessageSuffix =
        "\nFor reference, these are the guidelines that include our commit message conventions: https://github.com/nblockchain/conventions/blob/master/docs/WorkflowGuidelines.md";

    public static isValidUrl(text: string) {
        if (text.indexOf(" ") >= 0) {
            return false;
        }

        // Borrowed from https://www.freecodecamp.org/news/check-if-a-javascript-string-is-a-url/
        try {
            return Boolean(new URL(text));
        } catch {
            return false;
        }
    }

    public static assertNotNone(
        text: Option<string>,
        errorMessage: string
    ): string {
        if (text instanceof None) {
            throw new Error(errorMessage);
        }
        return text.value;
    }

    public static assertNotNull(
        text: string | null,
        errorMessage: string
    ): string {
        if (text === null) {
            throw new Error(errorMessage);
        }
        return text as string;
    }

    public static assertCharacter(letter: string) {
        if (letter.length !== 1) {
            throw Error("This function expects a character as input");
        }
    }

    public static assertLine(line: string) {
        if (line.includes("\n")) {
            throw Error("This function expects a line as input");
        }
    }

    public static assertWord(word: string) {
        if (word.includes("\n") || word.includes(" ")) {
            throw Error(
                "This function expects a word as input.\n" +
                    "A word doesn't include line breaks and whitespaces."
            );
        }
    }

    public static assertHigherThanZero(number: number) {
        if (number < 1) {
            throw Error("This param cannot be negative or zero: " + number);
        }
    }

    public static assertUrl(url: string) {
        if (!Helpers.isValidUrl(url)) {
            throw Error("This function expects a url as input");
        }
    }

    public static assertChar(str: string) {
        if (str.length != 1) {
            throw Error("This function expects a char as input");
        }
    }

    public static findUrls(text: string) {
        const urlRegex = /(https?:\/\/[^\s]+)/g;
        return Option.ofObj(text.match(urlRegex));
    }

    public static isCommitUrl(url: string) {
        Helpers.assertUrl(url);
        return url.includes("/commit/");
    }

    public static isCharADigit(aChar: string) {
        Helpers.assertChar(aChar);
        // https://stackoverflow.com/a/66666216/544947
        return !isNaN(parseInt(aChar));
    }

    public static lineStartsWithBullet(line: string) {
        Helpers.assertLine(line);

        const allowedUnnumberedBulletChars = ["*", "-"];
        for (const bulletChar of allowedUnnumberedBulletChars) {
            const unnumberedPattern = bulletChar + " ";
            if (
                line.startsWith(unnumberedPattern) &&
                line.length > unnumberedPattern.length
            )
                return true;
        }

        const allowedNumberedBulletChars = ["."];
        for (const bulletChar of allowedNumberedBulletChars) {
            if (line.indexOf(bulletChar) > 0) {
                for (let i = 0; i < line.length; i++) {
                    if (Helpers.isCharADigit(line[i])) {
                        continue;
                    } else {
                        if (line[i] == bulletChar) {
                            return true;
                        }
                        break;
                    }
                }
            }
        }

        return false;
    }

    // NOTE: a code block delimiter is expected to be exactly the 3-backticks
    // sequence, but we tolerate trailing whitespace in it (e.g. "``` ") so that
    // the code block boundaries are still detected correctly; such trailing
    // whitespace is a problem that the trailing-whitespace plugin reports
    public static isCodeBlockDelimiter(line: string) {
        Helpers.assertLine(line);
        const codeBlockDelimiter = "```";
        return line.trimEnd() === codeBlockDelimiter;
    }

    public static isUpperCase(letter: string) {
        Helpers.assertCharacter(letter);
        const isUpperCase = letter.toUpperCase() == letter;
        const isLowerCase = letter.toLowerCase() == letter;

        return isUpperCase && !isLowerCase;
    }

    public static isLowerCase(letter: string) {
        Helpers.assertCharacter(letter);
        const isUpperCase = letter.toUpperCase() == letter;
        const isLowerCase = letter.toLowerCase() == letter;

        return isLowerCase && !isUpperCase;
    }

    public static isEmptyFooterReference(line: string) {
        Helpers.assertLine(line);
        const trimmedLine = line.trim();
        return (
            trimmedLine[0] === "[" &&
            trimmedLine.indexOf("]") === trimmedLine.length - 1
        );
    }

    public static isFooterReference(line: string) {
        Helpers.assertLine(line);
        return line[0] === "[" && line.indexOf("]") > 1;
    }

    public static isFixesOrClosesSentence(line: string) {
        Helpers.assertLine(line);
        return line.indexOf("Fixes ") == 0 || line.indexOf("Closes ") == 0;
    }

    public static isCoAuthoredByTag(line: string) {
        Helpers.assertLine(line);
        return line.indexOf("Co-authored-by: ") == 0;
    }

    public static isFooterNote(line: string): boolean {
        Helpers.assertLine(line);
        return (
            Helpers.isFooterReference(line) ||
            Helpers.isCoAuthoredByTag(line) ||
            Helpers.isFixesOrClosesSentence(line)
        );
    }

    public static numUpperCaseLetters(word: string) {
        Helpers.assertWord(word);
        return word.length - word.replace(/[A-Z]/g, "").length;
    }

    public static numNonAlphabeticalCharacters(word: string) {
        Helpers.assertWord(word);
        return word.length - word.replace(/[^a-zA-Z]/g, "").length;
    }

    public static isProperNoun(word: string) {
        Helpers.assertWord(word);
        const numUpperCase = Helpers.numUpperCaseLetters(word);
        const numNonAlphabeticalChars =
            Helpers.numNonAlphabeticalCharacters(word);

        return (
            numNonAlphabeticalChars > 0 ||
            (Helpers.isUpperCase(word[0]) && numUpperCase > 1) ||
            (Helpers.isLowerCase(word[0]) && numUpperCase > 0)
        );
    }

    public static wordIsStartOfSentence(word: string) {
        Helpers.assertWord(word);
        if (Helpers.isUpperCase(word[0])) {
            const numUpperCase = Helpers.numUpperCaseLetters(word);
            const numNonAlphabeticalChars =
                Helpers.numNonAlphabeticalCharacters(word);
            return numUpperCase == 1 && numNonAlphabeticalChars == 0;
        }
        return false;
    }

    public static includesHashtagRef(text: string) {
        return (
            text.match(`\\s+#[0-9]+`) !== null ||
            text.match(`^#[0-9]+`) !== null
        );
    }

    public static splitByEOLs(text: string, numberOfEols: number) {
        Helpers.assertHigherThanZero(numberOfEols);

        const unixEol = "\n";
        const windowsEol = "\r\n";
        const macEol = "\r";

        const preparedText = text
            .replaceAll(windowsEol, unixEol)
            .replaceAll(macEol, unixEol);

        let separator = "";
        do {
            separator += unixEol;
            numberOfEols--;
        } while (numberOfEols != 0);

        return preparedText.split(separator);
    }

    // NOTE: instead of removing code blocks from the text (which would shift the
    // line indexes and thus make line numbers reported to the user wrong), we
    // return one entry per line of the original text, with the lines that belong
    // to code blocks (including the delimiters) blanked out. Blank lines are then
    // ignored by the rules that use this, and behave like paragraph separators
    // for the ones that split the text into paragraphs.
    public static linesWithCodeBlocksBlankedOut(text: string): Array<string> {
        const lines = Helpers.splitByEOLs(text, 1);
        const result: Array<string> = Empty.array<string>();
        const blankLine = Empty.string();
        let inCodeBlock = false;
        for (const line of lines) {
            if (Helpers.isCodeBlockDelimiter(line)) {
                inCodeBlock = !inCodeBlock;
                result.push(blankLine);
                continue;
            }
            result.push(inCodeBlock ? blankLine : line);
        }
        return result;
    }

    public static splitIntoParagraphs(lines: Array<string>): Array<string> {
        const paragraphs: Array<string> = Empty.array<string>();
        let currentParagraphLines: Array<string> = Empty.array<string>();
        for (const line of lines) {
            if (line.length === 0) {
                if (currentParagraphLines.length > 0) {
                    paragraphs.push(currentParagraphLines.join("\n"));
                    currentParagraphLines = Empty.array<string>();
                }
                continue;
            }
            currentParagraphLines.push(line);
        }
        if (currentParagraphLines.length > 0) {
            paragraphs.push(currentParagraphLines.join("\n"));
        }
        return paragraphs;
    }

    // this function is needed as a workaround for 2 things:
    // 1) conventional-changelog-conventionalcommits ( https://github.com/conventional-changelog/conventional-changelog/tree/master/packages/conventional-changelog-conventionalcommits )
    // treating any `Foo: bar` string (even inside code blocks) as trailers, see https://git-scm.com/docs/git-interpret-trailers
    // 2) https://github.com/conventional-changelog/commitlint/issues/3412
    public static extractBodyFromCommitMsg(msg: string): Option<string> {
        const linesBeforeBody = 2;
        const lines = Helpers.splitByEOLs(msg, 1);
        if (lines.length < linesBeforeBody) {
            return Nothing;
        }
        const restOfLines = lines.slice(linesBeforeBody);
        if (restOfLines.length == 1 && restOfLines[0] === Empty.string()) {
            return Nothing;
        }
        return new Some(restOfLines.join("\n"));
    }
}
