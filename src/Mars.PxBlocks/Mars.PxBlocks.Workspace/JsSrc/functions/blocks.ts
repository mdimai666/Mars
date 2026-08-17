import * as Blockly from 'blockly/core';
import {
    ARGUMENT_EDITOR, ARGUMENT_REPORTER, FUNCTION_CALL, FUNCTION_CALL_OUTPUT,
    FUNCTION_DECLARATION, FUNCTION_DEFINITION, FUNCTION_RETURN, FUNCTIONS_COLOUR,
    REPORTERS_COLOUR, DEFAULT_ARG_NAMES, FunctionArgument, checkForType,
    editorTypeFor, reporterTypeFor, shadowForType, isBuiltinType,
} from './constants';
import {
    FunctionBlock, asFunctionBlock, extraStateOf, getDefinition,
    mutateCallersAndDefinition, renameValidator,
} from './manager';
import { showEditDialogForBlock } from './dialog';
import { setDuplicateOnDragStrategy } from './duplicateOnDrag';
import './fieldArgumentEditor';

// Блоки редактора функций MakeCode (порт pxtblocks/plugins/functions):
// определение/вызовы с типизированными аргументами, репортёры параметров,
// диалоговая декларация и return с переключателем +/−. Сериализация —
// extraState {name, functionid, arguments:[{id,name,type}]} (ест её и наш Runtime).

interface MutableBlock extends FunctionBlock {
    returnValueVisible_?: boolean;
}

const commonMixin = {
    saveExtraState(this: FunctionBlock) {
        return extraStateOf(this);
    },

    loadExtraState(this: FunctionBlock, state: { name?: string; functionid?: string; arguments?: FunctionArgument[] }) {
        this.name_ = state.name ?? '';
        this.functionId_ = state.functionid ?? '';
        this.arguments_ = (state.arguments ?? []).map((a) => ({ ...a }));
        this.updateDisplay_();
    },

    mutationToDom(this: FunctionBlock): Element {
        const mutation = Blockly.utils.xml.createElement('mutation');
        mutation.setAttribute('name', this.name_);
        mutation.setAttribute('functionid', this.functionId_);
        for (const arg of this.arguments_) {
            const element = Blockly.utils.xml.createElement('arg');
            element.setAttribute('name', arg.name);
            element.setAttribute('id', arg.id);
            element.setAttribute('type', arg.type);
            mutation.appendChild(element);
        }
        return mutation;
    },

    domToMutation(this: FunctionBlock, element: Element) {
        this.name_ = element.getAttribute('name') ?? '';
        this.functionId_ = element.getAttribute('functionid') ?? '';
        this.arguments_ = [];
        for (const child of Array.from(element.children)) {
            if (child.tagName.toLowerCase() !== 'arg') continue;
            this.arguments_.push({
                name: child.getAttribute('name') ?? '',
                id: child.getAttribute('id') ?? '',
                type: child.getAttribute('type') ?? '',
            });
        }
        this.updateDisplay_();
    },

    // VALUE-входы по id аргументов, в порядке arguments_ сразу после имени;
    // «пасть» STACK — в конец (алгоритм reorder из commonFunctionMixin PXT).
    updateArgumentInputs_(this: FunctionBlock) {
        for (const input of [...this.inputList]) {
            if (input.type !== Blockly.inputs.inputTypes.VALUE) continue;
            if (!this.arguments_.some((a) => a.id === input.name)) this.removeInput(input.name);
        }
        let inputIndex = this.inputList.findIndex((i) => i.type === Blockly.inputs.inputTypes.VALUE);
        if (inputIndex === -1) inputIndex = this.inputList.length;
        for (const arg of this.arguments_) {
            const input = this.inputList.find((i) => i.name === arg.id) ?? this.appendValueInput(arg.id);
            if (this.inputList.indexOf(input) !== inputIndex) {
                this.moveInputBefore(input.name, this.inputList[inputIndex + 1]?.name ?? null);
            }
            input.setCheck(checkForType(arg.type));
            inputIndex++;
        }
        if (this.getInput('STACK')) this.moveInputBefore('STACK', null);
    },
};

function ensureShadow(input: Blockly.Input, type: string, fieldName: string, text: string): void {
    let target = input.connection?.targetBlock() ?? null;
    if (!target || target.type !== type) {
        target?.dispose(false);
        const shadow = input.getSourceBlock().workspace.newBlock(type);
        shadow.setShadow(true);
        if (fieldName) shadow.setFieldValue(text, fieldName);
        (shadow as Blockly.BlockSvg).initSvg();
        input.connection?.connect(shadow.outputConnection as Blockly.Connection);
        target = shadow;
    } else if (fieldName) {
        target.setFieldValue(text, fieldName);
    }
    setDuplicateOnDragStrategy(target);
}

