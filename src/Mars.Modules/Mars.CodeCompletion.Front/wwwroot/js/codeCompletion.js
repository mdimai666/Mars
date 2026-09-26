let dotNetRef = null;
const watched = new Map();

// ДЕРЖАТЬ В СИНХРОНЕ с SemanticTokensQueryService.TokenTypes (порядок = индексы tokenType)
const semanticTokenTypes = [
    'namespace', 'class', 'enum', 'interface', 'struct', 'typeParameter',
    'parameter', 'variable', 'property', 'enumMember', 'event', 'method',
    'keyword', 'string', 'comment', 'number',
];
const semanticTokensByUri = new Map();
let semanticTokensEmitter = null;

export function init(ref) {
    dotNetRef = ref;
    registerSignatureHelp();
    registerSemanticTokens();
    registerTheme();
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

function registerSemanticTokens() {
    semanticTokensEmitter = new monaco.Emitter();
    monaco.languages.registerDocumentSemanticTokensProvider('csharp', {
        getLegend: () => ({ tokenTypes: semanticTokenTypes, tokenModifiers: [] }),
        provideDocumentSemanticTokens: (model) => {
            const data = semanticTokensByUri.get(decodeURI(model.uri.toString()));
            if (!data || data.length === 0) return null;
            return { data: new Uint32Array(data), resultId: null };
        },
        releaseDocumentSemanticTokens: () => { },
        onDidChangeSemanticTokens: semanticTokensEmitter.event,
    });
}

// Светлая палитра semantic-токенов — VS Code light_plus (theme-defaults/themes/light_plus.json):
// типы #267f99, методы #795e26, переменные/параметры #001080, константы/enumMember #0070c1,
// keyword #0000ff, string #a31515, comment #008000, number #098658.
function registerTheme() {
    monaco.editor.defineTheme('mars-light', {
        base: 'vs',
        inherit: true,
        rules: [
            { token: 'namespace', foreground: '267f99' },
            { token: 'class', foreground: '267f99' },
            { token: 'enum', foreground: '267f99' },
            { token: 'interface', foreground: '267f99' },
            { token: 'struct', foreground: '267f99' },
            { token: 'typeParameter', foreground: '267f99' },
            { token: 'method', foreground: '795e26' },
            { token: 'property', foreground: '001080' },
            { token: 'variable', foreground: '001080' },
            { token: 'parameter', foreground: '001080' },
            { token: 'event', foreground: '001080' },
            { token: 'enumMember', foreground: '0070c1' },
            { token: 'keyword', foreground: '0000ff' },
            { token: 'string', foreground: 'a31515' },
            { token: 'comment', foreground: '008000' },
            { token: 'number', foreground: '098658' },
        ],
        colors: {},
    });
    monaco.editor.setTheme('mars-light');
}

export function setSemanticTokens(uri, data) {
    semanticTokensByUri.set(uri, data ?? []);
    semanticTokensEmitter?.fire();
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
        timer = setTimeout(() => dotNetRef?.invokeMethodAsync('RunAnalyze', uri), 500);
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
    semanticTokensByUri.delete(uri);
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
