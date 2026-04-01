import path from 'path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target:      'https://localhost:7195',
        changeOrigin: true,
        secure:       false,
      },
      '/hubs': {
        target:      'https://localhost:7195',
        changeOrigin: true,
        secure:       false,
        ws:           true,
      },
    },
  },
  appType: 'spa',
})