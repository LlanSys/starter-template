// Startup interaction gate.
//
// The interactive router is not prerendered, so until the Blazor Server circuit has connected and the
// router has finished resolving the workspace and session, the page shows only the static loading card.
// Clicks made in that window land on HTML with no event handlers and are silently dropped, which users
// experience as "the first click after the page loads is ignored".
//
// This script runs before blazor.web.js. While the app is starting it:
//   * marks the document as busy (html.app-starting, aria-busy) so the cursor and assistive tech show it;
//   * swallows pointer clicks instead of letting them hit inert markup, remembering where the last one was;
//   * once the router marks itself ready ([data-app-ready]), moves focus to the form field under that
//     remembered point, so a click into a field during startup is not lost.
// It never replays a click on a button or link: the user had not seen that control when they clicked.
(() => {
    const readySelector = "[data-app-ready]";
    const focusableSelector =
        "input:not([type=hidden]):not([type=button]):not([type=submit]):not([type=checkbox]):not([type=radio]):not([disabled])," +
        "textarea:not([disabled]),select:not([disabled]),[contenteditable=true]";
    const exemptSelector = "#components-reconnect-modal, #blazor-error-ui";
    const root = document.documentElement;

    if (root.dataset.appStartupGate) {
        return;
    }

    root.dataset.appStartupGate = "on";
    root.classList.add("app-starting");
    root.setAttribute("aria-busy", "true");

    let pendingPoint = null;
    let observer = null;

    const isReady = () => document.querySelector(readySelector) !== null;

    const swallow = event => {
        if (isReady() || event.target?.closest?.(exemptSelector)) {
            return;
        }

        if (event.type === "pointerdown") {
            pendingPoint = { x: event.clientX, y: event.clientY };
        }

        event.preventDefault();
        event.stopImmediatePropagation();
    };

    const gatedEvents = ["pointerdown", "mousedown", "click"];
    for (const name of gatedEvents) {
        document.addEventListener(name, swallow, true);
    }

    const release = () => {
        observer?.disconnect();
        for (const name of gatedEvents) {
            document.removeEventListener(name, swallow, true);
        }

        root.classList.remove("app-starting");
        root.removeAttribute("aria-busy");
        root.dataset.appStartupGate = "released";

        if (!pendingPoint) {
            return;
        }

        const point = pendingPoint;
        pendingPoint = null;
        // Wait one frame so the routed content has been laid out before hit-testing.
        requestAnimationFrame(() => {
            const field = document.elementFromPoint(point.x, point.y)?.closest(focusableSelector);
            if (field && document.activeElement !== field) {
                field.focus();
            }
        });
    };

    if (isReady()) {
        release();
        return;
    }

    observer = new MutationObserver(() => {
        if (isReady()) {
            release();
        }
    });
    observer.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["data-app-ready"] });
})();
