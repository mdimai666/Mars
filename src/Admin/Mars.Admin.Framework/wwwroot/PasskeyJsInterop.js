function convertToBase64(o) {
    if (!o) {
        return undefined;
    }

    // Normalize Array to Uint8Array
    if (Array.isArray(o)) {
        o = Uint8Array.from(o);
    }

    // Normalize ArrayBuffer to Uint8Array
    if (o instanceof ArrayBuffer) {
        o = new Uint8Array(o);
    }

    // Convert Uint8Array to base64
    if (o instanceof Uint8Array) {
        let str = '';
        for (let i = 0; i < o.byteLength; i++) {
            str += String.fromCharCode(o[i]);
        }
        o = window.btoa(str);
    }

    if (typeof o !== 'string') {
        throw new Error('Could not convert to base64 string');
    }

    // Convert base64 to base64url
    return o.replace(/\+/g, '-').replace(/\//g, '_').replace(/=*$/g, '');
}

// Ручная сериализация: у части парольных менеджеров сломан PublicKeyCredential.toJSON ("Illegal invocation").
// См. https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0
function serializeCredential(credential) {
    return JSON.stringify({
        authenticatorAttachment: credential.authenticatorAttachment,
        clientExtensionResults: credential.getClientExtensionResults(),
        id: credential.id,
        rawId: convertToBase64(credential.rawId),
        response: {
            attestationObject: convertToBase64(credential.response.attestationObject),
            authenticatorData: convertToBase64(credential.response.authenticatorData ??
                credential.response.getAuthenticatorData?.() ?? undefined),
            clientDataJSON: convertToBase64(credential.response.clientDataJSON),
            publicKey: convertToBase64(credential.response.getPublicKey?.() ?? undefined),
            publicKeyAlgorithm: credential.response.getPublicKeyAlgorithm?.() ?? undefined,
            transports: credential.response.getTransports?.() ?? undefined,
            signature: convertToBase64(credential.response.signature),
            userHandle: convertToBase64(credential.response.userHandle),
        },
        type: credential.type,
    });
}

// Cookie-схема (A1): авторизация — Identity-cookie, уходит same-origin запросом
// благодаря credentials: 'include'; токенов в localStorage больше нет.
async function fetchJson(url, options) {
    const response = await fetch(url, { credentials: 'include', ...options });
    const text = await response.text();

    let body = null;
    try {
        body = text ? JSON.parse(text) : null;
    } catch {
        // не-JSON тело (например, HTML ошибки прокси)
    }

    if (!response.ok) {
        const message = body?.message || body?.errorMessage || body?.title || `HTTP ${response.status}`;
        throw new Error(message);
    }

    return body;
}

export function isAvailable() {
    return typeof PublicKeyCredential !== 'undefined'
        && typeof navigator !== 'undefined'
        && !!navigator.credentials;
}

export async function registerPasskey(optionsUrl, submitUrl, name) {
    const optionsJson = await fetchJson(optionsUrl, { method: 'POST' });
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    const credential = await navigator.credentials.create({ publicKey: options });

    return await fetchJson(submitUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ credentialJson: serializeCredential(credential), name }),
    });
}

export async function loginWithPasskey(optionsUrl, loginUrl) {
    const optionsJson = await fetchJson(optionsUrl, { method: 'POST' });
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    const credential = await navigator.credentials.get({ publicKey: options });

    return await fetchJson(loginUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ credentialJson: serializeCredential(credential) }),
    });
}
