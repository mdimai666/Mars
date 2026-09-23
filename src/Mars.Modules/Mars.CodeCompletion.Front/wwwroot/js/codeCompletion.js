let dotNetRef = null;
const watched = new Map();

export function init(ref) {
    dotNetRef = ref;
    registerSignatureHelp();
}

function registerSignatureHelp() {
    monaco.languages.registerSignatureHelpProvider('csharp', {
        signatureHelpTriggerCharacters: ['('],
        signatureHelpRetriggerCharacters: [','],
        provideSignatureHelp: async (model, position) => {
            if (!dotNetRef) return null;
            const result = await dotNetRef.invokeMethodAsync('ProvideSignatureHelp', decodeURI(model.uri.toString()), position);
            if (!result || !result.signatures || result.signatures.length === 0) return null;
            const signatures = result.signatures.map(s => ({
                label: { label: s.label, description: s.documentation ?? '' },
                documentation: s.documentation ?? '',
                parameters: (s.parameters ?? []).map(p => ({ label: p.label, documentation: p.documentation ?? '' })),
            }));
            return {
                value: {
                    signatures,
                    activeSignature: result.activeSignature ?? 0,
                    activeParameter: result.activeParameter ?? 0,
                },
                dispose: () => { },
            };
        },
    });
}

function getModel(uri) {
    return monaco.editor.getModel(monaco.Uri.parse(uri));
}

export function getModelSnapshot(uri, position) {
    const model = getModel(uri);
    if (!model) return null;
    const code = model.getValue();
    const offset = position ? model.getOffsetAt(position) : code.length;
    return { code, offset };
}

export function watchModel(uri) {
    const model = getModel(uri);
    if (!model) return;
    unwatchModel(uri);

    let timer = null;
    const changeSub = model.onDidChangeContent(() => {
        if (timer) clearTimeout(timer);
        timer = setTimeout(() => dotNetRef?.invokeMethodAsync('RunDiagnostics', uri), 500);
    });
    const disposeSub = model.onWillDispose(() => {
        unwatchModel(uri);
        dotNetRef?.invokeMethodAsync('RemoveDocument', uri);
    });
    watched.set(uri, { changeSub, disposeSub, getTimer: () => timer });
}

export function unwatchModel(uri) {
    const w = watched.get(uri);
    if (!w) return;
    const timer = w.getTimer();
    if (timer) clearTimeout(timer);
    w.changeSub.dispose();
    w.disposeSub.dispose();
    watched.delete(uri);
}

export function setMarkers(uri, diagnostics) {
    const model = getModel(uri);
    if (!model) return;
    const markers = (diagnostics ?? []).map(d => {
        const start = model.getPositionAt(d.offsetFrom);
        const end = model.getPositionAt(d.offsetTo);
        return {
            severity: d.severity,
            message: d.message,
            code: d.id ?? '',
            startLineNumber: start.lineNumber,
            startColumn: start.column,
            endLineNumber: end.lineNumber,
            endColumn: end.column,
        };
    });
    monaco.editor.setModelMarkers(model, 'mars-code-completion', markers);
}

export function clearMarkers(uri) {
    const model = getModel(uri);
    if (model) monaco.editor.setModelMarkers(model, 'mars-code-completion', []);
}
