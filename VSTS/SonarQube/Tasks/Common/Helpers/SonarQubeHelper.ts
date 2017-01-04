import tl = require("vsts-task-lib/task");

import { VstsServerUtils } from "./VstsServerUtils";

export class SonarQubeHelper {
    public static exitOnPrBuild(): void {
        tl.debug("Checking if this is a PR build.");

        if (VstsServerUtils.isPrBuild() && !(VstsServerUtils.isFeatureEnabled("SQPullRequestBot", true))) {
            console.log("Skipping SonarQube analysis because this build was triggered by a pull request.");
            process.exit();
        }
    }
}