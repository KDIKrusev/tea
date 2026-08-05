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
        project: ["tsconfig.json", "tsconfig.app.json", "tsconfig.spec.json"],
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

      // Loose equality is banned everywhere EXCEPT against null.
      //
      // Reason (story C-C, verified site by site): all twenty `==`/`!=` occurrences in this codebase
      // were `x != null` / `x == null` — the one case where loose equality is the precise tool,
      // because it means "neither null nor undefined" in a single comparison. There was not one
      // instance of loose equality between two ordinary values.
      //
      // Rewriting them as `x !== null && x !== undefined` would double their length, and getting a
      // single one wrong (`x != null` → `x !== null`) silently starts accepting `undefined` — a
      // behaviour change smuggled in under a lint fix. `{ null: "ignore" }` keeps the rule strict
      // where it protects something and quiet where it does not.
      "eqeqeq": ["error", "always", { "null": "ignore" }],
      "no-console": ["warn", { "allow": ["warn", "error"] }],
      
      // Angular best practices for maritime applications
      "@angular-eslint/no-lifecycle-call": "error",
      // All 21 components are OnPush as of story C-G. Escalated from "warn" to "error" so the
      // exception cannot creep back: a component that needs default change detection now has to
      // say so explicitly, in a review someone reads.
      "@angular-eslint/prefer-on-push-component-change-detection": "error",
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
      
      // Same reasoning as the TypeScript `eqeqeq` above: `x != null` in a template is a nullish
      // check, and both template occurrences are exactly that.
      "@angular-eslint/template/eqeqeq": ["error", { "allowNullOrUndefined": true }],

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
