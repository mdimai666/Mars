import * as Blockly from 'blockly/core';

// «Шапка» хат-блоков (px_start/px_loop). В обход штатного style.hat: jsonInit Blockly
// читает style.hat и тут же обнуляет style ПРЯМО в общем JSON определения — шапка
// доставалась бы только первому созданному экземпляру блока (flyout → drag → flyout
// оставались без шапки). Расширение вызывается при каждом создании блока, поэтому
// hat выставляется здесь.
Blockly.Extensions.register('px_hat_cap', function (this: Blockly.Block) {
    (this as unknown as { hat?: string }).hat = 'cap';
});
