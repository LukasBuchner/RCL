import js from "@eslint/js";
import globals from "globals";
import tseslint from "typescript-eslint";
import pluginReact from "eslint-plugin-react";
import pluginReactHooks from "eslint-plugin-react-hooks";
import pluginReactRefresh from "eslint-plugin-react-refresh";

export default [
  {
    ignores: ["dist/**", "node_modules/**", "*.generated.*", "**/__generated__/**", "*.config.js", "*.config.cjs"],
  },
  {
    files: ["**/*.{js,mjs,cjs,ts,mts,cts,jsx,tsx}"],
    plugins: {
      react: pluginReact,
      "react-hooks": pluginReactHooks,
      "react-refresh": pluginReactRefresh,
    },
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        ecmaFeatures: {
          jsx: true,
        },
      },
    },
    settings: {
      react: {
        version: "detect",
      },
    },
    rules: {
      ...js.configs.recommended.rules,
      ...pluginReact.configs.recommended.rules,
      ...pluginReactHooks.configs.recommended.rules,
      "react/react-in-jsx-scope": "off", // Not needed in React 17+
      "react/jsx-uses-react": "off", // Not needed in React 17+
      "react/prop-types": "off", // Not needed with TypeScript
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          ignoreRestSiblings: true,
        },
      ],
      "react-refresh/only-export-components": [
        "warn",
        { allowConstantExport: true },
      ],
      "no-console": ["error"],
    },
  },
  ...tseslint.configs.recommended,
  {
    // The logger is the single module permitted to call `console`.
    files: ["src/utils/logger.ts"],
    rules: { "no-console": "off" },
  },
  {
    // Node-side tooling and tests run outside the browser logger.
    files: [
      "**/*.test.{ts,tsx}",
      "**/__tests__/**",
      "src/test/**",
      "**/codegen.ts",
      "**/generatePossibleTypes.ts",
      "*.config.{ts,mts,cts}",
    ],
    rules: { "no-console": "off" },
  },
];
