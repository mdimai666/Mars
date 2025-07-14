window.d_onPageLoad = function () {
    //console.log('d_onPageLoad');
}

window.BeauityJsonInSelector = function (selector, value) {
    setTimeout(() => {
        const div = document.querySelector(selector);
        if (!div) return;

        const content = value || div.innerHTML;
        div.innerHTML = '';
        const pre = document.createElement('pre');
        pre.innerHTML = beauityJSON(content);
        div.appendChild(pre);
    }, 10);
}

function beauityJSON(data) {
    if (!data) return '';

    // Parse if input is string
    if (typeof data === 'string') {
        try {
            if (data.trim().startsWith('{') || data.trim().startsWith('[')) {
                data = JSON.parse(data);
            } else {
                return 'is String:: ' + data;
            }
        } catch (e) {
            return 'Error parsing JSON: ' + e.message;
        }
    }

    return syntaxHighlight(data);
}

function syntaxHighlight(json) {
    if (typeof json != 'string') {
        json = JSON.stringify(json, undefined, 2);
    }
    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
        var cls = 'number';
        if (/^"/.test(match)) {
            if (/:$/.test(match)) {
                cls = 'key';
            } else {
                cls = 'string';
            }
        } else if (/true|false/.test(match)) {
            cls = 'boolean';
        } else if (/null/.test(match)) {
            cls = 'null';
        }
        return '<span class="' + cls + '">' + match + '</span>';
    });
}

document.addEventListener('click', function (event) {
    //if (event.target.closest('#spotlight .spl-pane')) {
    if (event.target.classList.contains("spl-pane")) {
        Spotlight.close();
    }
});

window.blazor_newTab = function (url) {
    window.open(url, '_blank').focus();
}

function BlazorDownloadFile(filename, contentType, content) {
    // Blazor marshall byte[] to a base64 string, so we first need to convert the string (content) to a Uint8Array to create the File

    console.warn(content.length)

    //const data = base64DecToArr(content);
    const data = (content);

    // Create the URL
    const file = new File([data], filename, { type: contentType });
    const exportUrl = URL.createObjectURL(file);

    // Create the <a> element and click on it
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = filename;
    a.target = "_self";
    a.click();

    // We don't need to keep the url, let's release the memory
    // On Safari it seems you need to comment this line... (please let me know if you know why)
    URL.revokeObjectURL(exportUrl);
}

window.MarsTriggerFileDownload = (fileName, url) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

// Convert a base64 string to a Uint8Array. This is needed to create a blob object from the base64 string.
// The code comes from: https://developer.mozilla.org/fr/docs/Web/API/WindowBase64/D%C3%A9coder_encoder_en_base64
function b64ToUint6(nChr) {
    return nChr > 64 && nChr < 91 ? nChr - 65 : nChr > 96 && nChr < 123 ? nChr - 71 : nChr > 47 && nChr < 58 ? nChr + 4 : nChr === 43 ? 62 : nChr === 47 ? 63 : 0;
}

function base64DecToArr(sBase64, nBlocksSize) {
    var
        sB64Enc = sBase64.replace(/[^A-Za-z0-9\+\/]/g, ""),
        nInLen = sB64Enc.length,
        nOutLen = nBlocksSize ? Math.ceil((nInLen * 3 + 1 >> 2) / nBlocksSize) * nBlocksSize : nInLen * 3 + 1 >> 2,
        taBytes = new Uint8Array(nOutLen);

    for (var nMod3, nMod4, nUint24 = 0, nOutIdx = 0, nInIdx = 0; nInIdx < nInLen; nInIdx++) {
        nMod4 = nInIdx & 3;
        nUint24 |= b64ToUint6(sB64Enc.charCodeAt(nInIdx)) << 18 - 6 * nMod4;
        if (nMod4 === 3 || nInLen - nInIdx === 1) {
            for (nMod3 = 0; nMod3 < 3 && nOutIdx < nOutLen; nMod3++, nOutIdx++) {
                taBytes[nOutIdx] = nUint24 >>> (16 >>> nMod3 & 24) & 255;
            }
            nUint24 = 0;
        }
    }
    return taBytes;
}

//===========

//console.log('0. CTRL + S');
document.addEventListener("DOMContentLoaded", function (event) {
    //console.log("DOM fully loaded and parsed");
    document.addEventListener('keydown', track_save_shortcut);
});

/**
 * @param {KeyboardEvent} e
 */
function track_save_shortcut(e) {
    if (e.ctrlKey && e.code == 'KeyS') {

        var refreshBtn = document.querySelector('.DEV_btn_page__refresh');

        if (refreshBtn) {
            e.preventDefault();
            console.log('CTRL + S');
            refreshBtn.click();
        }
    }
}

function triggerHotCheckAni() {
    // Показываем все элементы с классом HotCheckAni
    document.querySelectorAll('.HotCheckAni').forEach(element => {
        element.style.display = 'block'; // или 'flex', 'inline' в зависимости от нужного типа отображения
    });

    // Скрываем элементы через 900 мс
    setTimeout(function () {
        document.querySelectorAll('.HotCheckAni').forEach(element => {
            element.style.display = 'none';
        });
    }, 900);
}

//=========== iframe code editor. TODO remove

let _dotNetHelper;

window.DotnetKeepMe = (dotNetHelper) => {
    //return dotNetHelper.invokeMethodAsync('GetHelloMessage');
    _dotNetHelper = dotNetHelper;
    return "OK";
};

