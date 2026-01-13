import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  plugins: [],
  resolve: {
    alias: {
      '@': '/src',
    },
  },
  server: { port: 5173 },
  build: {
    outDir: 'dist',
    rollupOptions: {
      // Build the SPA (index.html) and the service worker (src/sw.ts) as a separate entry
      input: {
        main: resolve(__dirname, 'index.html'),
        sw: resolve(__dirname, 'src/sw.ts')
      },
      output: {
        // Keep service worker at a stable name and root; other entries hashed in assets/
        entryFileNames: (chunk) => (chunk.name === 'sw' ? 'sw.js' : 'assets/[name]-[hash].js'),
        chunkFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]'
      }
    }
  }
});
