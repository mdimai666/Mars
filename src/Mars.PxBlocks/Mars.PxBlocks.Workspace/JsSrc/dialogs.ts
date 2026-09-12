import * as Blockly from 'blockly/core';

// Нативные окна Blockly (HTML <dialog class="blocklyDialog">) заменены окном модуля
// в стиле диалога функций: prompt — создание и переименование переменной, confirm —
// удаление переменной, alert — предупреждения (в т.ч. валидация функций).

let overlay: HTMLDivElement | null = null;
let keyHandler: ((event: KeyboardEvent) => void) | null = null;

function hideModal(): void {
    if (keyHandler) {
        document.removeEventListener('keydown', keyHandler, true);
        keyHandler = null;
    }
    overlay?.remove();
    overlay = null;
}

/// Каркас: оверлей + карточка + шапка (заголовок, затем кнопки); Escape — onEscape.
function createModal(title: string, onEscape: () => void): { header: HTMLDivElement; body: HTMLDivElement } {
    hideModal();

    const root = document.createElement('div');
    root.className = 'pxb-fn-overlay';

    const dialog = document.createElement('div');
    dialog.className = 'pxb-fn-dialog pxb-fn-dialog--prompt';

    const header = document.createElement('div');
    header.className = 'pxb-fn-header';
    const titleElement = document.createElement('span');
    titleElement.className = 'pxb-fn-title';
    titleElement.textContent = title;
    header.appendChild(titleElement);

    const body = document.createElement('div');
    body.className = 'pxb-fn-body';

    dialog.appendChild(header);
    dialog.appendChild(body);
    root.appendChild(dialog);
    document.body.appendChild(root);
    overlay = root;

    keyHandler = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            onEscape();
        }
    };
    document.addEventListener('keydown', keyHandler, true);

    return { header, body };
}

function addButton(header: HTMLDivElement, label: string, primary: boolean, onClick: () => void): void {
    const element = document.createElement('button');
    element.type = 'button';
    if (primary)
        element.className = 'pxb-fn-done';
    element.textContent = label;
    element.addEventListener('click', onClick);
    header.appendChild(element);
}

function showPrompt(
    message: string,
    defaultValue: string,
    callback: (result: string | null) => void,
): void {
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'pxb-fn-input';
    input.value = defaultValue;

    const finish = (result: string | null) => {
        hideModal();
        callback(result);
    };
    const modal = createModal(message, () => finish(null));
    modal.body.appendChild(input);
    addButton(modal.header, Blockly.Msg['DIALOG_OK'], true, () => finish(input.value));
    addButton(modal.header, Blockly.Msg['DIALOG_CANCEL'], false, () => finish(null));

    input.focus();
    input.select();
    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            finish(input.value);
        }
    });
}

function showConfirm(message: string, callback: (result: boolean) => void): void {
    const finish = (result: boolean) => {
        hideModal();
        callback(result);
    };
    const modal = createModal('', () => finish(false));
    modal.body.textContent = message;
    addButton(modal.header, Blockly.Msg['DIALOG_OK'], true, () => finish(true));
    addButton(modal.header, Blockly.Msg['DIALOG_CANCEL'], false, () => finish(false));
}

function showAlert(message: string, callback?: () => void): void {
    const finish = () => {
        hideModal();
        callback?.();
    };
    const modal = createModal('', finish);
    modal.body.textContent = message;
    addButton(modal.header, Blockly.Msg['DIALOG_OK'], true, finish);
}

export function ensureBlocklyDialogs(): void {
    Blockly.dialog.setPrompt(showPrompt);
    Blockly.dialog.setConfirm(showConfirm);
    Blockly.dialog.setAlert(showAlert);
}
