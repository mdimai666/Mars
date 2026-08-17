// e2e-проверка редактора функций MakeCode (Этап 14C): flyout «Functions»
// (кнопка «Make a Function...», return с +/−), диалог создания функции
// с типизированным аргументом, появление определения и call-блока во flyout.
// Запуск: node e2e/check-functions.mjs [url]   (по умолчанию http://localhost:5215)
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

// 1. Flyout «Functions»: кнопка + return.
await page.click('.pxb-rail-item:has-text("Functions")');
await page.waitForTimeout(600);
console.log('Functions flyout: блоков =', await page.evaluate(flyoutBlocks));
console.log('Кнопка Make a Function... =', await page.locator('.blocklyFlyout .blocklyFlyoutButton').count());
await page.screenshot({ path: outDir + 'flyout-Functions.png' });

// 2. Диалог создания функции.
await page.click('.blocklyFlyout .blocklyFlyoutButton');
await page.waitForSelector('.pxb-fn-overlay');
await page.waitForTimeout(600);
await page.screenshot({ path: outDir + 'fn-dialog-empty.png' });

// 3. Два аргумента (num, text); имена параметров читаем с полей декларации:
//    текстовые поля в Blockly 13 несут класс blocklyTextInputField; порядок в
//    DOM после moveInputBefore не меняется, поэтому сортируем по X (первое
//    поле слева — имя функции, остальные — параметры в порядке сигнатуры).
const argNames = () =>
    [...document.querySelectorAll('.pxb-fn-editor g.blocklyTextInputField')]
        .map((g) => ({ x: g.getBoundingClientRect().x, t: g.textContent?.trim() }))
        .sort((a, b) => a.x - b.x)
        .slice(1).map((o) => o.t);
await page.click('.pxb-fn-header button:has-text("+ Number")');
await page.waitForTimeout(400);
await page.click('.pxb-fn-header button:has-text("+ Text")');
await page.waitForTimeout(600);
console.log('Параметры после добавления:', await page.evaluate(argNames));
await page.screenshot({ path: outDir + 'fn-dialog-args.png' });

// 4. Клик по полю параметра открывает виджет с иконками (вверх/корзинка/вниз).
await page.locator('.pxb-fn-editor g.blocklyTextInputField').nth(1).click();
await page.waitForTimeout(400);
console.log('Иконки виджета (up/remove/down):',
    await page.locator('.argumentEditorMoveUpIcon').count(),
    await page.locator('.argumentEditorRemoveIcon').count(),
    await page.locator('.argumentEditorMoveDownIcon').count());
await page.screenshot({ path: outDir + 'fn-widget-icons.png' });

// 5. Перемещаем первый параметр вниз.
await page.click('.argumentEditorMoveDownIcon');
await page.waitForTimeout(400);
console.log('Параметры после перемещения:', await page.evaluate(argNames));
await page.screenshot({ path: outDir + 'fn-dialog-moved.png' });

// 6. Удаляем первый параметр (теперь это text) — кликаем поле по тексту.
await page.locator('.pxb-fn-editor g.blocklyTextInputField', { hasText: 'text' }).click();
await page.waitForTimeout(400);
await page.click('.argumentEditorRemoveIcon');
await page.waitForTimeout(400);
console.log('Параметры после удаления:', await page.evaluate(argNames));
await page.screenshot({ path: outDir + 'fn-dialog-after-remove.png' });

// 7. Подтверждаем — определение с одним параметром сразу после имени.
await page.click('.pxb-fn-header button.pxb-fn-done');
await page.waitForTimeout(800);
console.log('Оверлей закрыт:', await page.locator('.pxb-fn-overlay').count() === 0);
await page.screenshot({ path: outDir + 'fn-created.png' });

// 4. Во flyout теперь есть call-блок определённой функции.
await page.click('.pxb-rail-item:has-text("Basic")');
await page.waitForTimeout(400);
await page.click('.pxb-rail-item:has-text("Functions")');
await page.waitForTimeout(600);
console.log('Functions flyout после создания: блоков =', await page.evaluate(flyoutBlocks));
await page.screenshot({ path: outDir + 'flyout-Functions-after.png' });

await browser.close();
console.log('OK, screenshots in e2e/out/');
