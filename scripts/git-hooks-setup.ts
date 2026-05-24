import { execSync } from "node:child_process";

if (!process.env.CI) {
    execSync("git config core.hooksPath .husky", { stdio: "inherit" });
}
