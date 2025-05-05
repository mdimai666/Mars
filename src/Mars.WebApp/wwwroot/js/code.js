// good info in - https://mono.software/2017/04/11/custom-intellisense-with-monaco-editor/
function getAreaInfo(text) {
    // opening for strings, comments and CDATA
    var items = ['"', '\'', '<!--', '<![CDATA['];
    var isCompletionAvailable = true;
    // remove all comments, strings and CDATA
    text = text.replace(/"([^"\\]*(\\.[^"\\]*)*)"|\'([^\'\\]*(\\.[^\'\\]*)*)\'|<!--([\s\S])*?-->|<!\[CDATA\[(.*?)\]\]>/g, '');
    for (var i = 0; i < items.length; i++) {
        var itemIdx = text.indexOf(items[i]);
        if (itemIdx > -1) {
            // we are inside one of unavailable areas, so we remove that area
            // from our clear text
            text = text.substring(0, itemIdx);
            // and the completion is not available
            isCompletionAvailable = false;
        }
    }
    return {
        isCompletionAvailable: isCompletionAvailable,
        clearedText: text
    };
}

function getLastOpenedTag(text) {
    // get all tags inside of the content
    var tags = text.match(/<\/*(?=\S*)([a-zA-Z-]+)/g);
    if (!tags) {
        return undefined;
    }
    // we need to know which tags are closed
    var closingTags = [];
    for (var i = tags.length - 1; i >= 0; i--) {
        if (tags[i].indexOf('</') === 0) {
            closingTags.push(tags[i].substring('</'.length));
        }
        else {
            // get the last position of the tag
            var tagPosition = text.lastIndexOf(tags[i]);
            var tag = tags[i].substring('<'.length);
            var closingBracketIdx = text.indexOf('/>', tagPosition);
            // if the tag wasn't closed
            if (closingBracketIdx === -1) {
                // if there are no closing tags or the current tag wasn't closed
                //debugger;
                if (!closingTags.length || closingTags[closingTags.length - 1] !== tag) {
                    // we found our tag, but let's get the information if we are looking for
                    // a child element or an attribute
                    text = text.substring(tagPosition);
                    return {
                        tagName: tag,
                        isAttributeSearch: text.indexOf('<') > text.indexOf('>')
                    };
                }
                // remove the last closed tag
                closingTags.splice(closingTags.length - 1, 1);
            }
            // remove the last checked tag and continue processing the rest of the content
            text = text.substring(0, tagPosition);
        }
    }
}


function my_monaco_init__attrs() {

    monaco.languages.registerCompletionItemProvider('html', {
        provideCompletionItems: function (model, position) {
            // find out if we are completing a property in the 'dependencies' object.
            var textUntilPosition = model.getValueInRange({ startLineNumber: 1, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column });
            // var match = textUntilPosition.match(/"dependencies"\s*:\s*\{\s*("[^"]*"\s*:\s*"[^"]*"\s*,\s*)*([^"]*)?$/);


            // let textUntilPosition = getTextBeforePointer();
            let info = getAreaInfo(textUntilPosition); // { clearedText: "<div class=>[.....]	<div ", isCompletionAvailable: true }
            var lastTag = getLastOpenedTag(info.clearedText);//lastTag> {tagName: "div", isAttributeSearch: true}
            // console.warn('info>', info);
            // console.warn('lastTag>', lastTag);
            //debugger;
            if (!lastTag) return;
            
            let isAttributeSearch = lastTag.isAttributeSearch

            var match = true; //lastTag.isAttributeSearch == false
            if (!match) {
                return { suggestions: [] };
            }
            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn
            };

            if (isAttributeSearch) {
                return {
                    suggestions: createDependencyProposals__attributes(range, { ...lastTag }) //attributes
                };
            }

            return {
                suggestions: createDependencyProposals(range)
            };
        }
    });
}

function FindComponentsProps() {
    let list = {};
    let names = [];

    // let proto = Object.getPrototypeOf(App.$root.$options.components);
    let proto = (App.$root.$options.components);
    const MAX_DEPTH = 20;
    let depth = 0;
    while (Object.getPrototypeOf(proto) && depth <= MAX_DEPTH) {
        proto = Object.getPrototypeOf(proto);
        depth++;

        let components = Object.keys(proto);

        if ("QImg" in proto) {
            console.dir(proto['QImg']);
        }

        names.push(...components)
        // Object.assign(list, {})
        for (let name of components) {
            /** @type {Vue.Component} */
            let v = proto[name];

            list[name] = {
                name: name,
                props: v && Object.keys(v.options && v.options.props || {}) || [],
                component: v,
                tag: name.replace(/(\w)([A-Z])(\w+?)/g, '$1-$2$3').toLowerCase()

            }

            if (name == 'QImg') {
                // console.warn(list[name]);
            }
        }
    }

    return list;

}
function FindComponentsProps2() {
    let aa1 = Object.getPrototypeOf(App.$root.$options.components);
    let aa2 = Object.getPrototypeOf(Object.getPrototypeOf(App.$root.$options.components));
    let aa3 = Object.getPrototypeOf(Object.getPrototypeOf(Object.getPrototypeOf(App.$root.$options.components)));
    // this.comList = aa.__proto__.concat(aa.__proto__.__proto__);

    let bb1 = Object.keys(aa1);
    let bb2 = Object.keys(aa2);
    let bb3 = Object.keys(aa3);

    let _comp = {};
    bb1.map(s => _comp[s] = aa1[s]);
    bb2.map(s => _comp[s] = aa2[s]);
    bb3.map(s => _comp[s] = aa3[s]);

    // this.comListData = _comp;

    let black_list = ['RouterView', 'RouterLink', 'Sidebar1', "BuilderNode", "BuilderBox", "DynamicComponent", 'vue-dropzone', 'DPanel']
        .concat('DashItem', 'DashLayout', 'tags-input', 'vue-draggable-resizable')
        .concat('Transition', 'TransitionGroup')

    // console.warn(black_list)

    let comList = bb1.concat(bb2).concat(bb3)
        .filter(s => !black_list.includes(s)).slice(0, 116);

    return comList
}

//window.ww = FindComponentsProps


function my_monaco_init__less() {
    monaco.languages.registerCompletionItemProvider('less', {
        provideCompletionItems: function (model, position) {
            // find out if we are completing a property in the 'dependencies' object.
            var textUntilPosition = model.getValueInRange({ startLineNumber: 1, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column });
            // var match = textUntilPosition.match(/"dependencies"\s*:\s*\{\s*("[^"]*"\s*:\s*"[^"]*"\s*,\s*)*([^"]*)?$/);


            // let textUntilPosition = getTextBeforePointer();
            // let info = getAreaInfo(textUntilPosition); // { clearedText: "<div class=>[.....]	<div ", isCompletionAvailable: true }
            // var lastTag = getLastOpenedTag(info.clearedText);//lastTag> {tagName: "div", isAttributeSearch: true}
            // console.warn('info>', info);
            // console.warn('lastTag>', lastTag);

            // let isAttributeSearch = lastTag.isAttributeSearch

            var match = true; //lastTag.isAttributeSearch == false
            if (!match) {
                return { suggestions: [] };
            }
            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn
            };

            return {
                suggestions: createDependencyProposals__less(range)
            };
        }
    });
}