function BlazorPostMessage(selector, data) {
    let iframe = document.querySelector(selector)
    iframe.contentWindow.postMessage(data, iframe.src)
}

window.addEventListener("message", receiveMessage, false);
/**
 * @param {MessageEvent<{name:string, value:string}>} event
 */
function receiveMessage(event) {
    let data = event.data;

    if (!data.name) return;

    console.log("Blazor:receiveMessage=" + data.name)
    //console.warn(_dotNetHelper)

    return _dotNetHelper.invokeMethodAsync('GetMonacoMessage', data.name, data.value);
}

//===========

document.body.addEventListener('click', function (e) {
    if (!e.target.hasAttribute('scroll-to')) return;

    e.preventDefault();

    const selector = e.target.getAttribute('scroll-to');
    const scrollTargetSelector = e.target.getAttribute('scroll-target');

    const elementToScrollTo = document.querySelector(selector);
    if (!elementToScrollTo) return;

    const scrollContainer = scrollTargetSelector
        ? document.querySelector(scrollTargetSelector)
        : window;

    const scrollPosition = elementToScrollTo.offsetTop - 150;

    scrollContainer.scrollTo({
        top: scrollPosition,
        behavior: 'smooth'
    });
});

document.body.addEventListener('click', function (e) {
    if (e.target.classList.contains('f-dark-theme-toggle')) {
        e.preventDefault();

        const isDark = document.body.classList.contains('dark');

        set_theme_darklight(!isDark);
    }
});

var docCookies = {
    getItem: function (sKey) {
        return decodeURIComponent(document.cookie.replace(new RegExp("(?:(?:^|.*;)\\s*" + encodeURIComponent(sKey).replace(/[\-\.\+\*]/g, "\\$&") + "\\s*\\=\\s*([^;]*).*$)|^.*$"), "$1")) || null;
    },
    setItem: function (sKey, sValue, vEnd, sPath, sDomain, bSecure) {
        if (!sKey || /^(?:expires|max\-age|path|domain|secure)$/i.test(sKey)) { return false; }
        var sExpires = "";
        if (vEnd) {
            switch (vEnd.constructor) {
                case Number:
                    sExpires = vEnd === Infinity ? "; expires=Fri, 31 Dec 9999 23:59:59 GMT" : "; max-age=" + vEnd;
                    break;
                case String:
                    sExpires = "; expires=" + vEnd;
                    break;
                case Date:
                    sExpires = "; expires=" + vEnd.toUTCString();
                    break;
            }
        }
        document.cookie = encodeURIComponent(sKey) + "=" + encodeURIComponent(sValue) + sExpires + (sDomain ? "; domain=" + sDomain : "") + (sPath ? "; path=" + sPath : "") + (bSecure ? "; secure" : "");
        return true;
    },
    removeItem: function (sKey, sPath, sDomain) {
        if (!sKey || !this.hasItem(sKey)) { return false; }
        document.cookie = encodeURIComponent(sKey) + "=; expires=Thu, 01 Jan 1970 00:00:00 GMT" + (sDomain ? "; domain=" + sDomain : "") + (sPath ? "; path=" + sPath : "");
        return true;
    },
    hasItem: function (sKey) {
        return (new RegExp("(?:^|;\\s*)" + encodeURIComponent(sKey).replace(/[\-\+\*]/g, "\\$&") + "\\s*\\=")).test(document.cookie);
    },
    keys: /* optional method: you can safely remove it! */ function () {
        var aKeys = document.cookie.replace(/((?:^|\s*;)[^\=]+)(?=;|$)|^\s*|\s*(?:\=[^;]*)?(?:\1|$)/g, "").split(/\s*(?:\=[^;]*)?;\s*/);
        for (var nIdx = 0; nIdx < aKeys.length; nIdx++) { aKeys[nIdx] = decodeURIComponent(aKeys[nIdx]); }
        return aKeys;
    }
};

function set_theme_darklight(setdark) {
    if (setdark) {
        document.body.classList.add('dark')
        docCookies.setItem('dark', true)
    } else {
        document.body.classList.remove('dark')
        docCookies.setItem('dark', false)
    }
}

function NavFromJs(url) {
    DotNet.invokeMethodAsync('AppFront', 'NavFromJs', url)
        .then(data => {
            //console.log(data);
        });
}

function MyJS_Cookie_Remove(key) {
    docCookies.removeItem(key)
}

window.affix = function (elementRef, id, offsetTop) {
    const element = document.getElementById(id);
    const rect = element.getBoundingClientRect();
    const initialOffsetTop = rect.top + window.pageYOffset - offsetTop;

    window.addEventListener('scroll', () => {
        const currentScrollPosition = window.pageYOffset;

        if (window.innerWidth < 1200) return;

        if (currentScrollPosition >= initialOffsetTop) {
            element.style.position = 'fixed';
            element.style.top = `${offsetTop}px`;
        } else {
            element.style.position = 'relative';
            element.style.top = '';
        }
    });
};

window.setOffcanvasState = function (querySel, state) {
    const offcanvasElementList = document.querySelectorAll(querySel)
    const offcanvasList = [...offcanvasElementList].map(offcanvasEl => new bootstrap.Offcanvas(offcanvasEl))
    offcanvasList.map(s => s[state ? 'show' : 'hide']())

}

window.toggleOffcanvas = function (querySel) {
    const offcanvasElementList = document.querySelectorAll(querySel)
    const offcanvasList = [...offcanvasElementList].map(offcanvasEl => new bootstrap.Offcanvas(offcanvasEl))
    offcanvasList.map(s => s.toggle())

}
