import path from 'node:path';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// Aspire injects the Search API's address through service discovery (WithReference in AppHost.cs).
// The dev server proxies /api to it, so the browser only ever talks to the Vite origin: no CORS.
// See docs/adr/0014-web-ui-architecture.md#stack-and-hosting.
const searchApiUrl = process.env.services__searchapi__https__0 ?? process.env.services__searchapi__http__0;

export default defineConfig({
    plugins: [react(), tailwindcss()],
    server: {
        proxy: searchApiUrl
            ? {
                  '/api': {
                      target: searchApiUrl,
                      // The API uses the local ASP.NET Core development certificate, which Node doesn't trust.
                      secure: false,
                  },
              }
            : undefined,
    },
    resolve: {
        alias: {
            '@': path.resolve(import.meta.dirname, './src'),
        },
    },
    test: {
        include: ['src/**/*.test.{ts,tsx}'],
    },
});
