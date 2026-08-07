/// <reference path="../../global.d.ts" />

class App {
    _actions = []

    domReady(a) {
        app._actions.push(a)
    }

    trigggerLoad() {
        for (let a of this._actions) {
            a();
        }
    }

    /**
     * 
     * @param {string} login 
     * @param {string} password 
     * @returns {Promise<UserActionResult>}
     */
    async Login(login, password) {
        const url = `/api/Account/Login`;
        let data = { login, password };

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });

            /** @type {IAccountLoginResponse} */
            const res = await response.json();

            if (res.isAuthSuccessful) {
                localStorage.setItem('authToken', res.token);
            }

            let a = new UserActionResult();
            a.ok = res.isAuthSuccessful;
            a.message = res.errorMessage;

            return a;
        } catch (err) {
            let res = new UserActionResult();
            res.message = err?.message || "Unknown error";
            return res;
        }
    }

    Logout() {
        let cookieKey = ".AspNetCore.Identity.Application";
        let sPath = '', sDomain = '';

        document.cookie = encodeURIComponent(cookieKey) + "=; expires=Thu, 01 Jan 1970 00:00:00 GMT" + (sDomain ? "; domain=" + sDomain : "") + (sPath ? "; path=" + sPath : "");

        localStorage.removeItem("authToken");
        if (location.pathname.toLowerCase().startsWith('/logout')) {
            location = '/';
        } else {
            location.reload();
        }
    }
}

class UserActionResult {
    data = {}
    message = ""
    ok = false
}

var app = new App();
window.app = app;
document.addEventListener('DOMContentLoaded', () => {
    app.trigggerLoad();
});

function navlinks_detect_active(selector) {
    let links = document.querySelectorAll(selector);
    for (let i = 0; i < links.length; i++) {
        let a = links[i];
        let match_full = a.hasAttribute('match-full');
        let href = a.getAttribute('href');

        if (href == '/' && location.pathname == href) {
            a.classList.add('active')
        }
        else if (location.pathname == href) {
            a.classList.add('active');
        }
        else if (href == '/') {
            a.classList.remove('active');
        }
        else if (location.pathname.startsWith(href) && !match_full) {
            a.classList.add('active');
        }
    }
}
