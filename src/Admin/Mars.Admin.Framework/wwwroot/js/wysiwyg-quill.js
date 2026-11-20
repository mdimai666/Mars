let loadPromise = null;

function versionParam() {
    return new URL(import.meta.url).searchParams.get('v');
}

function vendorUrl(name) {
    const url = new URL(`../vendor/quill/${name}`, import.meta.url);
    const v = versionParam();
    if (v) url.searchParams.set('v', v);
    return url.href;
}

// UMD-сборки (quill, blot-formatter) не создают глобал, если уже загружен
// monaco loader.js (появился window.define.amd) — AMD-ветка перехватывает
// регистрацию. Прячем define на время загрузки скрипта.
function injectScript(src) {
    return new Promise((resolve, reject) => {
        const savedDefine = window.define;
        if (savedDefine && savedDefine.amd) window.define = undefined;
        const restore = () => { window.define = savedDefine; };
        const s = document.createElement('script');
        s.src = src;
        s.onload = () => { restore(); resolve(); };
        s.onerror = () => { restore(); reject(new Error(`Failed to load ${src}`)); };
        document.head.appendChild(s);
    });
}

function injectCss(href) {
    if (document.querySelector(`link[href="${href}"]`)) return;
    const l = document.createElement('link');
    l.rel = 'stylesheet';
    l.href = href;
    document.head.appendChild(l);
}

export function ensureLoaded() {
    if (!loadPromise) {
        injectCss(vendorUrl('quill.snow.css'));
        loadPromise = injectScript(vendorUrl('quill.min.js'))
            .then(() => injectScript(vendorUrl('quill-blot-formatter.min.js')));
    }
    return loadPromise;
}

export async function init(editor, toolbar, optionsJson) {
    await ensureLoaded();
    if (editor.__quill) return;
    const Quill = window.Quill;
    Quill.register('modules/blotFormatter', window.QuillBlotFormatter.default);
    const userOptions = optionsJson ? JSON.parse(optionsJson) : {};
    editor.__quill = new Quill(editor, {
        debug: 'error',
        placeholder: 'Текст',
        theme: 'snow',
        ...userOptions,
        modules: { toolbar, blotFormatter: {}, ...(userOptions.modules || {}) },
    });
}

export function getHtml(editor) {
    return editor.__quill ? editor.__quill.root.innerHTML : null;
}

export function loadHtml(editor, html) {
    if (editor.__quill) editor.__quill.root.innerHTML = html ?? '';
}

export function insertImage(editor, url) {
    const quill = editor.__quill;
    if (!quill) return;
    const Delta = window.Quill.import('delta');
    const selection = quill.getSelection();
    const index = selection !== null ? selection.index : 0;
    quill.updateContents(new Delta().retain(index).insert({ image: url }, { alt: url }));
}
