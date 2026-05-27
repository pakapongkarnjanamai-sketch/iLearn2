import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'

const getEnv = (env: Record<string, string>, ...keys: string[]) => {
  for (const key of keys) {
    const value = env[key]
    if (value && value.trim()) {
      return value.trim()
    }
  }

  return undefined
}

const normalizeBasePath = (value: string | undefined) => {
  if (!value || value === '/') {
    return '/'
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`
  return withLeadingSlash.endsWith('/') ? withLeadingSlash : `${withLeadingSlash}/`
}

const projectPath = (path: string) => fileURLToPath(new URL(path, import.meta.url))

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    base: normalizeBasePath(getEnv(env, 'VITE_ILEARN_ADMIN_APP_BASE_PATH', 'VITE_APP_BASE_PATH')),
    resolve: {
      alias: {
        'es-toolkit/compat/get': projectPath('./src/lib/es-toolkit-compat/get.ts'),
        'es-toolkit/compat/isPlainObject': projectPath('./src/lib/es-toolkit-compat/isPlainObject.ts'),
        'es-toolkit/compat/last': projectPath('./src/lib/es-toolkit-compat/last.ts'),
        'es-toolkit/compat/maxBy': projectPath('./src/lib/es-toolkit-compat/maxBy.ts'),
        'es-toolkit/compat/minBy': projectPath('./src/lib/es-toolkit-compat/minBy.ts'),
        'es-toolkit/compat/omit': projectPath('./src/lib/es-toolkit-compat/omit.ts'),
        'es-toolkit/compat/range': projectPath('./src/lib/es-toolkit-compat/range.ts'),
        'es-toolkit/compat/sortBy': projectPath('./src/lib/es-toolkit-compat/sortBy.ts'),
        'es-toolkit/compat/sumBy': projectPath('./src/lib/es-toolkit-compat/sumBy.ts'),
        'es-toolkit/compat/throttle': projectPath('./src/lib/es-toolkit-compat/throttle.ts'),
        'es-toolkit/compat/uniqBy': projectPath('./src/lib/es-toolkit-compat/uniqBy.ts'),
        'use-sync-external-store/with-selector': projectPath('./src/lib/useSyncExternalStoreWithSelectorShim.ts'),
        'use-sync-external-store/with-selector.js': projectPath('./src/lib/useSyncExternalStoreWithSelectorShim.ts'),
        'use-sync-external-store/shim/with-selector': projectPath('./src/lib/useSyncExternalStoreWithSelectorShim.ts'),
        'use-sync-external-store/shim/with-selector.js': projectPath('./src/lib/useSyncExternalStoreWithSelectorShim.ts'),
      },
    },
    plugins: [react(), tailwindcss()],
    optimizeDeps: {
      include: [
        'use-sync-external-store/with-selector',
        'use-sync-external-store/with-selector.js',
        'use-sync-external-store/shim/with-selector',
        'use-sync-external-store/shim/with-selector.js',
      ],
    },
    build: {
      sourcemap: true,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes('node_modules/@microsoft/signalr')) {
              return 'signalr'
            }

            if (id.includes('node_modules/recharts') || id.includes('node_modules/d3-')) {
              return 'charts'
            }

            if (id.includes('node_modules/react') || id.includes('node_modules/react-router-dom')) {
              return 'react'
            }

            return undefined
          },
        },
      },
    },
  }
})
