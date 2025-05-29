
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

export function activateJSextensions(blazorMonacoId) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)

    if (!blazorMonaco.Mars_extensions_activated) {
        blazorMonaco.Mars_extensions_activated = true;
        emmetMonaco.emmetHTML(monaco, ['html', 'php', 'handlebars'])
        emmetMonaco.emmetCSS(monaco)
        if (monaco_plugin_init_log_lang) monaco_plugin_init_log_lang()
        else "'monaco_plugin_init_log_lang' not found";
    }

    editor.updateOptions({ wordWrap: "on" })

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
}

export function setModelLanguage(blazorMonacoId, lang) {
    let editor = getEditorByBlazorMonacoId(blazorMonacoId)
    monaco.editor.setModelLanguage(editor.getModel(), lang);
}
