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

// Центровка: создаём блок кликом по flyout (автосейв), перезагружаем —
// при старте содержимое должно оказаться в центре видимой области.
// Блок if — C-образный: центр bbox попадает в вырез, кликаем в сплошную верхнюю планку.
await page.click('.blocklyFlyout .blocklyDraggable', { position: { x: 60, y: 16 } });
await page.waitForTimeout(800);
await page.reload({ waitUntil: 'load' });
await page.waitForSelector('.pxb-rail');
await page.waitForSelector('.blocklySvg');
await page.waitForTimeout(1200);

const centers = () => {
    const svg = document.querySelector('.blocklySvg').getBoundingClientRect();
    const rects = [...document.querySelectorAll('.blocklyBlockCanvas .blocklyDraggable')]
        .map((el) => el.getBoundingClientRect());
    if (!rects.length) return { blocks: 0 };
    const left = Math.min(...rects.map((r) => r.left));
    const top = Math.min(...rects.map((r) => r.top));
    const right = Math.max(...rects.map((r) => r.right));
    const bottom = Math.max(...rects.map((r) => r.bottom));
    return {
        blocks: rects.length,
        svgCenter: [Math.round(svg.left + svg.width / 2), Math.round(svg.top + svg.height / 2)],
        blocksCenter: [Math.round((left + right) / 2), Math.round((top + bottom) / 2)],
    };
};

console.log('CENTERED AT START:', JSON.stringify(await page.evaluate(centers)));
await page.screenshot({ path: outDir + 'centered.png' });

// Сдвигаем полотно и возвращаем кнопкой Center.
await page.mouse.move(1200, 700);
await page.mouse.down();
await page.mouse.move(700, 300, { steps: 5 });
await page.mouse.up();
await page.waitForTimeout(400);
console.log('AFTER PAN:', JSON.stringify(await page.evaluate(centers)));
await page.click('button:has-text("Center")');
await page.waitForTimeout(400);
console.log('AFTER CENTER BTN:', JSON.stringify(await page.evaluate(centers)));
await page.screenshot({ path: outDir + 'centered-btn.png' });

await browser.close();
console.log('OK, screenshots in e2e/out/');