// ── function_definition ──────────────────────────────────────────────────────

Blockly.Blocks[FUNCTION_DEFINITION] = {
    ...commonMixin,

    init(this: FunctionBlock) {
        this.name_ = '';
        this.functionId_ = '';
        this.arguments_ = [];
        Blockly.Extensions.apply('px_hat_cap', this, false);
        this.setColour(FUNCTIONS_COLOUR);
        this.setInputsInline(true);
        this.appendDummyInput('function_title')
            .appendField(new Blockly.FieldLabel(Blockly.Msg['FUNCTIONS_DEFNORETURN_TITLE']), 'function_title_label');
        this.appendDummyInput('function_name')
            .appendField(new Blockly.FieldTextInput('', renameValidator), 'function_name');
        this.appendStatementInput('STACK');
        this.setTooltip('A function with typed arguments; use return to give a value back.');
    },

    updateDisplay_(this: FunctionBlock) {
        this.updateArgumentInputs_();
        const nameField = this.getField('function_name');
        if (nameField && nameField.getValue() !== this.name_) nameField.setValue(this.name_);
        for (const arg of this.arguments_) {
            const input = this.getInput(arg.id);
            if (!input) continue;
            ensureShadow(input, reporterTypeFor(arg.type), 'VALUE', arg.name);
        }
    },

    customContextMenu(this: FunctionBlock, options: Array<Record<string, unknown>>) {
        if (this.isInFlyout) return;
        options.push({
            enabled: true,
            text: Blockly.Msg['FUNCTIONS_EDIT_OPTION'],
            callback: () => showEditDialogForBlock(this),
        });
        const block = this;
        options.push({
            enabled: true,
            text: (Blockly.Msg['FUNCTIONS_CREATE_CALL_OPTION'] ?? 'Create \'call %1\'').replace('%1', this.name_),
            callback: () => {
                const caller = block.workspace.newBlock(FUNCTION_CALL) as unknown as FunctionBlock;
                caller.loadExtraState(extraStateOf(block));
                caller.initSvg();
                caller.render();
                const xy = block.getRelativeToSurfaceXY();
                caller.moveBy(xy.x + 50, xy.y + 80);
            },
        });
    },
};

// ── function_call / function_call_output ─────────────────────────────────────

function initCall(this: FunctionBlock, withOutput: boolean) {
    this.name_ = '';
    this.functionId_ = '';
    this.arguments_ = [];
    this.setColour(FUNCTIONS_COLOUR);
    this.setInputsInline(true);
    if (withOutput) this.setOutput(true);
    else {
        this.setPreviousStatement(true);
        this.setNextStatement(true);
    }
    this.appendDummyInput('function_title')
        .appendField(new Blockly.FieldLabel(Blockly.Msg['FUNCTIONS_CALL_TITLE']), 'function_title_label');
    this.appendDummyInput('function_name')
        .appendField(new Blockly.FieldLabel(''), 'function_name_label');
}

const callMixin = {
    ...commonMixin,

    updateDisplay_(this: FunctionBlock) {
        this.updateArgumentInputs_();
        this.getField('function_name_label')?.setValue(this.name_);
        for (const arg of this.arguments_) {
            const input = this.getInput(arg.id);
            if (!input) continue;
            const shadow = shadowForType(arg.type);
            if (shadow) ensureShadow(input, shadow.type, shadow.field, shadow.value);
        }
    },

    onchange(this: FunctionBlock, event: Blockly.Events.Abstract) {
        if (this.isInFlyout || this.workspace.isReadOnly()) return;
        if (event.type === Blockly.Events.BLOCK_CREATE && (event as Blockly.Events.BlockCreate).ids?.includes(this.id)) {
            const definition = getDefinition(this.name_, this.workspace);
            if (definition) {
                // Сигнатура могла разъехаться при копировании — синхронизируем с определением.
                if (JSON.stringify(definition.arguments_) !== JSON.stringify(this.arguments_)) {
                    this.loadExtraState(extraStateOf(definition));
                }
            } else if (this.name_) {
                // Вызов без определения (вставили извне) — создаём определение рядом.
                const definition = this.workspace.newBlock(FUNCTION_DEFINITION) as unknown as FunctionBlock;
                definition.loadExtraState(extraStateOf(this));
                definition.initSvg();
                definition.render();
                const xy = this.getRelativeToSurfaceXY();
                definition.moveBy(xy.x - 50, xy.y - 120);
            }
        }
        if (event.type === Blockly.Events.BLOCK_DELETE && this.name_ && !this.disposed
            && !getDefinition(this.name_, this.workspace)) {
            this.dispose(false);
        }
    },

    customContextMenu(this: FunctionBlock, options: Array<Record<string, unknown>>) {
        if (this.isInFlyout) return;
        const block = this;
        options.push({
            enabled: true,
            text: Blockly.Msg['FUNCTIONS_GO_TO_DEFINITION_OPTION'],
            callback: () => getDefinition(block.name_, block.workspace)?.select(),
        });
    },
};

