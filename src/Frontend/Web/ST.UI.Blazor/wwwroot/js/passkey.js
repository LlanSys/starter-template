function coerceToArrayBuffer(thing) {
    if (typeof thing === "string") {
        let base64 = thing.replace(/-/g, "+").replace(/_/g, "/");
        let padLen = (4 - (base64.length % 4)) % 4;
        base64 += "=".repeat(padLen);

        const binaryString = window.atob(base64);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }
    if (thing instanceof Uint8Array) {
        return thing.buffer;
    }
    throw new Error("Could not coerce to ArrayBuffer");
}

function coerceToBase64Url(thing) {
    let bytes;
    if (thing instanceof ArrayBuffer) {
        bytes = new Uint8Array(thing);
    } else if (thing instanceof Uint8Array) {
        bytes = thing;
    } else {
        throw new Error("Could not coerce to base64url");
    }
    let binary = "";
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    const base64 = window.btoa(binary);
    return base64.replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "");
}

function preformatAssertionOptions(options) {
    const publicKey = { ...options };
    publicKey.challenge = coerceToArrayBuffer(publicKey.challenge);
    if (publicKey.allowCredentials) {
        publicKey.allowCredentials = publicKey.allowCredentials.map(c => ({
            ...c,
            id: coerceToArrayBuffer(c.id)
        }));
    }
    return publicKey;
}

function formatAssertionForServer(credential) {
    return {
        id: credential.id,
        rawId: coerceToBase64Url(credential.rawId),
        type: credential.type,
        response: {
            authenticatorData: coerceToBase64Url(credential.response.authenticatorData),
            clientDataJSON: coerceToBase64Url(credential.response.clientDataJSON),
            signature: coerceToBase64Url(credential.response.signature),
            userHandle: credential.response.userHandle ? coerceToBase64Url(credential.response.userHandle) : null
        }
    };
}

export async function isConditionalMediationAvailable() {
    return !!(window.PublicKeyCredential && PublicKeyCredential.isConditionalMediationAvailable && await PublicKeyCredential.isConditionalMediationAvailable());
}

let currentAbortController = null;
function getAbortSignal() {
    currentAbortController = new AbortController();
    return currentAbortController.signal;
}

export function cancelConditionalAssertion() {
    if (currentAbortController) {
        currentAbortController.abort();
    }
}

export async function startConditionalAssertion(optionsJson, dotNetRef) {
    if (!(await isConditionalMediationAvailable())) {
        return; // silently no-op
    }

    const options = preformatAssertionOptions(JSON.parse(optionsJson));

    try {
        const credential = await navigator.credentials.get({
            publicKey: options,
            mediation: "conditional",
            signal: getAbortSignal()
        });

        const assertionResponse = formatAssertionForServer(credential);
        await dotNetRef.invokeMethodAsync("OnConditionalAssertionCompleted", assertionResponse);
    } catch (err) {
        if (err.name !== "AbortError") {
            console.error("Conditional assertion failed:", err);
        }
    }
}

export async function startExplicitAssertion(optionsJson) {
    const options = preformatAssertionOptions(JSON.parse(optionsJson));
    const credential = await navigator.credentials.get({ publicKey: options });
    return JSON.stringify(formatAssertionForServer(credential));
}
