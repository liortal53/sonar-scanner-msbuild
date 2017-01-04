
import tl = require("vsts-task-lib/task");

import { ToolRunner } from "vsts-task-lib/toolrunner";
import { SonarQubeEndpoint } from "vsts-sonarqube-common/SonarQubeEndpoint";
import { VstsServerUtils } from "vsts-sonarqube-common/VstsServerUtils";

export class ParametersHelper {

    /**
     * Applies parameters for SonarQube features enabled by the user.
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static applyParameters(toolRunner: ToolRunner): ToolRunner {
        toolRunner = ParametersHelper.addConnectionParams(toolRunner);
        toolRunner = ParametersHelper.addProjectParams(toolRunner);
        toolRunner = ParametersHelper.addSourcesParams(toolRunner);
        toolRunner = ParametersHelper.addSettingsParams(toolRunner);
        toolRunner = ParametersHelper.addIssuesModeInPrBuild(toolRunner);
        return toolRunner;
    }

    /**
     * Applies required parameters for connecting a Java-based plugin to SonarQube.
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static addConnectionParams(toolRunner: ToolRunner): ToolRunner {
        let sqEndpoint: SonarQubeEndpoint = SonarQubeEndpoint.getTaskSonarQubeEndpoint();
        toolRunner.arg("-Dsonar.host.url=" + sqEndpoint.Url);
        toolRunner.arg("-Dsonar.login=" + sqEndpoint.Token);
        return toolRunner;
    }

    /**
     * Applies parameters for manually specifying the project name, key and version to SonarQube.
     * This will override any settings that may have been specified manually by the user.
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static addProjectParams(toolRunner: ToolRunner): ToolRunner {
        toolRunner.arg("-Dsonar.projectKey=" + tl.getInput("projectKey", true));
        toolRunner.arg("-Dsonar.projectName=" + tl.getInput("projectName", true));
        toolRunner.arg("-Dsonar.projectVersion=" + tl.getInput("projectVersion", true));
        return toolRunner;
    }

    /**
     * Applies parameters for manually specifying the sources path and the project base directory.
     * This will override any settings that may have been specified manually by the user.
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static addSourcesParams(toolRunner: ToolRunner): ToolRunner {
        toolRunner.arg("-Dsonar.sources=" + tl.getPathInput("sources", true, true));
        // could also use tl.getVariable("Build.SourcesDirectory") (not sure which one is the best)
        toolRunner.arg("-Dsonar.projectBaseDir=" + tl.getVariable("System.DefaultWorkingDirectory"));
        return toolRunner;
    }

    /**
     * Applies parameters for manually specifying the settings or the settings file.
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static addSettingsParams(toolRunner: ToolRunner): ToolRunner {
        if (tl.filePathSupplied("configFile")) {
            toolRunner.arg("-Dproject.settings=" + tl.getPathInput("configFile", true, true));
        }

        return toolRunner;
    }

    /**
     * Applies parameters that will run SQ analysis in issues mode if this is a pull request build
     * @param toolRunner     ToolRunner to add parameters to
     * @returns {ToolRunner} ToolRunner with parameters added
     */
    public static addIssuesModeInPrBuild(toolrunner: ToolRunner): ToolRunner {
        if (VstsServerUtils.isPrBuild()) {
            console.log("Detected a PR build - running the SonarQube analysis in issues mode");

            toolrunner.arg("-Dsonar.analysis.mode=issues");
            toolrunner.arg("-Dsonar.report.export.path=sonar-report.json");
        } else {
            tl.debug("Running a full SonarQube analysis");
        }

        return toolrunner;
    }
}