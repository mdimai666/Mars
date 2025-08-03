// Overwrite Embed Block Tool
import EmbedTool from '@editorjs/embed';

export default class Embed extends EmbedTool {
    static get toolbox() {
        return {
            title: 'Embed',
            icon: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-youtube w-6 h-6 mx-1"><path d="M2.5 17a24.12 24.12 0 0 1 0-10 2 2 0 0 1 1.4-1.4 49.56 49.56 0 0 1 16.2 0A2 2 0 0 1 21.5 7a24.12 24.12 0 0 1 0 10 2 2 0 0 1-1.4 1.4 49.55 49.55 0 0 1-16.2 0A2 2 0 0 1 2.5 17"></path><path d="m10 15 5-3-5-3z"></path></svg>'
        };
    }

    /**
     * Render Embed tool content
     *
     * @returns {HTMLElement}
     */
    render() {
        if (!this.data.service) {
            const container = document.createElement('div');

            container.appendChild(this.hintDiv());

            this.element = container;
            const input = document.createElement('input');
            input.classList.add('cdx-input');
            input.placeholder = 'https://www.youtube.com/watch?v=w8vsuOXZBXc';
            input.type = 'url';
            input.addEventListener('paste', (event) => {
                const url = event.clipboardData.getData('text');
                const service = Object.keys(Embed.services).find((key) => Embed.services[key].regex.test(url));
                if (service) {
                    this.onPaste({ detail: { key: service, data: url } });
                }
            });
            container.appendChild(input);

            return container;
        }
        return super.render();
    }

    hintDiv() {
        const hint = document.createElement('div');
        hint.innerHTML = 'services : ';
        hint.style.fontSize = "12px";
        hint.style.color = "lightgrey";
        hint.style.display = "flex"
        hint.style.gap = "0 0.5em"
        hint.style.flexWrap = "wrap"
        hint.style.marginBottom = "10px"

        for (let key in EmbedTool.services) {
            let service = EmbedTool.services[key]
            let embedUrl = service.embedUrl
            let url = new URL(embedUrl);
            let urlString = url.origin
            let a = document.createElement('a');
            a.href = urlString;
            a.target = "_blank"
            a.innerHTML = key
            hint.appendChild(a)
        }
        return hint
    }

    validate(savedData) {
        return savedData.service && savedData.source ? true : false;
    }
}