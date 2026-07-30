import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unused-vars': ['warn', { 'argsIgnorePattern': '^_' }],
      'react-hooks/set-state-in-effect': 'off',
      'react-hooks/purity': 'off',
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    files: ['src/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-globals': ['error', {
        name: 'fetch',
        message: 'Use fetchWithAccessControl / fetchResponseWithAccessControl from src/lib/apiClient.ts',
      }],
    },
  },
  {
    files: [
      'src/lib/apiClient.ts',
      'src/lib/createDataSource.ts',
      'src/lib/createRestDataSource.ts',
      'src/pages/system-config/HealthCheckPage.tsx',
    ],
    rules: {
      'no-restricted-globals': 'off',
    },
  },
  {
    files: ['src/pages/**/*.tsx'],
    rules: {
      'no-restricted-syntax': ['error', {
        selector: "JSXOpeningElement[name.name='button']",
        message: 'Use AppButton / IconButton / SegmentedToggle from src/components/ui',
      }],
    },
  },
])

