export function showPrompt(message) {
    return prompt(message, 'Type anything here');
}

export async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
}

export async function downloadFileFromUrl(fileName, url) {
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? '';
    anchor.click();
    anchor.remove();
}
