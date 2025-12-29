// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

export function showPrompt(message) {
  return prompt(message, 'Type anything here');
}

export function InitModule() {
    console.log("InitModule");

    // Обработчик наведения
    document.body.addEventListener('mouseover', function (e) {
        const target = e.target;
        if (target.matches('.red-ui-workspace-chart-event-layer [v-add-class-hover]')) {
            const hoverClass = target.getAttribute('v-add-class-hover');
            if (hoverClass) {
                target.classList.add(hoverClass);
            }
        }
    });

    // Обработчик ухода курсора
    document.body.addEventListener('mouseout', function (e) {
        const target = e.target;
        if (target.matches('.red-ui-workspace-chart-event-layer [v-add-class-hover]')) {
            const hoverClass = target.getAttribute('v-add-class-hover');
            if (hoverClass) {
                target.classList.remove(hoverClass);
            }
        }
    });
}

export function ScrollDownElement(selector) {
    /** @type {HTMLElement} */
    var objDiv = document.querySelector(selector);
    setTimeout(() => {
        objDiv.scrollTop = objDiv.scrollHeight;
    },1)
}

export function ShowOffcanvas(htmlId, open) {

    var myOffcanvas = document.getElementById(htmlId)

    var bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(myOffcanvas)
    
    if (open) {
        bsOffcanvas.show();
    } else {
        bsOffcanvas.hide();
    }
}

export function HtmlGetElementScroll(selector) {
    let e = document.querySelector(selector)

    return { x: e.scrollLeft, y: e.scrollTop }
}

const resizeObservers = new Map();

export function observeSize(element, dotNetRef) {
    if (!element) return;

    const observer = new ResizeObserver(entries => {
        for (const entry of entries) {
            dotNetRef.invokeMethodAsync(
                "OnElementResize",
                entry.contentRect.width,
                entry.contentRect.height
            );
        }
    });

    observer.observe(element);
    resizeObservers.set(element, observer);
}

export function unobserveSize(element) {
    const observer = resizeObservers.get(element);
    if (observer) {
        observer.disconnect();
        resizeObservers.delete(element);
    }
}

const scrollObservers = new Map();

/**
 * 
 * @param {HTMLElement} element
 * @param {any} dotNetRef
 * @returns
 */
export function observeScroll(element, dotNetRef) {
    if (!element) return;

    const handler = () => {
        dotNetRef.invokeMethodAsync(
            "OnElementScroll",
            element.scrollTop,
            element.scrollLeft,
            element.scrollHeight,
            element.clientHeight,
            element.scrollWidth,
            element.clientWidth
        );
    };

    element.addEventListener("scroll", handler, { passive: true });

    scrollObservers.set(element, handler);
}

export function unobserveScroll(element) {
    const handler = scrollObservers.get(element);
    if (!handler) return;

    element.removeEventListener("scroll", handler);
    scrollObservers.delete(element);
}
