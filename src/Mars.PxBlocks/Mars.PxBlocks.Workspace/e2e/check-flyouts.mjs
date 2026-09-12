// e2e-проверка flyout-ов стандартных категорий стенда PxBlocks (системный Edge, headless):
// Математика (мутатор math_is_divisibleby_mutator), Переменные (свой flyout-колбэк
// core.variables.*), Циклы, Текст. Любая ошибка Blockly — в PAGE/CONSOLE ERROR.
// Запуск: node e2e/check-flyouts.mjs [url]   (по умолчанию http://localhost:5215)
import { cliUrl, flyoutBlocks, openPage, outDir } from './helpers.mjs';

const { browser, page } = await openPage(cliUrl());

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
