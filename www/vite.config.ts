import { defineConfig } from 'vite'
import { resolve, parse } from 'path'
import { readdirSync } from 'fs'

// Automatically find all HTML files in the root directory
const htmlInputs = {};
readdirSync(__dirname).forEach(file => {
  if (file.endsWith('.html')) {
    const name = parse(file).name;
    htmlInputs[name] = resolve(__dirname, file);
  }
});

export default defineConfig({
  root: './',
  base: './',
  build: {
    outDir: 'dist',
    rollupOptions: {
      input: htmlInputs,
    },
  },
  server: {
    port: 3000
  }
})
