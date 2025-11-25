import { defineConfig } from 'vite'

export default defineConfig({
  server: {
    port: 3001,
    // Use HTTP for development since the backend WebSocket server doesn't have SSL
    https: false
  }
})
