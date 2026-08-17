import * as Blockly from 'blockly/core';

// Порт редактора функций MakeCode (pxtblocks/plugins/functions), Этап 14C.
// Типы блоков и базовые строки — как в PXT; цвета — наша палитра категорий
// (functions = hue 290 штатных процедур, reporters = цвет переменных).

export const FUNCTION_DEFINITION = 'function_definition';
export const FUNCTION_CALL = 'function_call';
export const FUNCTION_CALL_OUTPUT = 'function_call_output';
export const FUNCTION_DECLARATION = 'function_declaration';
export const FUNCTION_RETURN = 'function_return';
export const ARGUMENT_REPORTER = 'argument_reporter_';
export const ARGUMENT_EDITOR = 'argument_editor_';

export const FUNCTIONS_COLOUR = '#995ba5';
export const REPORTERS_COLOUR = '#a55b80';

export const CREATE_FUNCTION_CALLBACK = 'CREATE_FUNCTION';

export interface FunctionArgument {
    id: string;
    name: string;
    type: string;
}

// Встроенные типы аргументов MakeCode; всё остальное — custom (у нас не создаётся,
// но чужие сценарии с typename парсер/блоки терпят).
export const BUILTIN_ARG_TYPES = ['number', 'string', 'boolean', 'Array'];

export const DEFAULT_ARG_NAMES: Record<string, string> = {
    number: 'num',
    string: 'text',
    boolean: 'bool',
    Array: 'array',
};

export const DEFAULT_FUNCTION_NAME = 'doSomething';

export function isBuiltinType(type: string): boolean {
    return BUILTIN_ARG_TYPES.includes(type);
}

// check value-входа: примитивы с большой буквы (Boolean/Number/String), Array и custom как есть.
export function checkForType(type: string): string {
    if (!isBuiltinType(type)) return type;
    if (type === 'Array') return 'Array';
    return type.charAt(0).toUpperCase() + type.slice(1);
}

// Блок-репортёр параметра по типу аргумента.
export function reporterTypeFor(type: string): string {
    switch (type) {
        case 'boolean': return ARGUMENT_REPORTER + 'boolean';
        case 'number': return ARGUMENT_REPORTER + 'number';
        case 'string': return ARGUMENT_REPORTER + 'string';
        case 'Array': return ARGUMENT_REPORTER + 'array';
        default: return ARGUMENT_REPORTER + 'custom';
    }
}

// Блок-редактор параметра в диалоге по типу аргумента.
export function editorTypeFor(type: string): string {
    switch (type) {
        case 'boolean': return ARGUMENT_EDITOR + 'boolean';
        case 'number': return ARGUMENT_EDITOR + 'number';
        case 'string': return ARGUMENT_EDITOR + 'string';
        case 'Array': return ARGUMENT_EDITOR + 'array';
        default: return ARGUMENT_EDITOR + 'custom';
    }
}

// Shadow-блок значения в call-блоке по типу аргумента (наши core.* вместо math_number и т.п.).
export function shadowForType(type: string): { type: string; field: string; value: string } | null {
    switch (type) {
        case 'boolean': return { type: 'core.logic.boolean', field: 'BOOL', value: 'TRUE' };
        case 'number': return { type: 'core.math.number', field: 'NUM', value: '1' };
        case 'string': return { type: 'core.text.text', field: 'TEXT', value: 'abc' };
        case 'Array': return { type: 'lists_create_empty', field: '', value: '' };
        default: return null;
    }
}

// Английские строки редактора функций (стандартные MakeCode, как в msg.ts плагина).
export function ensureFunctionMsg(): void {
    const msg = Blockly.Msg;
    const define = (key: string, value: string) => {
        if (!msg[key]) msg[key] = value;
    };
    define('FUNCTIONS_DEFNORETURN_TITLE', 'function');
    define('FUNCTIONS_CALL_TITLE', 'call');
    define('FUNCTION_CREATE_NEW', 'Make a Function...');
    define('FUNCTION_FLYOUT_LABEL', 'Your Functions');
    define('FUNCTIONS_EDIT_OPTION', 'Edit Function');
    define('FUNCTIONS_CREATE_CALL_OPTION', 'Create \'call %1\'');
    define('FUNCTIONS_GO_TO_DEFINITION_OPTION', 'Go to Definition');
    define('FUNCTIONS_DELETE_PARAMETER_BUTTON', 'Delete');
    define('FUNCTION_WARNING_DUPLICATE_ARG', 'Functions cannot use the same argument name more than once.');
    define('FUNCTION_WARNING_ARG_NAME_IS_FUNCTION_NAME', 'Argument names must not be the same as the function name.');
    define('FUNCTION_WARNING_EMPTY_NAME', 'Function and argument names cannot be empty.');
    define('FUNCTIONS_DIALOG_TITLE', 'Edit Function');
    define('FUNCTIONS_DIALOG_DONE', 'Done');
    define('FUNCTIONS_DIALOG_CANCEL', 'Cancel');
    define('FUNCTIONS_ADD_NUMBER', '+ Number');
    define('FUNCTIONS_ADD_STRING', '+ Text');
    define('FUNCTIONS_ADD_BOOLEAN', '+ Boolean');
    define('FUNCTIONS_ADD_ARRAY', '+ Array');
}
