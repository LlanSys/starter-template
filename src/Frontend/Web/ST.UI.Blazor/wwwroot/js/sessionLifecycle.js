const activityEvents = ["keydown", "mousedown", "mousemove", "touchstart", "scroll", "visibilitychange"];

let currentDotNetRef = null;
let currentHandler = null;
let lastNotificationAt = 0;

export function start(dotNetRef) {
    stop();

    currentDotNetRef = dotNetRef;
    currentHandler = () => {
        const now = Date.now();
        if (now - lastNotificationAt < 1000) {
            return;
        }

        lastNotificationAt = now;
        currentDotNetRef.invokeMethodAsync("NotifyActivityAsync").catch(() => { });
    };

    for (const eventName of activityEvents) {
        window.addEventListener(eventName, currentHandler, { passive: true });
    }
}

export function stop() {
    if (!currentHandler) {
        return;
    }

    for (const eventName of activityEvents) {
        window.removeEventListener(eventName, currentHandler);
    }

    currentHandler = null;
    currentDotNetRef = null;
    lastNotificationAt = 0;
}
