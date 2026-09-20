
export function showPrompt(message) {
    return prompt(message, 'Type anything here');
}

// Ссылки на .NET-объекты CodeEditor2 по id BlazorMonaco: JS-команды редактора
// (например CodeLens «выполнить» в .http) вызывают их без Blazor-обёртки.
const dotnetRefs = new Map();

function getEditorByBlazorMonacoId(blazorMonacoId) {
    return blazorMonaco.editors.find(s => s.id == blazorMonacoId).editor
}

export function f_editor_doaction(blazorMonacoId, action_id) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)
    let action = editor.getAction(action_id);
    action.run();
}

export function activateJSextensions(blazorMonacoId, optionsJson, dotNetRef) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)

    if (!blazorMonaco.Mars_extensions_activated) {
        blazorMonaco.Mars_extensions_activated = true;
        emmetMonaco.emmetHTML(monaco, ['html', 'php', 'handlebars'])
        emmetMonaco.emmetCSS(monaco)
        if (monaco_plugin_init_log_lang) monaco_plugin_init_log_lang()
        else "'monaco_plugin_init_log_lang' not found";
        registerHttpLanguage()
    }

    // произвольные опции редактора из JSON (CodeEditor2.OptionsJson)
    if (optionsJson) {
        try {
            editor.updateOptions(JSON.parse(optionsJson))
        } catch (e) {
            console.error('invalid editor options json:', optionsJson, e)
        }
    }

    if (dotNetRef) dotnetRefs.set(blazorMonacoId, dotNetRef)
    editor.__marsBlazorId = blazorMonacoId

    if (dotNetRef) {
        // События курсора и текста для .NET: связь формы с блоком под курсором в документе .http.
        // О смене строки сообщаем один раз, правку текста — с дебаунсом (печать не должна долбить .NET).
        let lastLine = -1;
        editor.onDidChangeCursorPosition(e => {
            const line = e.position.lineNumber;
            if (line === lastLine) return;
            lastLine = line;
            dotNetRef.invokeMethodAsync('CursorMovedToLine', line);
        });
        let contentTimer;
        editor.onDidChangeModelContent(() => {
            lastLine = -1;
            clearTimeout(contentTimer);
            contentTimer = setTimeout(() => dotNetRef.invokeMethodAsync('ContentChanged'), 400);
        });
    }

    add_more_actions(editor)
}

