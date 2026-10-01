import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const apiUrl = loadEnv(mode, process.cwd(), '').VITE_API_URL || 'http://localhost:5112'
  return {
    plugins: [react(), tailwindcss()],
    server: {
      // The blog and sitemap are HTML/XML pages served by the API. In production the host passes these paths
      // through (see render.yaml); in development Vite does the same.
      proxy: {
        '^/blog(/.*)?$': { target: apiUrl, changeOrigin: true },
        '/sitemap.xml': { target: apiUrl, changeOrigin: true },
      },
    },
  }
})
