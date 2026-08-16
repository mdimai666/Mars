// e2e-проверка страницы «Браузерные скрипты» стенда PxBlocks: загрузить пример
// (поиск в Википедии), нажать Run и дождаться вывода первых трёх результатов.
// Сценарий исполняется НА СЕРВЕРЕ в видимом системном Edge (контекст «browser»).
// Запуск: node e2e/check-browser.mjs [url]   (по умолчанию http://localhost:5215/browser)
import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const url = process.argv[2] ?? 'http://localhost:5215/browser';
const outDir = fileURLToPath(new URL('./out/', import.meta.url));
mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ channel: 'msedge', headless: true });
const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
page.on('pageerror', (e) => console.log('PAGE ERROR:', e.message));
page.on('console', (m) => {
    if (m.type() === 'error') console.log('CONSOLE ERROR:', m.text());
});
page.on('response', (r) => {
    if (r.status() >= 400) console.log('HTTP', r.status(), r.url());
});

await page.goto(url, { waitUntil: 'load' });
// WASM-приложение стартует после загрузки фреймворка — даём время.
await page.waitForSelector('.pxb-rail', { timeout: 90000 });
await page.waitForSelector('.blocklySvg', { timeout: 30000 });
await page.waitForTimeout(1000);

await page.click('button:has-text("Пример: поиск в Википедии")');
await page.waitForTimeout(800);
await page.screenshot({ path: outDir + 'browser-sample.png' });

await page.click('button:has-text("Run")');
console.log('RUN: сценарий запущен на сервере (серверный Edge должен открыться)');

// Итог: либо третья строка результата, либо ошибка исполнения.
await Promise.race([
    page.waitForSelector('.px-blocks-output div:not(.px-blocks-output-error):has-text("[3]")', { timeout: 150000 }),
    page.waitForSelector('.px-blocks-output-error', { timeout: 150000 }),
]);
await page.waitForTimeout(400);

const lines = await page.$$eval('.px-blocks-output div', els => els.map(e => e.textContent?.trim()));
console.log('OUTPUT:');
for (const line of lines) console.log('  ' + line);
await page.screenshot({ path: outDir + 'browser-done.png' });

await browser.close();
console.log('OK, screenshots in e2e/out/');