function createDependencyProposals__less(range) {

    // let coms = window.$_FindComponentsProps;

    // console.warn('>>>', tagName);

    // let tagComponent = Object.values(coms).find(s => s.tag == tagName.toLowerCase());

    // console.warn(tagComponent);

    // let snippets = Object.values(tagComponent.props).map(s => {


    //   /** @type {import('monaco-editor').languages.CompletionItem} */
    //   let snip = {
    //     label: s,
    //     kind: monaco.languages.CompletionItemKind.Property,
    //     // documentation: vue_snippets[s].description,
    //     insertText: `${s}="$1"$2`,
    //     range: range,
    //     insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    //     sortText: '_'+s
    //   };
    //   return snip;
    // });


    let less = less_option.globalVars;
    /** @type {import('monaco-editor').languages.CompletionItem[]} */
    let list = Object.keys(less).map(s => {
        /** @type {import('monaco-editor').languages.CompletionItem} */
        let z = {
            label: '@' + s,
            kind: monaco.languages.CompletionItemKind.Value,
            // documentation: less[s],
            insertText: '@' + s,
            range: range,
            detail: less[s],
            // insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet
        }
        return z
    });

    list = list.concat([
        {
            label: '@_resonsive',
            kind: monaco.languages.CompletionItemKind.Value,
            // documentation: "Vue model bind",
            detail: `@media @mobiles... @desktop...`,
            insertText: `@media @mobiles {\n\t$1\n}\n\n@media @desktop {\n\t$2\n}\n`,
            range: range,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet

        },
    ]);

    return list;
};

