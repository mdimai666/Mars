async function sendRequest(type, request) {
    let endPoint;
    const prefix = '/api/Monaco';

    switch (type) {
        case 'complete': endPoint = '/completion/complete'; break;
        case 'signature': endPoint = '/completion/signature'; break;
        case 'hover': endPoint = '/completion/hover'; break;
        case 'codeCheck': endPoint = '/completion/codeCheck'; break;
    }
    return await fetch(prefix + endPoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(request)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        });
}

function registerCsharpProvider() {

    var assemblies = [
        //'.\\bin\\Debug\\net8.0\\System.Text.Json.dll',
        //'.\\bin\\Debug\\net8.0\\MyShared.dll',
    ];

    monaco.languages.registerCompletionItemProvider('csharp', {
        triggerCharacters: [".", " "],
        provideCompletionItems: async (model, position) => {
            let suggestions = [];

            let request = {
                Code: model.getValue(),
                Position: model.getOffsetAt(position),
                Assemblies: assemblies
            }

            let resultQ = await sendRequest("complete", request);

            for (let elem of resultQ) {
                suggestions.push({
                    label: {
                        label: elem.suggestion,
                        description: elem.description
                    },
                    //kind: monaco.languages.CompletionItemKind.Function,
                    kind: elem.kind,
                    insertText: elem.suggestion
                });
            }

            return { suggestions: suggestions };
        }
    });

    monaco.languages.registerSignatureHelpProvider('csharp', {
        signatureHelpTriggerCharacters: ["("],
        signatureHelpRetriggerCharacters: [","],

        provideSignatureHelp: async (model, position, token, context) => {

            let request = {
                Code: model.getValue(),
                Position: model.getOffsetAt(position),
                Assemblies: assemblies
            }

            let resultQ = await sendRequest("signature", request);
            if (!resultQ) return;

            let signatures = [];
            for (let signature of resultQ.signatures) {
                let params = [];
                for (let param of signature.parameters) {
                    params.push({
                        label: param.label,
                        documentation: param.documentation ?? ""
                    });
                }

                signatures.push({
                    label: signature.label,
                    documentation: signature.documentation ?? "",
                    parameters: params,
                });
            }

            let signatureHelp = {};
            signatureHelp.signatures = signatures;
            signatureHelp.activeParameter = resultQ.activeParameter;
            signatureHelp.activeSignature = resultQ.activeSignature;

            return {
                value: signatureHelp,
                dispose: () => { }
            };
        }
    });

    monaco.languages.registerHoverProvider('csharp', {
        provideHover: async function (model, position) {

            let request = {
                Code: model.getValue(),
                Position: model.getOffsetAt(position),
                Assemblies: assemblies
            }

            let resultQ = await sendRequest("hover", request);

            if (resultQ) {
                posStart = model.getPositionAt(resultQ.offsetFrom);
                posEnd = model.getPositionAt(resultQ.offsetTo);

                return {
                    range: new monaco.Range(posStart.lineNumber, posStart.column, posEnd.lineNumber, posEnd.column),
                    contents: [
                        { value: resultQ.information }
                    ]
                };
            }

            return null;
        }
    });

    monaco.editor.onDidCreateModel(function (model) {
        async function validate() {

            let request = {
                Code: model.getValue(),
                Assemblies: assemblies
            }

            let resultQ = await sendRequest("codeCheck", request)

            let markers = [];

            for (let elem of resultQ) {
                posStart = model.getPositionAt(elem.offsetFrom);
                posEnd = model.getPositionAt(elem.offsetTo);
                markers.push({
                    severity: elem.severity,
                    startLineNumber: posStart.lineNumber,
                    startColumn: posStart.column,
                    endLineNumber: posEnd.lineNumber,
                    endColumn: posEnd.column,
                    message: elem.message,
                    code: elem.id
                });
            }

            monaco.editor.setModelMarkers(model, 'csharp', markers);
        }

        var handle = null;
        model.onDidChangeContent(() => {
            monaco.editor.setModelMarkers(model, 'csharp', []);
            clearTimeout(handle);
            handle = setTimeout(() => validate(), 500);
        });
        validate();
    });

    /*monaco.languages.registerInlayHintsProvider('csharp', {
        displayName: 'test',
        provideInlayHints(model, range, token) {
            return {
                hints: [
                    {
                        label: "Test",
                        tooltip: "Tooltip",
                        position: { lineNumber: 3, column: 2},
                        kind: 2
                    }
                ],
                dispose: () => { }
            };
        }

    });*/

    /*monaco.languages.registerCodeActionProvider("csharp", {
        provideCodeActions: async (model, range, context, token) => {
            const actions = context.markers.map(error => {
                console.log(context, error);
                console.log("range=", range);
                //let markerRange = new monaco.Range(2, 8, 2, 22);
                debugger
                let edits = [];
                for (const change of codeFix.changes) {
                    for (const textChange of change.textChanges) {
                        edits.push({
                            resource: model.uri,
                            versionId: undefined,
                            textEdit: {
                                range: this._textSpanToRange(model, textChange.span),
                                text: textChange.newText
                            }
                        });
                    }
                }

                return {
                    title: `Example quick fix`,
                    diagnostics: [error],
                    kind: "quickfix",
                    //edit: {
                    //    edits: [
                    //        {
                    //            resource: model.uri,
                    //            edits: [
                    //                {
                    //                    range: error,
                    //                    text: "This text replaces the text with the error"
                    //                }
                    //            ]
                    //        }
                    //    ]
                    //},
                    edit: { edits: edits },
                    //command: { id:'command.id', title: 'command title' },
                    isPreferred: true
                };
            });
            return {
                actions: actions,
                dispose: () => { }
            }
        }
    });*/

}
