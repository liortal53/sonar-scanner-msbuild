import { Utils } from "../../Utils";

import * as chai from "chai";

describe("Utils", () => {

    before(() => {
        chai.should();
    });

    describe("#isNullOrEmpty()", () => {
        it("should return true when the value is undefined", () => {
            Utils.isNullOrEmpty(undefined).should.equal(true, "should be true");
        });

        it("should return true when the value is null", () => {
            Utils.isNullOrEmpty(null).should.equal(true, "should be true");
        });

        it("should return true when the value is empty string", () => {
            Utils.isNullOrEmpty("").should.equal(true, "should be true");
        });

        it("should return false when the value is not an empty string", () => {
            Utils.isNullOrEmpty("foo").should.equal(false, "should be false");
        });
    });
});