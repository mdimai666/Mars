export default class BlazorUploader {

    editorJsWrapper;

    constructor(editorJsWrapper) {
        this.editorJsWrapper = editorJsWrapper
    }

    /**
     * Upload file to the server and return an uploaded image data
     * @param {File} file - file selected from the device or pasted by drag-n-drop
     * @return {Promise.<{success, file: {url}}>}
     */
    async uploadByFile(file) {

        let buffer = new Uint8Array(await file.arrayBuffer())

        return await this.editorJsWrapper.blazorInstance.invokeMethodAsync("UploadFileAction", buffer, file.name);
    }

    /**
     * Send URL-string to the server. Backend should load image by this URL and return an uploaded image data
     * @param {string} url - pasted image URL
     * @return {Promise.<{success, file: {url}}>}
     */
    async uploadByUrl(url) {

    }
}