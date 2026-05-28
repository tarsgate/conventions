import { spawnSync } from "child_process";
import os from "os";

export function runCommitLintOnMsg(inputMsg: string) {
    // FIXME: should we .lowerCase().startsWith("win") in case it starts
    // returning Win64 in the future? thing is, our CI doesn't like this
    // change (strangely enough it causes commitlint test suite to fail)
    const command = os.platform() === "win32" ? "npx.cmd" : "npx";
    return spawnSync(command, ["commitlint", "--verbose"], { input: inputMsg });
}
