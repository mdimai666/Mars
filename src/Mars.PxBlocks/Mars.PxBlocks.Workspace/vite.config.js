import { defineConfig } from 'vite'

export default defineConfig({
    build: {
        lib: {
            entry: './JsSrc/index.ts',
            name: 'MarsPxBlocks',
            fileName: () => 'PxBlocks.js',
            formats: ['es'],
        },
        rollupOptions: {
            output: {
                dir: 'wwwroot/dist',
            },
        },
        sourcemap: true,
        target: 'es2020',
    },
    plugins: [],
});
