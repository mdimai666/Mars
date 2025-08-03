import EditorJS from '@editorjs/editorjs';
import { EditorConfig } from '@editorjs/editorjs';
import Paragraph from '@editorjs/paragraph';
import Header from '@editorjs/header';
import List from '@editorjs/list';
//import SimpleImage from '@editorjs/simple-image';
//import ImageTool from '@editorjs/image';
import ImageTool from './ImageTool2';
import Delimiter from '@editorjs/delimiter';
import Checklist from '@editorjs/checklist';
import Quote from '@editorjs/quote';
import CodeTool from '@editorjs/code';
//import Embed from '@editorjs/embed';
import Embed from './EmbedTool2';
import Table from '@editorjs/table';
import LinkTool from '@editorjs/link';
import Warning from '@editorjs/warning';
import Marker from '@editorjs/marker';
import InlineCode from '@editorjs/inline-code';
import Raw from '@editorjs/raw';
import AttachesTool from '@editorjs/attaches';
import AttachesTool2 from './AttachesTool2';
import DragDrop from "editorjs-drag-drop";
import Undo from 'editorjs-undo';
//import { ImageUploaderConfig } from './ImageUploader';
import BlazorUploader from './BlazorUploader';

// https://github.com/editor-js/awesome-editorjs

/**
 *
 * @param {HTMLElement} el
 */
export class EditorJsWrapper {

    markerName = 'EditorJsWrapper';

    /** @type {EditorJS}*/
    editorJsInstance;

    /** @type {string}*/
    componentId = ''

    /** @type {HTMLElement}*/
    domElement;

    /** @type {any}*/
    blazorInstance;

    constructor(componentId, domElement, blazorInstance) {
        this.componentId = componentId;
        this.domElement = domElement;
        this.blazorInstance = blazorInstance;
    }

    async initialize(elementId) {
        /** @type {EditorConfig}*/
        const editorConfig = await this.getBlazorProperty("Config");
        const data = await this.getBlazorProperty("ContentJson");

        if (!elementId) return;

        let self = this;

        if (editorConfig != null) {
            //console.log(data);

            this.editorJsInstance = new EditorJS({
                holder: elementId,
                tools: this.getTools(editorConfig),
                autofocus: editorConfig.autoFocus,
                placeholder: editorConfig.placeholder,
                readOnly: editorConfig.readOnly,

                onChange: async () => {
                    await this.getData();
                },
                onReady: () => {
                    let editor = this.editorJsInstance;
                    //debugger;
                    new Undo({ editor });
                    new DragDrop(editor);
                    this.blazorInstance.invokeMethodAsync("OnReady");
                    console.log('Editor.js is ready to work!');
                },
                data: data
            });
        }
    }

    /**
     * get value of property
     * @param {string} propertyName
     * @returns
     */
    async getBlazorProperty(propertyName) {
        return await this.blazorInstance.invokeMethodAsync("GetBlazorProperty", propertyName);
    }

    async destroy() {
        this.editorJsInstance.destroy();
    }

    async getData() {
        const data = await this.editorJsInstance.save();
        await this.blazorInstance.invokeMethodAsync("SetValue", JSON.stringify(data));
    }

    /**
     * @param {EditorConfig} config
     */
    getTools(config) {
        let _self = this;
        return {
            /**
             * Each Tool is a Plugin. Pass them via 'class' option with necessary settings {@link docs/tools.md}
            * https://github.com/editor-js/awesome-editorjs
             */
            paragraph: {
                class: Paragraph,
                inlineToolbar: true,
            },
            header: {
                class: Header,
                inlineToolbar: ['marker', 'link'],
                config: {
                    placeholder: 'Header'
                },
                shortcut: 'CMD+SHIFT+H'
            },

            //image: SimpleImage,
            image: { // https://github.com/editor-js/image
                class: ImageTool,
                config: {
                    //editorJsWrapper: _self, //тут объект копируется через json и теряет методы
                    componentId: this.componentId,
                    onClickSelectImageUserMedia: true,
                    //endpoints: { byFile: "/uploadFileMock", byUrl: "/fetchUrlMock" },
                    //uploader: new BlazorUploader(this),
                    actions: ImageTool.Actions(this),
                    uploader: ImageTool.DownloadableFileViaBlazorCallback(this),
                }
            },
            attaches: { // https://github.com/editor-js/attaches
                class: AttachesTool2,
                //config: ImageUploaderConfig(this)
                //config: new BlazorUploader(this)
                config: {
                    componentId: this.componentId,
                    onClickSelectImageUserMedia: true,
                    actions: AttachesTool2.Actions(this),
                    uploader: AttachesTool2.DownloadableFileViaBlazorCallback(this),
                },
                shortcut: 'CMD+SHIFT+A'
            },

            list: {
                class: List,
                inlineToolbar: true,
                shortcut: 'CMD+SHIFT+L'
            },

            //checklist: { дублирется
            //    class: Checklist,
            //    inlineToolbar: true,
            //},

            quote: {
                class: Quote,
                inlineToolbar: true,
                config: {
                    quotePlaceholder: 'Enter a quote',
                    captionPlaceholder: 'Quote\'s author',
                },
                shortcut: 'CMD+SHIFT+O'
            },

            warning: Warning,

            marker: {
                class: Marker,
                shortcut: 'CMD+SHIFT+M'
            },

            code: {
                class: CodeTool,
                //shortcut: 'CMD+SHIFT+C'
            },

            delimiter: Delimiter,

            inlineCode: {
                class: InlineCode,
                //shortcut: 'CMD+SHIFT+C'
            },

            linkTool: LinkTool,

            embed: {
                class: Embed,
                //config: {
                //    services: {
                //        youtube: true,
                //        coub: true,
                //    }
                //}
            },

            table: {
                class: Table,
                inlineToolbar: true,
                shortcut: 'CMD+ALT+T'
            },

            raw: Raw,
        }
    }

}

export class EditorJsHandler {
    /** @type {{ [key: string]: EditorJsWrapper }} */
    editors = {};

    constructor() { }

    /**
     *
     * @param {string} editorId
     * @param {HTMLElement} domElement
     * @param {any} blazorInstance
     */
    async initializeEditor(editorId, domElement, blazorInstance) {
        if (this.editors[editorId]) {
            await this.editors[editorId].destroy();
        }

        const editor = new EditorJsWrapper(editorId, domElement, blazorInstance);

        await editor.initialize(editorId);
        //console.warn("editor",editor)

        this.editors[editorId] = editor;
    }

    /**
     *
     * @param {string} componentId
     * @param {string} methodName
     * @param {...any} params
     * @returns
     */
    callEditorMethod(componentId, methodName, ...params) {
        let editorJsInstance = this.editors[componentId].editorJsInstance
        return editorJsInstance[methodName](...params);
    }
}

window.editorJsHandler = new EditorJsHandler();

export function sayHello(name) {
    console.warn(`Hello, ${name}!`);
}
