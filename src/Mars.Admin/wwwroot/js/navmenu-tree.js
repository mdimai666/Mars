// Закрытие контекстного меню дерева навигации по клику вне меню и Escape.
// document-level listener, т.к. fixed-бэкдроп внутри acrylic-карточки
// (backdrop-filter создаёт containing block) не покрыл бы вьюпорт.
let ref = null;

function handler(e) {
    if (e.type === 'keydown' && e.key !== 'Escape') return;
    if (e.type === 'pointerdown' && e.target.closest('.nmt-menu, .nmt-row__tools')) return;
    const r = ref;
    unwatch();
    r?.invokeMethodAsync('CloseMenuFromJs');
}

export function watchClose(dotnet) {
    unwatch();
    ref = dotnet;
    document.addEventListener('pointerdown', handler, true);
    document.addEventListener('keydown', handler, true);
    // меню рисуется Blazor'ом после ответа интеропа — меряем на следующем кадре
    // и флипаем вверх, если вниз не помещается
    requestAnimationFrame(() => {
        const m = document.querySelector('.nmt-menu');
        if (!m) return;
        const r = m.getBoundingClientRect();
        if (r.bottom > window.innerHeight - 8) m.classList.add('nmt-menu--up');
    });
}

export function unwatch() {
    if (!ref) return;
    document.removeEventListener('pointerdown', handler, true);
    document.removeEventListener('keydown', handler, true);
    ref = null;
}
