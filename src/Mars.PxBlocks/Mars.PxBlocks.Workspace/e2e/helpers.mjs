// Общий пролог e2e-проверок стенда PxBlocks: системный Edge (headless), папка
// скриншотов e2e/out/, журнал ошибок страницы. Запуск: node e2e/check*.mjs [url]
import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

/// URL из аргумента командной строки либо запасной (страница стенда по умолчанию).
export const cliUrl = (fallback = 'http://localhost:5215') => process.argv[2] ?? fallback;

export const outDir = (() => {
    const dir = fileURLToPath(new URL('./out/', import.meta.url));
    mkdirSync(dir, { recursive: true });
    return dir;
})();

/// Редактор готов: открыть страницу, дождаться рейки и полотна Blockly.
export async function openPage(url, { httpErrors = false, railTimeout = 30000 } = {}) {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
    page.on('pageerror', (e) => console.log('PAGE ERROR:', e.message));
    page.on('console', (m) => {
        if (m.type() === 'error') console.log('CONSOLE ERROR:', m.text());
    });
    if (httpErrors) {
        page.on('response', (r) => {
            if (r.status() >= 400) console.log('HTTP', r.status(), r.url());
        });
    }

    await page.goto(url, { waitUntil: 'load' });
    await page.waitForSelector('.pxb-rail', { timeout: railTimeout });
    await page.waitForSelector('.blocklySvg');
    await page.waitForTimeout(1200);
    return { browser, page };
}

/// Для page.evaluate: число блоков в открытом flyout.
export const flyoutBlocks = () =>
    [...document.querySelectorAll('.blocklyFlyout .blocklyDraggable')]
        .map((el) => el.getAttribute('data-id'))
        .filter(Boolean).length;
