import * as Blockly from 'blockly/core';

// Блок «создать объект»: кнопка «+» добавляет пары поле→значение (input_value),
// состояние (имена полей) сохраняется через saveExtraState/loadExtraState.
// Регистрируется как mutator и включается свойством "mutator" в определении блока:
// Blockly запрещает не-мутаторным расширениям менять mutator-свойства блока.

const PLUS_ICON =
    'data:image/svg+xml,' +
    encodeURIComponent(
        `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">` +
        `<circle cx="12" cy="12" r="11" fill="#8889dd"/>` +
        `<path d="M12 6v12M6 12h12" stroke="#fff" stroke-width="3" stroke-linecap="round"/>` +
        `</svg>`);

const VALUE_PREFIX = 'px_obj_value_';
const KEY_PREFIX = 'px_obj_key_';
const PLUS_INPUT = 'px_obj_plus';

interface ObjectBuilderState {
    keys: string[];
}

type ObjectBuilderBlock = Blockly.Block & {
    pxRowCounter: number;
    saveExtraState(): ObjectBuilderState;
    loadExtraState(state: ObjectBuilderState): void;
};

function addRow(block: ObjectBuilderBlock, key: string): void {
    block.pxRowCounter++;
    block.appendValueInput(VALUE_PREFIX + block.pxRowCounter)
        .appendField('field')
        .appendField(new Blockly.FieldTextInput(key), KEY_PREFIX + block.pxRowCounter)
        .appendField('value');
}

function removeDynamicRows(block: ObjectBuilderBlock): void {
    for (let i = block.pxRowCounter; i >= 1; i--) {
        block.removeInput(VALUE_PREFIX + i, true);
    }
    block.pxRowCounter = 0;
}

function ensurePlusRow(block: ObjectBuilderBlock): void {
    block.removeInput(PLUS_INPUT, true);
    block.appendDummyInput(PLUS_INPUT)
        .appendField(new Blockly.FieldImage(PLUS_ICON, 20, 20, 'add field', onPlusClick));
}

function onPlusClick(field: Blockly.FieldImage): void {
    const block = field.getSourceBlock() as ObjectBuilderBlock;
    if (block.isInFlyout) {
        return;
    }
    addRow(block, 'field' + (block.pxRowCounter + 1));
    ensurePlusRow(block);
}

Blockly.Extensions.registerMutator(
    'px_object_builder',
    {
        saveExtraState(this: ObjectBuilderBlock): ObjectBuilderState {
            const keys: string[] = [];
            for (let i = 1; i <= this.pxRowCounter; i++) {
                const field = this.getField(KEY_PREFIX + i) as Blockly.FieldTextInput | null;
                keys.push(field ? String(field.getValue()) : '');
            }
            return { keys };
        },

        loadExtraState(this: ObjectBuilderBlock, state: ObjectBuilderState): void {
            removeDynamicRows(this);
            for (const key of state.keys ?? []) {
                addRow(this, key);
            }
            ensurePlusRow(this);
        },
    },
    function (this: ObjectBuilderBlock) {
        this.pxRowCounter = 0;
        this.setInputsInline(false);
        addRow(this, 'field1');
        ensurePlusRow(this);
    });
