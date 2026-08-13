// Mars AiChat — поддержка плавающей кнопки и терминала
const POS_KEY = 'mars.aichat.fab.pos';
const FAB_MARGIN = 12;

// Формат позиции: { edge: 'left'|'right'|'top'|'bottom', pos: 0..1 } —
// край + доля вдоль него. Пиксели не сохраняются: при ресайзе окна кнопка
// остаётся у выбранного края (CSS calc делает это без JS).
export function getFabPos() {
    try {
        const raw = localStorage.getItem(POS_KEY);
        if (!raw) return null;
        const v = JSON.parse(raw);
        if (v && typeof v.edge === 'string' && typeof v.pos === 'number') {
            return { edge: v.edge, pos: clamp01(v.pos) };
        }
        // старый формат {x, y}: прижимаем к ближайшему краю
        if (v && typeof v.x === 'number' && typeof v.y === 'number') {
            return migrateXyPos(v.x, v.y);
        }
    } catch { /* ignore */ }
    return null;
}

function migrateXyPos(x, y) {
    const w = window.innerWidth, h = window.innerHeight;
    const BW = 120, BH = 40; // дефолтные размеры кнопки (до первого перетаскивания)
    const dLeft = x, dRight = w - x - BW, dTop = y, dBottom = h - y - BH;
    const min = Math.min(dLeft, dRight, dTop, dBottom);
    if (min === dLeft || min === dRight) {
        return { edge: min === dLeft ? 'left' : 'right', pos: clamp01(y / Math.max(1, h - BH)) };
    }
    return { edge: min === dTop ? 'top' : 'bottom', pos: clamp01(x / Math.max(1, w - BW)) };
}

function clamp01(v) {
    return Math.min(1, Math.max(0, v));
}

export function saveFabPos(edge, pos) {
    try {
        localStorage.setItem(POS_KEY, JSON.stringify({ edge, pos }));
    } catch { /* ignore */ }
}

export function getViewport() {
    return { w: window.innerWidth, h: window.innerHeight };
}

let dragCleanup = null;
const DRAG_THRESHOLD = 5;

// Общий каркас жеста перетаскивания: порог различения клика и сдвига,
// слушатели на window, защита от незавершённого предыдущего жеста.
// Возвращает стартовый rect элемента { x, y, w, h }.
function beginDrag(el, pointerId, clientX, clientY, handlers) {
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
            handlers.onMove(lastX, lastY);
        }
    }

    function finish(e, cancelled) {
        if (e.pointerId !== pointerId) return;
        cleanup();
        if (cancelled) return;
        handlers.onEnd(moved, lastX, lastY);
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

// Клик/перетаскивание кнопки: если указатель не сдвинулся дальше порога —
// это клик (OnFabClick), иначе — OnFabDragMove/OnFabDragEnd с финальной позицией.
export function startFabDrag(dotnetRef, el, pointerId, clientX, clientY) {
    return beginDrag(el, pointerId, clientX, clientY, {
        onMove: (x, y) => dotnetRef.invokeMethodAsync('OnFabDragMove', x, y),
        onEnd: (moved, x, y) => moved
            ? dotnetRef.invokeMethodAsync('OnFabDragEnd', x, y)
            : dotnetRef.invokeMethodAsync('OnFabClick'),
    });
}

// Перетаскивание окна терминала за шапку (OnTermDragMove/OnTermDragEnd).
// Клика тут нет; окно не даём уехать за экран — видимой остаётся хотя бы шапка.
export function startTermDrag(dotnetRef, el, pointerId, clientX, clientY) {
    const rect = el.getBoundingClientRect();
    const clampPos = (x, y) => ({
        x: Math.min(Math.max(x, 80 - rect.width), window.innerWidth - 80),
        y: Math.min(Math.max(y, 0), window.innerHeight - 40),
    });

    return beginDrag(el, pointerId, clientX, clientY, {
        onMove: (x, y) => {
            const p = clampPos(x, y);
            dotnetRef.invokeMethodAsync('OnTermDragMove', p.x, p.y);
        },
        onEnd: (moved, x, y) => {
            if (!moved) return;
            const p = clampPos(x, y);
            dotnetRef.invokeMethodAsync('OnTermDragEnd', p.x, p.y);
        },
    });
}

// Нативный resize держит размер в inline-style элемента — Blazor его не видит
// и при перерисовке (или повторном открытии) размер терялся бы. Наблюдатель
// сообщает фактический размер в C#. Первый срабатывание observe() — текущий
// (дефолтный) размер, его не передаём, чтобы не фиксировать дефолт в пикселях.
let termResizeObserver = null;
export function observeTermResize(dotnetRef, el) {
    if (termResizeObserver) {
        termResizeObserver.disconnect();
        termResizeObserver = null;
    }

    let first = true;
    const ro = new ResizeObserver(() => {
        if (first) {
            first = false;
            return;
        }
        const r = el.getBoundingClientRect();
        dotnetRef.invokeMethodAsync('OnTermResize', r.width, r.height);
    });
    ro.observe(el);
    termResizeObserver = ro;
}

// Ресайз окна браузера: терминал позиционируется в пикселях и после уменьшения
// страницы мог остаться за экраном — сообщаем новый вьюпорт, C# вернёт окно
// в видимую область.
let viewportResizeHandler = null;
export function observeViewportResize(dotnetRef) {
    if (viewportResizeHandler) {
        window.removeEventListener('resize', viewportResizeHandler);
    }
    viewportResizeHandler = () => {
        dotnetRef.invokeMethodAsync('OnViewportResize', window.innerWidth, window.innerHeight);
    };
    window.addEventListener('resize', viewportResizeHandler);
}

export function scrollToBottom(el) {
    if (el) el.scrollTop = el.scrollHeight;
}

export function focusElement(el) {
    if (el) el.focus();
}

export function clickElement(el) {
    if (el) el.click();
}

// Вставка файлов из буфера в поле ввода: Blazor не отдаёт файлы из paste-события,
// поэтому читаем их здесь и шлём в .NET base64-ом (OnClipboardFile).
// Текст без файлов не перехватываем — обычная вставка текста работает как раньше.
export function watchPaste(dotnetRef, el) {
    if (!el) return;
    el.addEventListener('paste', (e) => {
        const files = e.clipboardData && e.clipboardData.files;
        if (!files || files.length === 0) return;
        e.preventDefault();
        for (const file of files) {
            const reader = new FileReader();
            reader.onload = () => {
                const base64 = String(reader.result).split(',')[1] || '';
                dotnetRef.invokeMethodAsync('OnClipboardFile', file.name || null, file.type || null, base64);
            };
            reader.readAsDataURL(file);
        }
    });
}
