/**
 * The Scanner CLI task logic
 */

import path = require("path");
import tl = require("vsts-task-lib/task");
import { ToolRunner } from "vsts-task-lib/toolrunner";
import { SonarQubeHelper } from "vsts-sonarqube-common/SonarQubeHelper";
import { ParametersHelper } from "./parameters-helper";

async function run(): Promise<void> {
    try {
        SonarQubeHelper.exitOnPrBuild();

        let sonarScannerToolPath: string = path.join(__dirname, "sonar-scanner/bin/sonar-scanner.bat");
        tl.checkPath(sonarScannerToolPath, "SonarQube Scanner CLI");
        let sonarScannerTool: ToolRunner = tl.tool(sonarScannerToolPath);
        ParametersHelper.applyParameters(sonarScannerTool);
        await sonarScannerTool.exec();

        tl.setResult(tl.TaskResult.Succeeded, "SonarQube Analysis succeeded.");
    } catch (error) {
        tl.error("Task failed with the following error: " + error);
        tl.setResult(tl.TaskResult.Failed, "SonarQube Analysis failed.");
    }
}

run();