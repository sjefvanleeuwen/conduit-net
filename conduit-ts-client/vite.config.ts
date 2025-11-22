import { defineConfig } from 'vite';
import { resolve } from 'path';
import dts from 'vite-plugin-dts';

export default defineConfig({
  plugins: [dts({ include: ['src'] })],
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      name: 'ConduitClient',
      fileName: 'conduit-client',
    },
    rollupOptions: {
      external: ['@msgpack/msgpack'],
      output: {
        globals: {
          '@msgpack/msgpack': 'MessagePack',
        },
      },
    },
  },
});