// Замена строк через executeEdits: в отличие от setValue сохраняет курсор, выделение и стек undo
// (синхронизация формы параметров с текстом блока .http).
export function replaceLines(blazorMonacoId, startLine, endLine, text) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)
    let model = editor.getModel()
    let end = Math.min(endLine, model.getLineCount())
    editor.executeEdits('mars-form-sync', [{
        range: new monaco.Range(startLine, 1, end, model.getLineLength(end) + 1),
        text: text,
    }])
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

    // команда CodeLens «выполнить» над строкой запроса в .http: ставим курсор на строку
    // и сообщаем .NET (CodeEditor2.OnRunRequest), какой запрос запустить
    editor.addAction({
        id: 'mars.http.run',
        label: 'Run HTTP request',
        keybindings: [],
        precondition: null,
        keybindingContext: null,
        run: (ed, line) => {
            const lineNumber = (typeof line === 'number' && line > 0) ? line : ed.getPosition().lineNumber
            ed.setPosition({ lineNumber: lineNumber, column: 1 })
            ed.revealLineInCenter(lineNumber)
            const ref = dotnetRefs.get(ed.__marsBlazorId)
            if (ref) ref.invokeMethodAsync('RunRequestAtLine', lineNumber)
        }
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

//=== язык .http (VS Code REST Client) =========================================
// В бандл Monaco язык http не входит, поэтому monarch-грамматика своя; за образец
// взята TextMate-грамматика humao.restclient (syntaxes/http.tmLanguage.json).

const httpMethods = 'get|post|put|delete|patch|head|options|connect|trace|lock|unlock|propfind|proppatch|copy|move|mkcol|mkcalendar|acl|search';

// Строка запроса: метод и URL-подобный аргумент (со слэшем, {{переменной}} или схемой) —
// без такой оговорки строка тела «delete the item» тоже сошла бы за запрос.
const httpRequestLine = new RegExp('^\\s*(' + httpMethods + ')(\\s+)(?=(?:\\S*[/]|\\{\\{|\\w+://))', 'i');

function registerHttpLanguage() {
    if (monaco.languages.getLanguages().some(l => l.id === 'http')) return;

    monaco.languages.register({ id: 'http', aliases: ['HTTP', 'http'], extensions: ['.http', '.rest'] });

    monaco.languages.setLanguageConfiguration('http', {
        comments: { lineComment: '#' },
        brackets: [['{', '}'], ['[', ']'], ['(', ')']],
        autoClosingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"' },
            { open: "'", close: "'" },
        ],
        surroundingPairs: [
            { open: '{', close: '}' },
            { open: '[', close: ']' },
            { open: '(', close: ')' },
            { open: '"', close: '"' },
            { open: "'", close: "'" },
        ],
    });

    monaco.languages.setMonarchTokensProvider('http', {
        ignoreCase: true,
        defaultToken: '',
        tokenPostfix: '.http',
        tokenizer: {
            root: [
                // # @name posts — имя запроса: директива внутри комментария.
                // Литеральный @ экранирован классом: голый «@name» monarch принял бы
                // за ссылку на атрибут определения языка и упал при компиляции.
                [/^\s*(#+\s+[@]name\s+)(\S+)\s*$/, ['comment', 'metatag']],
                [/^\s*(?:#+|\/\/+).*$/, 'comment'],
                // переменные документа: @host = https://example.org
                [/^\s*(@)([^\s=]+)(\s*=\s*)(.*?)\s*$/, ['keyword', 'variable', 'delimiter', 'string']],
                // строка запроса: метод + URL [+ HTTP/версия]
                [/^\s*(GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS|CONNECT|TRACE|LOCK|UNLOCK|PROPFIND|PROPPATCH|COPY|MOVE|MKCOL|MKCALENDAR|ACL|SEARCH)(\s+)(\S.*?)(\s+)(HTTP\/[\d.]+)\s*$/,
                    ['keyword.control', 'white', 'constant.language', 'white', 'keyword.other']],
                [/^\s*(GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS|CONNECT|TRACE|LOCK|UNLOCK|PROPFIND|PROPPATCH|COPY|MOVE|MKCOL|MKCALENDAR|ACL|SEARCH)(\s+)(\S.*?)\s*$/,
                    ['keyword.control', 'white', 'constant.language']],
                // query-параметры, перенесённые на отдельные строки: ?a=b / &a=b
                [/^\s*([?&])([^=\s]+)(=)(.*)$/, ['keyword.operator', 'variable', 'delimiter', 'string']],
                // заголовки: имя, двоеточие, значение до конца строки
                [/^([\w-]+)(\s*:\s*)(.*?)\s*$/, ['tag', 'delimiter', 'string']],
                // тело: переменные {{…}}, строки, литералы и числа JSON
                [/\{\{[^}]*\}\}/, 'variable.predefined'],
                [/"[^"]*"/, 'string'],
                [/\b(?:true|false|null)\b/, 'constant.language'],
                [/\b\d+(?:\.\d+)?\b/, 'number'],
            ],
        },
    });

    // CodeLens «выполнить» над каждой строкой запроса — аналог Send Request из REST Client.
    // editor.addAction регистрирует команду в глобальном реестре с префиксом экземпляра
    // («<editorId>:mars.http.run»), поэтому id команды собираем по редактору модели.
    monaco.languages.registerCodeLensProvider('http', {
        provideCodeLenses(model) {
            const lenses = [];
            const editor = monaco.editor.getEditors().find(e => e.getModel() === model);
            if (!editor) return { lenses, dispose() { } };
            const commandId = editor.getId() + ':mars.http.run';
            for (let line = 1; line <= model.getLineCount(); line++) {
                if (!httpRequestLine.test(model.getLineContent(line))) continue;
                lenses.push({
                    range: { startLineNumber: line, startColumn: 1, endLineNumber: line, endColumn: 1 },
                    command: { id: commandId, title: '\u25b6 выполнить', arguments: [line] },
                });
            }
            return { lenses, dispose() { } };
        },
        resolveCodeLens(model, lens) { return lens; },
    });

    // Модели, созданные с languageId http до регистрации языка, перетокенизировать
    for (const model of monaco.editor.getModels()) {
        if (model.getLanguageId() === 'http') monaco.editor.setModelLanguage(model, 'http');
    }
}
