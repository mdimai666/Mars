// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function FramePostMessage(selector, data) {
    let iframe = document.querySelector(selector)
    iframe.contentWindow.postMessage(data, iframe.src)
}
