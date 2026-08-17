// e2e-проверка flyout-ов стандартных категорий стенда PxBlocks (системный Edge, headless):
// Математика (мутатор math_is_divisibleby_mutator), Переменные (свой flyout-колбэк
// core.variables.*), Циклы, Текст. Любая ошибка Blockly — в PAGE/CONSOLE ERROR.
// Запуск: node e2e/check-flyouts.mjs [url]   (по умолчанию http://localhost:5215)
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

const flyoutBlocks = () =>
    [...document.querySelectorAll('.blocklyFlyout .blocklyDraggable')]
        .map((el) => el.getAttribute('data-id'))
        .filter(Boolean).length;

for (const category of ['Math', 'Loops', 'Text', 'Arrays']) {
    await page.click(`.pxb-rail-item:has-text("${category}")`);
    await page.waitForTimeout(600);
    console.log(`${category}: блоков во flyout =`, await page.evaluate(flyoutBlocks));
    await page.screenshot({ path: outDir + `flyout-${category}.png` });
}

// Функции: свой flyout-колбэк (штатный набор Blockly + досрочный procedures_return).
await page.click('.pxb-rail-item:has-text("Functions")');
await page.waitForTimeout(600);
console.log('Functions: блоков во flyout =', await page.evaluate(flyoutBlocks));
await page.screenshot({ path: outDir + 'flyout-Functions.png' });

// Переменные: создаём переменную кнопкой flyout-а, затем смотрим get/set.
await page.click('.pxb-rail-item:has-text("Variables")');
await page.waitForTimeout(600);
await page.click('.blocklyFlyout .blocklyFlyoutButton');
// Blockly 13: свой DOM-диалог вместо window.prompt.
await page.waitForSelector('dialog.blocklyDialog');
await page.fill('#blockly-form-input', 'counter');
await page.click('.blocklyDialogConfirmButton');
await page.waitForTimeout(600);
await page.click('.pxb-rail-item:has-text("Basic")'); // закрыть/сбросить flyout
await page.waitForTimeout(400);
await page.click('.pxb-rail-item:has-text("Variables")');
await page.waitForTimeout(600);
console.log('Variables: блоков во flyout =', await page.evaluate(flyoutBlocks));
await page.screenshot({ path: outDir + 'flyout-Variables.png' });

await browser.close();
console.log('OK, screenshots in e2e/out/');
