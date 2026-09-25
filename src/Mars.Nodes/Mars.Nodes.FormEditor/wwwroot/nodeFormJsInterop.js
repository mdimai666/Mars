// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}
    
export function ShowOffcanvas(htmlId, open) {

    var myOffcanvas = document.getElementById(htmlId)

    var bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(myOffcanvas)

    if (open) {
        bsOffcanvas.show();
    } else {
        bsOffcanvas.hide();
    }
}

export function Offcanves_events_subscribe_hide(selector, dotNetHelper, methodName) {
    var myOffcanvas = document.querySelector(selector)

    myOffcanvas.addEventListener('hide.bs.offcanvas', function () {
        // do something...
        //console.warn(">hide.bs.offcanvas: " + methodName)

        dotNetHelper.invokeMethodAsync(methodName);

    })
}


export function HtmlGetElementScroll(selector) {
    let e = document.querySelector(selector)

    return { x: e.scrollLeft, y: e.scrollTop }
}

document.addEventListener("DOMContentLoaded", function () {
    
    document.addEventListener('keydown', f_on_tabpress_write_tab_onkeydown1);
});

/** @param {KeyboardEvent} e */
function f_on_tabpress_write_tab_onkeydown1(e) {

    if (!e.target.classList.contains('f-on-tabpress-write-tab')) return;

    if (e.key == 'Tab') {
        e.preventDefault();
        //var start = this.selectionStart;
        //var end = this.selectionEnd;

        //// set textarea value to: text before caret + tab + text after caret
        //this.value = this.value.substring(0, start) +
        //    "\t" + this.value.substring(end);

        //// put caret at right position again
        //this.selectionStart =
        //    this.selectionEnd = start + 1;

        if (!document.execCommand('insertText', false, '\t')) {
            this.setRangeText('\t');
        }
    }
    else if (e.ctrlKey && e.code == 'KeyS') {
        // Prevent the Save dialog to open
        console.log('CTRL + S');
        //e.stopPropagation()
        //e.preventDefault();

        //console.warn('e', e)

        let e2 = new KeyboardEvent(e.type, e)

        let NodeEditor1 = document.querySelector('.NodeEditor1')

        NodeEditor1.dispatchEvent(e2)

        return false
    }
}

export function f_editor_doaction(action_id) {
    let editor = monaco.editor.getEditors()[0].getAction(action_id);
    editor.run();
}

export function mvi_getSelection(el) {
    return { start: el?.selectionStart ?? 0, end: el?.selectionEnd ?? 0 };
}

export function mvi_setCaret(el, pos) {
    if (!(el instanceof HTMLElement)) return;

    el.focus();
    let p = Math.max(0, Math.min(pos, el.value?.length ?? 0));
    el.setSelectionRange(p, p);
}

function mvi_bindScroll(input) {
    if (!(input instanceof HTMLElement) || input.__mviScroll) return;

    const handler = () => {
        const highlight = input.parentElement?.querySelector('.mvi-highlight');
        if (highlight) highlight.scrollLeft = input.scrollLeft;
    };

    input.__mviScroll = handler;
    input.addEventListener('scroll', handler);
}

export function mvi_bind(root, input, dotNetRef, outsideMethod, pasteMethod) {
    mvi_outsideClick(root, dotNetRef, outsideMethod);
    mvi_onPaste(input, dotNetRef, pasteMethod);
    mvi_bindScroll(input);
}

const mviAnchors = new Map();

function mviPlace(anchor) {
    const entry = mviAnchors.get(anchor);
    if (!entry) return;

    const { popup, align } = entry;
    if (!popup.isConnected) {
        mviForget(anchor);
        return;
    }

    if (!popup.matches(':popover-open')) return;

    const a = anchor.getBoundingClientRect();
    const p = popup.getBoundingClientRect();
    const margin = 5;

    let top = a.bottom + margin;
    if (top + p.height > window.innerHeight - 4)
        top = Math.max(4, a.top - p.height - margin);

    let left = align === 'right' ? a.right - p.width : a.left;
    if (left + p.width > window.innerWidth - 4)
        left = window.innerWidth - 4 - p.width;
    if (left < 4)
        left = 4;

    popup.style.top = `${Math.round(top)}px`;
    popup.style.left = `${Math.round(left)}px`;
}

function mviForget(anchor) {
    const entry = mviAnchors.get(anchor);
    if (!entry) return;

    entry.observer?.disconnect();
    mviAnchors.delete(anchor);
}

export function mvi_popupOpen(popup, anchor, align) {
    if (!(popup instanceof HTMLElement) || !(anchor instanceof HTMLElement)) return;

    if (!popup.matches(':popover-open')) popup.showPopover();

    mviForget(anchor);

    const observer = new ResizeObserver(() => mviPlace(anchor));
    observer.observe(popup);

    mviAnchors.set(anchor, { popup, align, observer });
    mviPlace(anchor);
}

function mvi_outsideClick(root, dotNetRef, method) {
    if (!root || root.__mviOutside) return;

    const handler = (e) => {
        if (root.contains(e.target)) return;

        if (!root.querySelector('[popover]:popover-open')) return;

        dotNetRef.invokeMethodAsync(method);
    };

    root.__mviOutside = handler;
    document.addEventListener('pointerdown', handler, true);
}

function mvi_onPaste(el, dotNetRef, method) {
    if (!el || el.__mviPaste) return;

    const handler = (e) => {
        const text = (e.clipboardData || window.clipboardData)?.getData('text') ?? '';

        e.preventDefault();
        dotNetRef.invokeMethodAsync(method, text);
    };

    el.__mviPaste = handler;
    el.addEventListener('paste', handler);
}

export function mvi_dispose(root, input, anchor) {
    if (root?.__mviOutside) {
        document.removeEventListener('pointerdown', root.__mviOutside, true);
        root.__mviOutside = null;
    }

    if (input?.__mviPaste) {
        input.removeEventListener('paste', input.__mviPaste);
        input.__mviPaste = null;
    }

    if (input?.__mviScroll) {
        input.removeEventListener('scroll', input.__mviScroll);
        input.__mviScroll = null;
    }

    if (anchor) mviForget(anchor);
}

document.addEventListener('scroll', () => mviAnchors.forEach((_, anchor) => mviPlace(anchor)), true);
window.addEventListener('resize', () => mviAnchors.forEach((_, anchor) => mviPlace(anchor)));
