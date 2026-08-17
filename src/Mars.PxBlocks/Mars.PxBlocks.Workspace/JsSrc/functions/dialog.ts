import * as Blockly from 'blockly/core';
import {
    CREATE_FUNCTION_CALLBACK, FUNCTION_DECLARATION, FUNCTION_DEFINITION, ensureFunctionMsg,
} from './constants';
import {
    FunctionBlock, FunctionExtraState, asFunctionBlock, extraStateOf, getDefinition,
    mutateCallersAndDefinition, newFunctionExtraState, validateFunction,
} from './manager';

// Диалог создания/редактирования функции (аналог webapp/createFunction.tsx, без React):
// оверлей с мини-воркспейсом Blockly (function_declaration) и кнопками типов аргументов.
// Тип аргумента задаётся кнопкой добавления; имя — правкой поля редактора; удаление
// и перемещение параметра — иконками в виджете поля (field_argument_editor).

let overlay: HTMLDivElement | null = null;
let editorWorkspace: Blockly.WorkspaceSvg | null = null;
let declaration: FunctionBlock | null = null;

function hideDialog(): void {
    if (editorWorkspace) {
        editorWorkspace.dispose();
        editorWorkspace = null;
    }
    declaration = null;
    if (overlay) {
        overlay.remove();
        overlay = null;
    }
}

export function showFunctionDialog(
    workspace: Blockly.WorkspaceSvg,
    initial: FunctionExtraState,
    onConfirm: (state: FunctionExtraState) => void,
): void {
    ensureFunctionMsg();
    hideDialog();
    const msg = Blockly.Msg;

    overlay = document.createElement('div');
    overlay.className = 'pxb-fn-overlay';

    const dialog = document.createElement('div');
    dialog.className = 'pxb-fn-dialog';

    const header = document.createElement('div');
    header.className = 'pxb-fn-header';
    const title = document.createElement('span');
    title.className = 'pxb-fn-title';
    title.textContent = msg['FUNCTIONS_DIALOG_TITLE'];
    header.appendChild(title);

    const addButton = (label: string, add: (d: FunctionBlock) => void) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.textContent = label;
        button.addEventListener('click', () => declaration && add(declaration));
        header.appendChild(button);
    };
    addButton(msg['FUNCTIONS_ADD_NUMBER'], (d) => d.addNumberExternal());
    addButton(msg['FUNCTIONS_ADD_STRING'], (d) => d.addStringExternal());
    addButton(msg['FUNCTIONS_ADD_BOOLEAN'], (d) => d.addBooleanExternal());
    addButton(msg['FUNCTIONS_ADD_ARRAY'], (d) => d.addArrayExternal());

    const done = document.createElement('button');
    done.type = 'button';
    done.className = 'pxb-fn-done';
    done.textContent = msg['FUNCTIONS_DIALOG_DONE'];
    done.addEventListener('click', () => {
        if (!declaration) return;
        declaration.updateFunctionSignature();
        const state = extraStateOf(declaration);
        const error = validateFunction(state, workspace);
        if (error) {
            Blockly.dialog.alert(error);
            return;
        }
        hideDialog();
        onConfirm(state);
        workspace.refreshToolboxSelection();
    });
    header.appendChild(done);

    const cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.textContent = msg['FUNCTIONS_DIALOG_CANCEL'];
    cancel.addEventListener('click', hideDialog);
    header.appendChild(cancel);

    const editorHost = document.createElement('div');
    editorHost.className = 'pxb-fn-editor';

    dialog.appendChild(header);
    dialog.appendChild(editorHost);
    overlay.appendChild(dialog);
    document.body.appendChild(overlay);

    editorWorkspace = Blockly.inject(editorHost, {
        renderer: 'pxt',
        trashcan: false,
        sounds: false,
        move: { scrollbars: true, drag: true, wheel: true },
    });

    declaration = editorWorkspace.newBlock(FUNCTION_DECLARATION) as unknown as FunctionBlock;
    declaration.loadExtraState(initial);
    declaration.initSvg();
    declaration.render();

    editorWorkspace.addChangeListener(() => declaration?.updateFunctionSignature());
    try {
        editorWorkspace.centerOnBlock(declaration.id);
    } catch {
        // метрики ещё не готовы — не критично
    }
}

// «Edit Function» из контекстного меню определения (или вызова — идём в определение).
export function showEditDialogForBlock(block: FunctionBlock): void {
    const target = block.type === FUNCTION_DEFINITION
        ? block
        : getDefinition(block.name_, block.workspace) ?? block;
    showFunctionDialog(target.workspace as Blockly.WorkspaceSvg, extraStateOf(target), (state) => {
        mutateCallersAndDefinition(target.name_, target.workspace, state);
        target.updateDisplay_();
    });
}

// Кнопка «Make a Function...» во flyout категории Functions.
export function registerFunctionCallbacks(workspace: Blockly.WorkspaceSvg): void {
    ensureFunctionMsg();
    workspace.registerButtonCallback(CREATE_FUNCTION_CALLBACK, () => {
        Blockly.hideChaff();
        showFunctionDialog(workspace, newFunctionExtraState(workspace), (state) => {
            const definition = workspace.newBlock(FUNCTION_DEFINITION) as unknown as FunctionBlock;
            definition.loadExtraState(state);
            definition.initSvg();
            definition.render();
            const metrics = workspace.getMetrics();
            definition.moveBy(metrics.viewLeft + 40, metrics.viewTop + 40);
            workspace.refreshToolboxSelection();
        });
    });
}
