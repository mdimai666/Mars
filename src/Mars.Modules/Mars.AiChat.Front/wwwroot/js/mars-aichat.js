// Mars AiChat — поддержка плавающей кнопки и терминала
const POS_KEY = 'mars.aichat.fab.pos';
const FAB_MARGIN = 12;

export function getFabPos() {
    try {
        const raw = localStorage.getItem(POS_KEY);
        if (raw) return JSON.parse(raw);
    } catch { /* ignore */ }
    return null;
}

export function saveFabPos(x, y) {
    try {
        localStorage.setItem(POS_KEY, JSON.stringify({ x, y }));
    } catch { /* ignore */ }
}

export function getViewport() {
    return { w: window.innerWidth, h: window.innerHeight };
}

export function getFabRect(el) {
    if (!el) return { x: 0, y: 0, w: 120, h: 40 };
    const r = el.getBoundingClientRect();
    return { x: r.left, y: r.top, w: r.width, h: r.height };
}

// Перетаскивание кнопки: события вешаем на window, чтобы не терять указатель.
// dotnetRef должен иметь методы OnFabDragMove(x, y) и OnFabDragEnd(x, y).
export function startFabDrag(dotnetRef, pointerId, offsetX, offsetY) {
    let lastX = 0, lastY = 0;

    function onMove(e) {
        if (e.pointerId !== pointerId) return;
        lastX = e.clientX - offsetX;
        lastY = e.clientY - offsetY;
        dotnetRef.invokeMethodAsync('OnFabDragMove', lastX, lastY);
    }

    function onUp(e) {
        if (e.pointerId !== pointerId) return;
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerup', onUp);
        window.removeEventListener('pointercancel', onUp);
        dotnetRef.invokeMethodAsync('OnFabDragEnd', lastX, lastY);
    }

    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
    window.addEventListener('pointercancel', onUp);
}

export function scrollToBottom(el) {
    if (el) el.scrollTop = el.scrollHeight;
}

export function focusElement(el) {
    if (el) el.focus();
}
