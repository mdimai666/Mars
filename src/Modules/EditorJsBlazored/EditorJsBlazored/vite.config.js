import { defineConfig } from 'vite'
//import copy from 'rollup-plugin-copy'

export default defineConfig({
    build: {
        lib: {
            entry: './JsSrc/index.js', // Входной файл
            name: 'EditorJsBlazored', // Имя библиотеки
            fileName: 'EditorJsBlazored', // Имя файла
            //formats: ['umd'], // Формат сборки
            formats: ['iife'],
        },
        rollupOptions: {
            output: {
                dir: 'wwwroot/dist', // Указываем папку для выходных файлов
                // Настройки для UMD
                globals: {
                    // Зависимости, которые будут доступны глобально
                },

            },
        },
        sourcemap: true,
        //minify: false,
    },
    plugins: [],
});
