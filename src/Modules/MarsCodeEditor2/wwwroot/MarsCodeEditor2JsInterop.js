
export function showPrompt(message) {
    return prompt(message, 'Type anything here');
}

function getEditorByBlazorMonacoId(blazorMonacoId) {
    return blazorMonaco.editors.find(s => s.id == blazorMonacoId).editor
}

export function f_editor_doaction(blazorMonacoId, action_id) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)
    let action = editor.getAction(action_id);
    action.run();
}

export function activateJSextensions(blazorMonacoId, optionsJson) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)

    if (!blazorMonaco.Mars_extensions_activated) {
        blazorMonaco.Mars_extensions_activated = true;
        emmetMonaco.emmetHTML(monaco, ['html', 'php', 'handlebars'])
        emmetMonaco.emmetCSS(monaco)
        if (monaco_plugin_init_log_lang) monaco_plugin_init_log_lang()
        else "'monaco_plugin_init_log_lang' not found";
    }

    // произвольные опции редактора из JSON (CodeEditor2.OptionsJson)
    if (optionsJson) {
        try {
            editor.updateOptions(JSON.parse(optionsJson))
        } catch (e) {
            console.error('invalid editor options json:', optionsJson, e)
        }
    }

    add_more_actions(editor)
}

function add_more_actions(editor) {
    editor.addAction({
        id: 'wordWrap',
        // A label of the action that will be presented to the user.
        label: 'Word Wrap',
        // An optional array of keybindings for the action.
        keybindings: [
            monaco.KeyMod.Alt | monaco.KeyCode.KeyZ,
        ],
        // A precondition for this action.
        precondition: null,
        // A rule to evaluate on top of the precondition in order to dispatch the keybindings.
        keybindingContext: null,
        contextMenuGroupId: 'navigation',
        contextMenuOrder: 1.5,

        // Method that will be executed when the action is triggered.
        // @@param editor The editor instance is passed in as a convinience
        run: (ed) => {
            // alert("i'm running => " + ed.getPosition());
            // return null;
            //let wordWrap = editor.getOption(115)
            let wordWrap = editor.getOption(monaco.editor.EditorOption.wordWrap)
            let newVal = wordWrap == "on" ? "off" : "on"
            //console.warn('editor.wordWrap=', wordWrap);
            editor.updateOptions({ wordWrap: newVal })

            //setTimeout(() => {
            //    debugger
            //}, 1000)

        }
    });

    // аналог "Change Language Mode" из VS Code: доступно через F1 (Command Palette)
    editor.addAction({
        id: 'mars.changeLanguageMode',
        label: 'Change Language Mode',
        keybindings: [],
        precondition: null,
        keybindingContext: null,
        run: (ed) => showLanguagePicker(ed)
    });
}

function showLanguagePicker(editor) {
    const model = editor.getModel()
    const dom = editor.getDomNode()
    if (!model || !dom) return

    const currentLang = model.getLanguageId()
    const languages = []
    for (const l of monaco.languages.getLanguages()) {
        if (languages.some(x => x.id === l.id)) continue
        languages.push({ id: l.id, label: (l.aliases && l.aliases[0]) || l.id })
    }
    languages.sort((a, b) => a.label.localeCompare(b.label))

    // цвета/размеры как у Command Palette текущей темы редактора
    const isDark = dom.classList.contains('vs-dark') || dom.classList.contains('hc-black')
    const C = isDark
        ? { bg: '#252526', fg: '#cccccc', border: '#454545', inputBg: '#3c3c3c', inputFg: '#cccccc', inputBorder: '#007fd4', hover: '#094771', selBg: '#0060c0', selFg: '#ffffff' }
        : { bg: '#f3f3f3', fg: '#333333', border: '#c8c8c8', inputBg: '#ffffff', inputFg: '#333333', inputBorder: '#007fd4', hover: '#e8e8e8', selBg: '#0060c0', selFg: '#ffffff' }

    const overlay = document.createElement('div')
    overlay.style.cssText = `position:absolute;top:0;left:50%;transform:translateX(-50%);` +
        `width:calc(100% - 16px);max-width:650px;z-index:100;` +
        `background:${C.bg};color:${C.fg};border:1px solid ${C.border};border-radius:0 0 4px 4px;` +
        `box-shadow:0 4px 12px rgba(0,0,0,.25);padding:6px;`

    const input = document.createElement('input')
    input.placeholder = 'Select Language Mode'
    input.style.cssText = `width:100%;box-sizing:border-box;background:${C.inputBg};color:${C.inputFg};` +
        `border:1px solid ${C.inputBorder};border-radius:2px;padding:4px 8px;outline:none;font-size:13px;`

    const list = document.createElement('div')
    list.style.cssText = 'max-height:280px;overflow:auto;margin-top:6px;'

    let items = []
    let selected = -1

    function render(filter) {
        list.innerHTML = ''
        items = []
        selected = -1
        const f = (filter || '').toLowerCase()
        for (const l of languages) {
            if (f && !l.label.toLowerCase().includes(f) && !l.id.toLowerCase().includes(f)) continue
            const el = document.createElement('div')
            el.textContent = l.label + (l.id === currentLang ? ' ✓' : '')
            el.style.cssText = 'padding:3px 10px;cursor:pointer;font-size:13px;line-height:22px;white-space:nowrap;border-radius:2px;'
            el.onmouseenter = () => { if (!items[selected] || items[selected].el !== el) el.style.background = C.hover }
            el.onmouseleave = () => { if (!items[selected] || items[selected].el !== el) el.style.background = '' }
            el.onclick = () => apply(l.id)
            list.appendChild(el)
            items.push({ id: l.id, el })
        }
        if (items.length) setSelected(0)
    }

    function setSelected(i) {
        if (i < 0) i = 0
        if (i >= items.length) i = items.length - 1
        if (items.length === 0) return
        if (selected >= 0 && items[selected]) {
            items[selected].el.style.background = ''
            items[selected].el.style.color = ''
        }
        selected = i
        const it = items[selected]
        it.el.style.background = C.selBg
        it.el.style.color = C.selFg
        it.el.scrollIntoView({ block: 'nearest' })
    }

    function apply(langId) {
        monaco.editor.setModelLanguage(model, langId)
        close()
        editor.focus()
    }

    function close() {
        document.removeEventListener('mousedown', onDocMouseDown, true)
        overlay.remove()
    }

    function onDocMouseDown(e) {
        if (!overlay.contains(e.target)) close()
    }

    input.oninput = () => render(input.value)
    input.onkeydown = (e) => {
        if (e.key === 'Escape') {
            e.preventDefault()
            e.stopPropagation()
            close()
        } else if (e.key === 'ArrowDown') {
            e.preventDefault()
            e.stopPropagation()
            setSelected(selected + 1)
        } else if (e.key === 'ArrowUp') {
            e.preventDefault()
            e.stopPropagation()
            setSelected(selected - 1)
        } else if (e.key === 'Enter') {
            e.preventDefault()
            e.stopPropagation()
            if (selected >= 0 && items[selected]) apply(items[selected].id)
        }
    }

    // колесо мыши над пикером скроллит редактор, а не список
    overlay.addEventListener('wheel', (e) => {
        e.preventDefault()
        editor.setScrollTop(editor.getScrollTop() + e.deltaY)
    }, { passive: false })

    overlay.appendChild(input)
    overlay.appendChild(list)
    dom.appendChild(overlay)
    document.addEventListener('mousedown', onDocMouseDown, true)
    render('')
    input.focus()
}

export function setModelLanguage(blazorMonacoId, lang) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)
    monaco.editor.setModelLanguage(editor.getModel(), lang);
}
