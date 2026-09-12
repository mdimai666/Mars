import * as Blockly from 'blockly/core';

// Нативный prompt Blockly (HTML <dialog class="blocklyDialog") заменён окном модуля
// в стиле диалога функций: через него идут создание и переименование переменных,
// а также «New variable…» из выпадающих полей переменных.

let overlay: HTMLDivElement | null = null;

function hidePrompt(): void {
    overlay?.remove();
    overlay = null;
}

function showPrompt(
    message: string,
    defaultValue: string,
    callback: (result: string | null) => void,
): void {
    hidePrompt();

    const root = document.createElement('div');
    root.className = 'pxb-fn-overlay';

    const dialog = document.createElement('div');
    dialog.className = 'pxb-fn-dialog pxb-fn-dialog--prompt';

    const header = document.createElement('div');
    header.className = 'pxb-fn-header';
    const title = document.createElement('span');
    title.className = 'pxb-fn-title';
    title.textContent = message;
    header.appendChild(title);

    const done = document.createElement('button');
    done.type = 'button';
    done.className = 'pxb-fn-done';
    done.textContent = Blockly.Msg['DIALOG_OK'];
    done.addEventListener('click', () => finish(input.value));
    header.appendChild(done);

    const cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.textContent = Blockly.Msg['DIALOG_CANCEL'];
    cancel.addEventListener('click', () => finish(null));
    header.appendChild(cancel);

    const body = document.createElement('div');
    body.className = 'pxb-fn-body';
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'pxb-fn-input';
    input.value = defaultValue;
    body.appendChild(input);

    dialog.appendChild(header);
    dialog.appendChild(body);
    root.appendChild(dialog);
    document.body.appendChild(root);
    overlay = root;

    input.focus();
    input.select();
    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            finish(input.value);
        } else if (event.key === 'Escape') {
            event.preventDefault();
            finish(null);
        }
    });

    function finish(result: string | null): void {
        hidePrompt();
        callback(result);
    }
}

export function ensurePromptDialog(): void {
    Blockly.dialog.setPrompt(showPrompt);
}
