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

    _conditionalRestartListener: null,

    _clearConditionalRestart: function () {
        const listener = window.webauthn._conditionalRestartListener;
        if (listener) {
            window.removeEventListener("focus", listener);
            document.removeEventListener("visibilitychange", listener);
            window.webauthn._conditionalRestartListener = null;
        }
    },

    // The browser ends a conditional request with NotAllowedError when its timeout elapses, when another
    // tab or frame starts its own WebAuthn request, or when the user dismisses the picker. That is a normal
    // end of passkey autofill, not a failure: ask .NET for fresh options once the page is in use again.
    _scheduleConditionalRestart: function (dotNetRef) {
        window.webauthn._clearConditionalRestart();
        const listener = () => {
            if (document.visibilityState !== "visible" || !document.hasFocus()) {
                return;
            }
            window.webauthn._clearConditionalRestart();
            dotNetRef.invokeMethodAsync("RestartConditionalAssertionAsync").catch(() => { });
        };
        window.webauthn._conditionalRestartListener = listener;
        window.addEventListener("focus", listener);
        document.addEventListener("visibilitychange", listener);
    },

    cancelConditionalAssertion: function () {
        window.webauthn._clearConditionalRestart();
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

        // Only one WebAuthn request may be pending; end ours before starting its replacement.
        window.webauthn.cancelConditionalAssertion();
        const controller = new AbortController();
        window.webauthn._conditionalAbortController = controller;

        try {
            const credential = await navigator.credentials.get({
                publicKey,
                mediation: "conditional",
                signal: controller.signal
            });

            if (window.webauthn._conditionalAbortController === controller) {
                window.webauthn._conditionalAbortController = null;
            }

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
            const isCurrent = window.webauthn._conditionalAbortController === controller;
            if (isCurrent) {
                window.webauthn._conditionalAbortController = null;
            }

            if (err.name === "AbortError") {
                return; // We cancelled it: workspace change, explicit passkey sign-in, replacement or dispose.
            }

            if (err.name === "NotAllowedError") {
                console.debug("Passkey autofill request ended; it restarts when the page is focused.", err.message);
                if (isCurrent) {
                    window.webauthn._scheduleConditionalRestart(dotNetRef);
                }
                return;
            }

            console.error("Conditional passkey assertion failed:", err);
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
