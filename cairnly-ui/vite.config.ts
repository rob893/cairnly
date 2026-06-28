import { defineConfig, loadEnv } from 'vite';
import type { Plugin, UserConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { buildContentSecurityPolicy } from './src/utils/csp';

// Injects a baseline Content-Security-Policy <meta> tag into the production build only.
// It is intentionally skipped in dev so Vite's HMR/inline bootstrap scripts keep working.
// The tag is prepended to <head> so it precedes (and therefore governs) the bundle's
// script/style tags. Note: <meta>-delivered CSP cannot enforce frame-ancestors; add that
// (and other security headers) at the hosting layer.
function cspMetaPlugin(apiBaseUrl: string | undefined): Plugin {
  return {
    name: 'inject-csp-meta',
    apply: 'build',
    transformIndexHtml: {
      order: 'pre',
      handler() {
        return [
          {
            tag: 'meta',
            attrs: {
              'http-equiv': 'Content-Security-Policy',
              content: buildContentSecurityPolicy(apiBaseUrl)
            },
            injectTo: 'head-prepend'
          }
        ];
      }
    }
  };
}

// https://vite.dev/config/
// Set VITE_BASE_PATH env var for GitHub Pages deployment (e.g. '/my-repo/')
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react(), tailwindcss(), cspMetaPlugin(env.VITE_API_BASE_URL)],
    base: env.VITE_BASE_PATH || '/',
    publicDir: 'public',
    // Dedicated port (not Vite's default 5173) so Cairnly gets its own origin and
    // doesn't share service workers / storage with other local apps. strictPort
    // fails fast instead of silently falling back to another port.
    server: { port: 5180, strictPort: true },
    preview: { port: 5180, strictPort: true },
    build: {
      rollupOptions: {
        output: {
          // Split stable vendor libraries into their own long-lived chunks so they
          // stay cacheable across deploys. Vite 8 uses Rolldown, where the object-map
          // form of `manualChunks` is unsupported; `codeSplitting.groups` is the
          // equivalent, with each group's `name` becoming the emitted chunk name.
          codeSplitting: {
            groups: [
              { name: 'vendor-react', test: /[\\/]node_modules[\\/](react|react-dom|react-router)[\\/]/ },
              { name: 'vendor-query', test: /[\\/]node_modules[\\/]@tanstack[\\/]react-query[\\/]/ },
              { name: 'vendor-ui', test: /[\\/]node_modules[\\/]@heroui[\\/](react|styles)[\\/]/ },
              { name: 'vendor-icons', test: /[\\/]node_modules[\\/]lucide-react[\\/]/ },
              {
                name: 'vendor-charts',
                test: /[\\/]node_modules[\\/](recharts|d3-[^\\/]+|victory-vendor|internmap)[\\/]/
              },
              { name: 'vendor-http', test: /[\\/]node_modules[\\/](axios|jwt-decode)[\\/]/ }
            ]
          }
        }
      }
    }
  } satisfies UserConfig;
});
