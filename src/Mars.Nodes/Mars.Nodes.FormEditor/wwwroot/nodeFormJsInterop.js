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
