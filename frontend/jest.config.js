export default {
    moduleFileExtensions: ["js"],
    roots: ["<rootDir>/dist/tests"],
    testMatch: ["**/*.Test.js"],
    testEnvironment: "node",
    coveragePathIgnorePatterns: ["/node_modules/"],
    transform: {},
    moduleNameMapper: {
        "^@wailsjs/go/main/App$": "<rootDir>/__mocks__/@wailsjs/go/main/App.js",
    },
};
