import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
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

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    base: normalizeBasePath(getEnv(env, 'VITE_ILEARN_ADMIN_APP_BASE_PATH', 'VITE_APP_BASE_PATH')),
    plugins: [react(), tailwindcss()],
    build: {
      sourcemap: true,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes('node_modules/devextreme') || id.includes('node_modules/devextreme-react')) {
              return 'devextreme'
            }

            if (id.includes('node_modules/@microsoft/signalr')) {
              return 'signalr'
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