Blockly.Blocks[FUNCTION_CALL] = {
    ...callMixin,
    init(this: FunctionBlock) {
        initCall.call(this, false);
    },
};

Blockly.Blocks[FUNCTION_CALL_OUTPUT] = {
    ...callMixin,
    init(this: FunctionBlock) {
        initCall.call(this, true);
    },
};

// ── argument_reporter_* / argument_editor_* ─────────────────────────────────

const REPORTER_TYPES: Array<[string, string | null]> = [
    ['boolean', 'Boolean'],
    ['number', 'Number'],
    ['string', 'String'],
    ['array', 'Array'],
    ['custom', null],
];

for (const [suffix, check] of REPORTER_TYPES) {
    Blockly.Blocks[ARGUMENT_REPORTER + suffix] = {
        init(this: Blockly.Block) {
            this.jsonInit({
                message0: '%1',
                args0: [{ type: 'field_label_serializable', name: 'VALUE', text: '' }],
                colour: REPORTERS_COLOUR,
                output: check,
            });
            setDuplicateOnDragStrategy(this);
        },
        mutationToDom(this: Blockly.Block): Element | null {
            if (suffix !== 'custom') return null;
            const mutation = Blockly.utils.xml.createElement('mutation');
            mutation.setAttribute('typename', (this as unknown as { typeName_: string }).typeName_ ?? '');
            return mutation;
        },
        domToMutation(this: Blockly.Block, element: Element) {
            if (suffix !== 'custom') return;
            const typeName = element.getAttribute('typename') ?? '';
            (this as unknown as { typeName_: string }).typeName_ = typeName;
            this.setOutput(true, typeName);
        },
    };

    Blockly.Blocks[ARGUMENT_EDITOR + suffix] = {
        init(this: Blockly.Block) {
            this.jsonInit({
                message0: '%1',
                args0: [{
                    type: 'field_argument_editor',
                    name: 'TEXT',
                    text: DEFAULT_ARG_NAMES[suffix === 'array' ? 'Array' : suffix] ?? 'arg',
                }],
                colour: REPORTERS_COLOUR,
                output: check,
            });
        },
        getTypeName(this: Blockly.Block): string {
            if (suffix === 'custom') return (this as unknown as { typeName_: string }).typeName_ ?? 'any';
            return suffix === 'array' ? 'Array' : suffix;
        },
    };
}

// ── function_declaration (только в диалоге) ─────────────────────────────────

