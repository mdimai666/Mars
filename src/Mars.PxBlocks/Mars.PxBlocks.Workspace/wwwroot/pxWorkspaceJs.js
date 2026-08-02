export function findBlockId(clientX, clientY) {
    const el = document.elementFromPoint(clientX, clientY);
    if (!el) return null;
    const block = el.closest('[data-block-id]');
    return block ? block.getAttribute('data-block-id') : null;
}

export function findFieldInfo(clientX, clientY) {
    const el = document.elementFromPoint(clientX, clientY);
    if (!el) return null;

    const fieldGroup = el.closest('[data-field-name]');
    if (!fieldGroup) return null;

    return {
        blockId: fieldGroup.getAttribute('data-block-id'),
        fieldName: fieldGroup.getAttribute('data-field-name'),
        fieldType: fieldGroup.getAttribute('data-field-type')
    };
}

export function getFieldScreenRect(clientX, clientY) {
    const el = document.elementFromPoint(clientX, clientY);
    if (!el) return null;

    const fieldGroup = el.closest('[data-field-name]');
    if (!fieldGroup) return null;

    const rect = fieldGroup.getBoundingClientRect();
    return {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height
    };
}
