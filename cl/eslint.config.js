// @ts-check
const eslint = require("@eslint/js");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");

module.exports = tseslint.config(
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    languageOptions: {
      parserOptions: {
        project: ["tsconfig.json", "tsconfig.app.json"],
        createDefaultProgram: true,
      },
    },
    rules: {
      // Angular component and directive naming
      "@angular-eslint/directive-selector": [
        "error",
        {
          type: "attribute",
          prefix: "app",
          style: "camelCase",
        },
      ],
      "@angular-eslint/component-selector": [
        "error",
        {
          type: "element",
          prefix: "app",
          style: "kebab-case",
        },
      ],
      
      // TypeScript strict rules for maritime calculation accuracy
      "@typescript-eslint/no-unused-vars": ["error", { 
        "argsIgnorePattern": "^_",
        "varsIgnorePattern": "^_" 
      }],
      "@typescript-eslint/explicit-function-return-type": "warn",
      "@typescript-eslint/no-explicit-any": "error",
      
      // Code quality for maritime industry standards
      "prefer-const": "error",
      "no-var": "error",
      "curly": "error",
      "eqeqeq": "error",
      "no-console": ["warn", { "allow": ["warn", "error"] }],
      
      // Angular best practices for maritime applications
      "@angular-eslint/no-lifecycle-call": "error",
      "@angular-eslint/prefer-on-push-component-change-detection": "warn",
      "@angular-eslint/use-injectable-provided-in": "error",
      "@angular-eslint/no-input-rename": "error",
      "@angular-eslint/no-output-rename": "error",
      
      // RxJS rules for maritime calculation streams
      "@typescript-eslint/prefer-for-of": "error",
      "@typescript-eslint/array-type": ["error", { "default": "array-simple" }],
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {
      // Accessibility rules for maritime professional UI
      "@angular-eslint/template/alt-text": "error",
      "@angular-eslint/template/elements-content": "error",
      "@angular-eslint/template/label-has-associated-control": "error",
      "@angular-eslint/template/valid-aria": "error",
      "@angular-eslint/template/click-events-have-key-events": "error",
      "@angular-eslint/template/mouse-events-have-key-events": "error",
      
      // Angular template best practices
      "@angular-eslint/template/no-negated-async": "error",
      "@angular-eslint/template/use-track-by-function": "warn",
      "@angular-eslint/template/conditional-complexity": ["error", { "maxComplexity": 3 }],
    },
  },
  {
    // Special rules for test files
    files: ["**/*.spec.ts"],
    rules: {
      "@typescript-eslint/no-explicit-any": "off",
      "@typescript-eslint/explicit-function-return-type": "off",
      "no-console": "off",
    },
  }
);
