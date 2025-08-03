// Overwrite Image Block Tool
import ImageTool from '@editorjs/image';
//import EditorJsWrapper from './index';

export default class Image2 extends ImageTool {

    componentId = 0
    /** @type {EditorJsWrapper} */

    //constructor({ data, config, api, readOnly, block }) {
    constructor(params) {
        super(params)
        this.componentId = params.config.componentId;

        this.ui.onSelectFile = () => {
            if (params.config.onClickSelectImageUserMedia) {
                this.clickSelectImageUserMedia()
            }
            else {
                this.uploader.uploadSelectedFile({
                    onPreview: (src) => {
                        this.ui.showPreloader(src);
                    },
                });
            }
        }
    }

    getEditorJsWrapper() {
        return window.editorJsHandler.editors[this.componentId]
    }

    async clickSelectImageUserMedia() {
        let editorJsWrapper = this.getEditorJsWrapper();

        var ImageFileData = await editorJsWrapper.blazorInstance.invokeMethodAsync("OnClickSelectImageFileJSInvokable");

        if (!ImageFileData) return;

        this.image = ImageFileData;

    }

    appendCallback() {
        //alert(111);
        this.ui.nodes.fileButton.click();
    }

    static DownloadableFileViaBlazorCallback(editorJsWrapper) {
        return {
            /**
                * Upload file to the server and return an uploaded image data
                * @param {File} file - file selected from the device or pasted by drag-n-drop
                * @return {Promise.<{success, file: {url}}>}
                */
            async uploadByFile(file) {

                let buffer = new Uint8Array(await file.arrayBuffer())

                return await editorJsWrapper.blazorInstance.invokeMethodAsync("UploadFileAction", buffer, file.name);
            },

            /**
                * Send URL-string to the server. Backend should load image by this URL and return an uploaded image data
                * @param {string} url - pasted image URL
                * @return {Promise.<{success, file: {url}}>}
                */
            async uploadByUrl(url) {
                // your ajax request for uploading
                //return MyAjax.upload(file).then(() => {
                //    return {
                //        success: 1,
                //        file: {
                //            url: 'https://codex.so/upload/redactor_images/o_e48549d1855c7fc1807308dd14990126.jpg',
                //            // any other image data you want to store, such as width, height, color, extension, etc
                //        }
                //    }
                //})

            }
        }
    }

    static Actions(editorJsWrapper) {
        return [
            //{
            //    // image props action tool
            //    name: 'new_button',
            //    icon: `<svg viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
            //              <circle cx="10" cy="10" r="10" />
            //            </svg>`,
            //    title: 'New Button',
            //    toggle: true,
            //    action: (name) => {
            //        alert(`${name} button clicked`);
            //    }
            //}
        ]
    }
}