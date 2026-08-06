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

// Клик/перетаскивание кнопки. Вся логика различения — здесь, в JS:
// если указатель не сдвинулся дальше порога — это клик (OnFabClick),
// иначе — перетаскивание (OnFabDragMove/OnFabDragEnd с финальной позицией).
// dotnetRef должен иметь методы: OnFabClick(), OnFabDragMove(x, y), OnFabDragEnd(x, y).
// Возвращает стартовый rect кнопки { x, y, w, h }.
let dragCleanup = null;
const DRAG_THRESHOLD = 5;

export function startFabDrag(dotnetRef, el, pointerId, clientX, clientY) {
    // защита от слушателей, оставшихся от незавершённого предыдущего жеста
    if (dragCleanup) {
        dragCleanup();
        dragCleanup = null;
    }

    const rect = el.getBoundingClientRect();
    const startX = rect.left;
    const startY = rect.top;
    const offsetX = clientX - startX;
    const offsetY = clientY - startY;
    let moved = false;
    let lastX = startX;
    let lastY = startY;

    function onMove(e) {
        if (e.pointerId !== pointerId) return;
        lastX = e.clientX - offsetX;
        lastY = e.clientY - offsetY;
        if (!moved && (Math.abs(lastX - startX) > DRAG_THRESHOLD || Math.abs(lastY - startY) > DRAG_THRESHOLD)) {
            moved = true;
        }
        if (moved) {
            dotnetRef.invokeMethodAsync('OnFabDragMove', lastX, lastY);
        }
    }

    function finish(e, cancelled) {
        if (e.pointerId !== pointerId) return;
        cleanup();
        if (cancelled) return;
        if (moved) {
            dotnetRef.invokeMethodAsync('OnFabDragEnd', lastX, lastY);
        } else {
            dotnetRef.invokeMethodAsync('OnFabClick');
        }
    }

    function onUp(e) { finish(e, false); }
    function onCancel(e) { finish(e, true); }

    function cleanup() {
        window.removeEventListener('pointermove', onMove);
        window.removeEventListener('pointerup', onUp);
        window.removeEventListener('pointercancel', onCancel);
        if (dragCleanup === cleanup) dragCleanup = null;
    }

    window.addEventListener('pointermove', onMove);
    window.addEventListener('pointerup', onUp);
    window.addEventListener('pointercancel', onCancel);
    dragCleanup = cleanup;

    return { x: startX, y: startY, w: rect.width, h: rect.height };
}

export function scrollToBottom(el) {
    if (el) el.scrollTop = el.scrollHeight;
}

export function focusElement(el) {
    if (el) el.focus();
}
