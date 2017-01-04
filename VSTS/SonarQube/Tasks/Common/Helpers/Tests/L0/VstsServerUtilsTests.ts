import tl = require("vsts-task-lib/task");
import { VstsServerUtils } from "../../VstsServerUtils";

import * as chai from "chai";

describe("VstsServerUtils", () => {

    before(() => {
        chai.should();
    });

    describe("#isPrBuild()", () => {
        it("should return false when the scc provider is null", () => {
            // arrange
            tl.setVariable("build.repository.provider", null);

            // act
            var result: boolean = VstsServerUtils.isPrBuild();

            // assert
            result.should.equal(false, "should be false");
        });

        it("should return false when the scc provider is not tfsgit", () => {
            // arrange
            tl.setVariable("build.repository.provider", "something");

            // act
            var result: boolean = VstsServerUtils.isPrBuild();

            // assert
            result.should.equal(false, "should be false");
        });

        it("should return false when the scc provider is tfsgit but source branch is null", () => {
            // arrange
            tl.setVariable("build.repository.provider", "tfsgit");
            tl.setVariable("build.sourceBranch", null);

            // act
            var result: boolean = VstsServerUtils.isPrBuild();

            // assert
            result.should.equal(false, "should be false");
        });

        it("should return false when the scc provider is tfsgit but source branch doesn't start with refs/pull/", () => {
            // arrange
            tl.setVariable("build.repository.provider", "tfsgit");
            tl.setVariable("build.sourceBranch", "something");

            // act
            var result: boolean = VstsServerUtils.isPrBuild();

            // assert
            result.should.equal(false, "should be false");
        });

        it("should return true when the scc provider is tfsgit and the source branch starts with refs/pull/", () => {
            // arrange
            tl.setVariable("build.repository.provider", "tfsgit");
            tl.setVariable("build.sourceBranch", "refs/pull/");

            // act
            var result: boolean = VstsServerUtils.isPrBuild();

            // assert
            result.should.equal(true, "should be true");
        });
    });

    describe("#isFeatureEnabled()", () => {
        it("should return true when the variable exists and is true", () => {
            // arrange
            tl.setVariable("foo", "true");

            // act
            var result: boolean = VstsServerUtils.isFeatureEnabled("foo", false);

            // assert
            result.should.equal(true, "should be true");
        });

        it("should return false when the variable exists and is false", () => {
            // arrange
            tl.setVariable("foo", "false");

            // act
            var result: boolean = VstsServerUtils.isFeatureEnabled("foo", true);

            // assert
            result.should.equal(false, "should be false");
        });

        it("should return true when the variable doesn't exist and default value is true", () => {
            // arrange & Act
            var result: boolean = VstsServerUtils.isFeatureEnabled("bar", true);

            // assert
            result.should.equal(true, "should be true");
        });

        it("should return true when the variable doesn't exist and default value is false", () => {
            // arrange & Act
            var result: boolean = VstsServerUtils.isFeatureEnabled("bar", false);

            // assert
            result.should.equal(false, "should be false");
        });
    });
});