function my_monaco_init__js2() {
    monaco.languages.registerCompletionItemProvider('javascript', {
        provideCompletionItems: function (model, position) {
            // find out if we are completing a property in the 'dependencies' object.
            // var textUntilPosition = model.getValueInRange({ startLineNumber: 1, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column });

            let line = model.getLineContent(position.lineNumber);

            var match = line.match(/require\s*?\(\s*?['"](.*?)['"]\s*?\)/);
            // var match = textUntilPosition.match(/moment/);

            const require_list = Object.keys(window.$__require);

            // console.warn('>js<', match);

            // var match = true; //lastTag.isAttributeSearch == false
            if (!match) {
                return { suggestions: [] };
            }
            // var word = model.getWordUntilPosition(position); //start of word
            let word = model.getWordAtPosition(position);

            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn
            };

            return {
                suggestions: createDependencyProposals__javascript(range, require_list)
            };
        }
    });
}


function createDependencyProposals__javascript(range, require_list) {

    // let coms = window.$_FindComponentsProps;

    // console.warn('>>>', tagName);

    // let tagComponent = Object.values(coms).find(s => s.tag == tagName.toLowerCase());

    // console.warn(tagComponent);

    // let snippets = Object.values(tagComponent.props).map(s => {


    //   /** @type {import('monaco-editor').languages.CompletionItem} */
    //   let snip = {
    //     label: s,
    //     kind: monaco.languages.CompletionItemKind.Property,
    //     // documentation: vue_snippets[s].description,
    //     insertText: `${s}="$1"$2`,
    //     range: range,
    //     insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
    //     sortText: '_'+s
    //   };
    //   return snip;
    // });


    // let less = less_option.globalVars;
    /** @type {import('monaco-editor').languages.CompletionItem[]} */
    let list = require_list.map(s => {
        /** @type {import('monaco-editor').languages.CompletionItem} */
        let z = {
            label: s,
            kind: monaco.languages.CompletionItemKind.Value,
            // documentation: less[s],
            insertText: s,
            range: { insert: range, replace: range },
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
        }
        return z
    });
    // console.warn('js>goog');

    // let list = [
    //   {
    //     label: '1moment2',
    //     kind: monaco.languages.CompletionItemKind.Value,
    //     // documentation: "Vue model bind",
    //     insertText: '1moment2',
    //     range: range,
    //     insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet

    //   },
    // ];

    return list;
};



function createDependencyProposals__attributes(range, { tagName }) {

    let coms = window.$_FindComponentsProps;

    console.warn('>>>', tagName);

    let tagComponent = Object.values(coms).find(s => s.tag == tagName.toLowerCase());

    if (!tagComponent) return [];

    console.warn(tagComponent);

    let snippets = Object.values(tagComponent.props).map(s => {

        let prop = tagComponent.component.options.props[s];

        console.warn('>' + s, prop);

        let f_type = '';
        let f_default = prop.default || ''

        f_type = (prop.type && prop.type.name) || prop.type;

        if (Array.isArray(f_type)) {
            f_type = prop.type.map(s => s.name || '?').join('|')
        }

        /** @type {import('monaco-editor').languages.CompletionItem} */
        let snip = {
            label: s,
            kind: monaco.languages.CompletionItemKind.Property,
            // documentation: vue_snippets[s].description,
            detail: (f_type || '') + ((f_default) ? `:${f_default}` : ''),
            insertText: `${s}="$1"$2`,
            range: range,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            sortText: '_' + s
        };
        return snip;
    });
}

window.addEventListener("message", receiveMessage, false);

function add_messages(editor) {

}

/**
 * @param {MessageEvent<{name:string, value:string}>} event
 */
function receiveMessage(event) {

    let data = event.data;

    if (!data.name) return;

    console.log("receiveMessage=", event)

    if (data.name == "init") {
        init({
            ...data.value
        });

        setTimeout(() => {
            editor.getAction('editor.action.formatDocument').run();
        }, 500);
    }

    if (data.name == "getValue") {
        event.source.postMessage({ name: "getValue", value: editor.getValue() }, event.origin)
    }
    else if (data.name == "setValue") {
        editor.setValue(data.value)
    }
    else if (data.name == "setModelLanguage") {
        monaco.editor.setModelLanguage(editor.getModel(), data.value);
    }
    else if (data.name == "d_scrollDown") {
        editor.revealLineInCenter(editor.getModel().getLineCount())
    }
}
 

function fire_message(name, data) {
    //console.warn("fire_message=" + name, data);
    console.log("fire_message=" + name);

    //parent.postMessage({ name: data, value: data }, parent.location.origin);
    parent.postMessage({ name: name, value: data }, (document.location.ancestorOrigins && document.location.ancestorOrigins[0]) || document.location.origin);
}

