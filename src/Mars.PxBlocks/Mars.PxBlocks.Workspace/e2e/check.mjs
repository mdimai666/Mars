// e2e-проверка стенда PxBlocks системным Edge (headless): замеры ширины + скриншоты.
// Запуск: node e2e/check.mjs [url]   (по умолчанию http://localhost:5215)
import { chromium } from 'playwright';
import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const url = process.argv[2] ?? 'http://localhost:5215';
const outDir = fileURLToPath(new URL('./out/', import.meta.url));
mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ channel: 'msedge', headless: true });
const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
page.on('pageerror', (e) => console.log('PAGE ERROR:', e.message));
page.on('console', (m) => {
    if (m.type() === 'error') console.log('CONSOLE ERROR:', m.text());
});

await page.goto(url, { waitUntil: 'load' });
await page.waitForSelector('.pxb-rail');
await page.waitForSelector('.blocklySvg');
await page.waitForTimeout(1200);

const measure = () => ({
    svgAttrWidth: document.querySelector('.blocklySvg')?.getAttribute('width'),
    svgRectWidth: Math.round(document.querySelector('.blocklySvg')?.getBoundingClientRect().width ?? -1),
    workspaceWidth: document.querySelector('.px-blocks-workspace')?.offsetWidth,
    injectionWidth: document.querySelector('.px-blocks-workspace .injectionDiv')?.offsetWidth,
    toolboxDisplay: document.querySelector('.px-blocks-workspace .blocklyToolbox')
        ? getComputedStyle(document.querySelector('.px-blocks-workspace .blocklyToolbox')).display
        : 'absent',
    toolboxWidth: document.querySelector('.px-blocks-workspace .blocklyToolbox')?.offsetWidth,
    railWidth: document.querySelector('.pxb-rail')?.offsetWidth,
    bodyScrollWidth: document.body.scrollWidth,
    bodyClientWidth: document.body.clientWidth,
});

console.log('INITIAL:', JSON.stringify(await page.evaluate(measure)));
await page.screenshot({ path: outDir + 'initial.png' });

await page.click('.pxb-rail-item:has-text("Логика")');
await page.waitForTimeout(600);
console.log('CATEGORY OPEN:', JSON.stringify(await page.evaluate(measure)));
await page.screenshot({ path: outDir + 'category.png' });

await page.setViewportSize({ width: 1400, height: 900 });
await page.waitForTimeout(600);
console.log('AFTER RESIZE:', JSON.stringify(await page.evaluate(measure)));
await page.screenshot({ path: outDir + 'resized.png' });

await browser.close();
console.log('OK, screenshots in e2e/out/');
