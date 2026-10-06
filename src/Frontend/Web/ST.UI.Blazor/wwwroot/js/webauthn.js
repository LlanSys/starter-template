// Base64URL string <-> ArrayBuffer conversions

function coerceToArrayBuffer(thing) {
    if (typeof thing === "string") {
        // base64url to base64
        let base64 = thing.replace(/-/g, "+").replace(/_/g, "/");
        // pad
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

// Convert the backend options (JSON) into a format suitable for navigator.credentials.create
function preformatMakeCredentialOptions(options) {
    const preformatted = { ...options };

    preformatted.challenge = coerceToArrayBuffer(preformatted.challenge);
    preformatted.user.id = coerceToArrayBuffer(preformatted.user.id);

    if (preformatted.excludeCredentials) {
        preformatted.excludeCredentials = preformatted.excludeCredentials.map(c => ({
            ...c,
            id: coerceToArrayBuffer(c.id)
        }));
    }

    return preformatted;
}

// Convert the navigator.credentials.create output into JSON suitable for the backend
function publicKeyCredentialToJSON(pubKeyCred) {
    if (pubKeyCred instanceof Array) {
        return pubKeyCred.map(publicKeyCredentialToJSON);
    }

    if (pubKeyCred instanceof ArrayBuffer) {
        return coerceToBase64Url(pubKeyCred);
    }

    if (pubKeyCred instanceof Object) {
        let obj = {};
        for (let key in pubKeyCred) {
            obj[key] = publicKeyCredentialToJSON(pubKeyCred[key]);
        }
        return obj;
    }

    return pubKeyCred;
}

window.webauthn = {
    // Check if webauthn is supported
    isSupported: function () {
        return window.PublicKeyCredential !== undefined;
    },

    registerPasskey: async function (optionsJson) {
        try {
            const options = JSON.parse(optionsJson);
            const publicKey = preformatMakeCredentialOptions(options);

            const credential = await navigator.credentials.create({ publicKey });

            const result = {
                id: credential.id,
                rawId: coerceToBase64Url(credential.rawId),
                type: credential.type,
                response: {
                    attestationObject: coerceToBase64Url(credential.response.attestationObject),
                    clientDataJSON: coerceToBase64Url(credential.response.clientDataJSON),
                    transports: credential.response.getTransports ? credential.response.getTransports() : []
                }
            };

            return JSON.stringify(result);
        } catch (err) {
            console.error("Passkey registration error:", err);
            throw new Error(err.message || "Registration failed");
        }
    },

    loginWithPasskey: async function (optionsJson) {
        try {
            const options = JSON.parse(optionsJson);

            const publicKey = { ...options };
            publicKey.challenge = coerceToArrayBuffer(publicKey.challenge);

            if (publicKey.allowCredentials) {
                publicKey.allowCredentials = publicKey.allowCredentials.map(c => ({
                    ...c,
                    id: coerceToArrayBuffer(c.id)
                }));
            }

            const credential = await navigator.credentials.get({ publicKey });

            const result = {
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

            return JSON.stringify(result);
        } catch (err) {
            console.error("Passkey login error:", err);
            throw new Error(err.message || "Login failed");
        }
    },

    // --- Conditional UI (WebAuthn Level 3) ---

    isConditionalMediationAvailable: async function () {
        return !!(window.PublicKeyCredential &&
            PublicKeyCredential.isConditionalMediationAvailable &&
            await PublicKeyCredential.isConditionalMediationAvailable());
    },

    _conditionalAbortController: null,

    cancelConditionalAssertion: function () {
        if (window.webauthn._conditionalAbortController) {
            window.webauthn._conditionalAbortController.abort();
            window.webauthn._conditionalAbortController = null;
        }
    },

    startConditionalAssertion: async function (optionsJson, dotNetRef) {
        if (!(await window.webauthn.isConditionalMediationAvailable())) {
            return;
        }

        const options = JSON.parse(optionsJson);
        const publicKey = { ...options };
        publicKey.challenge = coerceToArrayBuffer(publicKey.challenge);
        if (publicKey.allowCredentials) {
            publicKey.allowCredentials = publicKey.allowCredentials.map(c => ({
                ...c,
                id: coerceToArrayBuffer(c.id)
            }));
        }

        window.webauthn._conditionalAbortController = new AbortController();

        try {
            const credential = await navigator.credentials.get({
                publicKey,
                mediation: "conditional",
                signal: window.webauthn._conditionalAbortController.signal
            });

            const result = {
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

            await dotNetRef.invokeMethodAsync("OnConditionalAssertionCompleted", result);
        } catch (err) {
            if (err.name !== "AbortError") {
                console.error("Conditional passkey assertion failed:", err);
            }
        }
    },

    startExplicitAssertion: async function (optionsJson) {
        const options = JSON.parse(optionsJson);
        const publicKey = { ...options };
        publicKey.challenge = coerceToArrayBuffer(publicKey.challenge);
        if (publicKey.allowCredentials) {
            publicKey.allowCredentials = publicKey.allowCredentials.map(c => ({
                ...c,
                id: coerceToArrayBuffer(c.id)
            }));
        }

        const credential = await navigator.credentials.get({ publicKey });

        const result = {
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

        return JSON.stringify(result);
    }
};