Blockly.Blocks[FUNCTION_DECLARATION] = {
    ...commonMixin,

    init(this: FunctionBlock) {
        this.name_ = '';
        this.functionId_ = '';
        this.arguments_ = [];
        Blockly.Extensions.apply('px_hat_cap', this, false);
        this.setColour(FUNCTIONS_COLOUR);
        this.setInputsInline(true);
        this.setDeletable(false);
        this.setMovable(false);
        this.appendDummyInput('function_title')
            .appendField(new Blockly.FieldLabel(Blockly.Msg['FUNCTIONS_DEFNORETURN_TITLE']), 'function_title_label');
        this.appendDummyInput('function_name')
            .appendField(new Blockly.FieldTextInput(''), 'function_name');
        this.appendStatementInput('STACK');
    },

    updateDisplay_(this: FunctionBlock) {
        this.updateArgumentInputs_();
        this.getField('function_name')?.setValue(this.name_);
        for (const arg of this.arguments_) {
            const input = this.getInput(arg.id);
            if (!input) continue;
            ensureShadow(input, editorTypeFor(arg.type), 'TEXT', arg.name);
        }
    },

    addParam_(this: FunctionBlock, type: string, defaultName: string) {
        const names = this.arguments_.map((a) => a.name);
        let name = defaultName;
        let suffix = 2;
        while (names.includes(name)) name = `${defaultName}${suffix++}`;
        this.arguments_.push({ id: Blockly.utils.idGenerator.genUid(), name, type });
        this.updateDisplay_();
    },

    addNumberExternal(this: FunctionBlock) {
        this.addParam_('number', DEFAULT_ARG_NAMES.number);
    },
    addStringExternal(this: FunctionBlock) {
        this.addParam_('string', DEFAULT_ARG_NAMES.string);
    },
    addBooleanExternal(this: FunctionBlock) {
        this.addParam_('boolean', DEFAULT_ARG_NAMES.boolean);
    },
    addArrayExternal(this: FunctionBlock) {
        this.addParam_('Array', DEFAULT_ARG_NAMES.Array);
    },

    // Перечитывает сигнатуру с мини-воркспейса: имя поля + подключённые редакторы.
    updateFunctionSignature(this: FunctionBlock) {
        this.name_ = this.getField('function_name')?.getValue() ?? '';
        const next: FunctionArgument[] = [];
        for (const input of this.inputList) {
            if (input.type !== Blockly.inputs.inputTypes.VALUE) continue;
            const editor = input.connection?.targetBlock();
            if (!editor) continue;
            const typeName = typeof (editor as unknown as { getTypeName?: () => string }).getTypeName === 'function'
                ? (editor as unknown as { getTypeName: () => string }).getTypeName()
                : 'string';
            next.push({
                id: input.name,
                name: editor.getFieldValue('TEXT') ?? '',
                type: typeName,
            });
        }
        this.arguments_ = next;
    },
};

// ── function_return с переключателем +/− ────────────────────────────────────

const svgUri = (path: string) => 'data:image/svg+xml;utf8,'
    + encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="#ffffff" d="${path}"/></svg>`);
const PLUS_PATH = 'M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z';
const MINUS_PATH = 'M5 11h14v2H5z';

Blockly.Blocks[FUNCTION_RETURN] = {
    init(this: MutableBlock) {
        this.returnValueVisible_ = true;
        this.setColour(FUNCTIONS_COLOUR);
        this.setInputsInline(true);
        this.setPreviousStatement(true);
        this.appendDummyInput('return_label').appendField(new Blockly.FieldLabel('return'), 'return_text');
        this.updateShape_();
    },

    updateShape_(this: MutableBlock) {
        const valueInput = this.getInput('RETURN_VALUE');
        if (this.returnValueVisible_) {
            if (!valueInput) this.appendValueInput('RETURN_VALUE');
            this.getInput('add_button')?.dispose();
            if (!this.getInput('rem_button')) {
                this.appendDummyInput('rem_button').appendField(new Blockly.FieldImage(
                    svgUri(MINUS_PATH), 24, 24, 'Remove return value', (field) => {
                        const block = field.getSourceBlock() as MutableBlock;
                        block.returnValueVisible_ = false;
                        block.updateShape_();
                    }));
            }
        } else {
            valueInput?.dispose();
            this.getInput('rem_button')?.dispose();
            if (!this.getInput('add_button')) {
                this.appendDummyInput('add_button').appendField(new Blockly.FieldImage(
                    svgUri(PLUS_PATH), 24, 24, 'Add return value', (field) => {
                        const block = field.getSourceBlock() as MutableBlock;
                        block.returnValueVisible_ = true;
                        block.updateShape_();
                    }));
            }
        }
    },

    mutationToDom(this: MutableBlock): Element {
        const mutation = Blockly.utils.xml.createElement('mutation');
        mutation.setAttribute('no_return_value', this.returnValueVisible_ ? 'false' : 'true');
        return mutation;
    },

    domToMutation(this: MutableBlock, element: Element) {
        this.returnValueVisible_ = element.getAttribute('no_return_value') !== 'true';
        this.updateShape_();
    },

    saveExtraState(this: MutableBlock) {
        return { noReturnValue: !this.returnValueVisible_ };
    },

    loadExtraState(this: MutableBlock, state: { noReturnValue?: boolean }) {
        this.returnValueVisible_ = !state.noReturnValue;
        this.updateShape_();
    },
};

// Типизированные аргументы вне built-in (custom) терпим при загрузке чужих сценариев.
export const CUSTOM_TYPES_ALLOWED = isBuiltinType;
