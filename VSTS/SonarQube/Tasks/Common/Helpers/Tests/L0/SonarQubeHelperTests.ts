import * as assert from "assert";

describe("SonarQubeHelper", () => {
    describe("#exitOnPrBuild()", () => {
        it("should do nothing if not on PR build", () => {
            assert.fail();
        });

        it("should do nothing if on PR build but SQPullRequestBot is set to false", () => {
            assert.fail();
        });

        it("should stop process if on PR build and SQPullRequestBot is set to true", () => {
            assert.fail();
        });

        it("should stop process if on PR build and SQPullRequestBot not defined", () => {
            assert.fail();
        });
    });
});