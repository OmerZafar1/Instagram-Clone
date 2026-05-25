// Global SignalR listener for real-time notifications
window.MiniInstagramNotifications = {
    connection: null,
    dotNetRef: null,
    started: false,

    async start(dotNetRef) {
        if (!window.signalR) return;
        this.dotNetRef = dotNetRef;

        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications', { withCredentials: true })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveNotification', (n) => this.onNotification(n));
        this.connection.on('UnreadCountChanged', (count) => this.onUnreadCount(count));

        try {
            await this.connection.start();
            this.started = true;
            console.info('[Notifications] Connected.');
        } catch (err) {
            console.warn('[Notifications]', err.message);
        }
    },

    async onNotification(notification) {
        if (this.dotNetRef) {
            try {
                await this.dotNetRef.invokeMethodAsync('OnNewNotification', notification);
            } catch { /* page gone */ }
        }

        // Browser toast when tab is in background
        if (document.hidden && 'Notification' in window && Notification.permission === 'granted') {
            const actor = notification.actorUserName || notification.ActorUserName || 'Someone';
            const msg = notification.message || notification.Message || '';
            new Notification(actor, { body: msg, icon: '/favicon.png' });
        }
    },

    async onUnreadCount(count) {
        if (!this.dotNetRef) return;
        try {
            await this.dotNetRef.invokeMethodAsync('OnUnreadCount', count);
        } catch { /* ignore */ }
    },

    async requestBrowserPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            await Notification.requestPermission();
        }
    }
};

(function () {
    function tryPermission() {
        window.MiniInstagramNotifications?.requestBrowserPermission();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryPermission);
    } else {
        tryPermission();
    }
})();
