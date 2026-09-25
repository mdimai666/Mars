// JS-модуль рабочей области DataSource: копирование полных значений ячеек.

const fullValueAttribute = 'data-full';

/**
 * Ячейки с data-full показывают усечённый текст (сжатый guid, json-сводка).
 * Обработчик copy подменяет выделение полными значениями, иначе скопированное
 * «начало…конец» нельзя вставить обратно в SQL.
 */
export function registerFullValueCopy(container) {
    if (!container) return;
    container.addEventListener('copy', onCopy);
}

function onCopy(e) {
    if (!e.clipboardData) return;

    const selection = document.getSelection();
    if (!selection || selection.isCollapsed || selection.rangeCount === 0) return;

    // Внутри input/textarea (в том числе в редакторе кода) копируем как обычно.
    const anchor = selection.anchorNode;
    const anchorElement = anchor?.nodeType === Node.ELEMENT_NODE ? anchor : anchor?.parentElement;
    if (anchorElement?.closest('input, textarea')) return;

    const range = selection.getRangeAt(0);
    const start = range.startContainer.nodeType === Node.ELEMENT_NODE
        ? range.startContainer
        : range.startContainer.parentElement;
    const table = start?.closest('table');
    if (!table || !table.querySelector(`[${fullValueAttribute}]`)) return;

    const lines = [];

    for (const row of table.querySelectorAll('tbody tr')) {
        if (!range.intersectsNode(row)) continue;

        const cells = [];

        for (const cell of row.cells) {
            if (!range.intersectsNode(cell)) continue;

            const full = cell.querySelector(`[${fullValueAttribute}]`);
            cells.push(full ? full.getAttribute(fullValueAttribute) : cell.innerText.trim());
        }

        if (cells.length) lines.push(cells.join('\t'));
    }

    if (lines.length === 0) return;

    e.clipboardData.setData('text/plain', lines.join('\n'));
    e.preventDefault();
}
