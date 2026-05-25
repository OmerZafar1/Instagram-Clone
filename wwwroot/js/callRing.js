// Ringtone + browser notification helper for incoming calls
window.MiniInstagramRing = {
    audioCtx: null,
    oscillator: null,
    gainNode: null,
    intervalId: null,
    originalTitle: null,
    titleFlashId: null,
    activeNotification: null,

    async requestPermission() {
        if (!('Notification' in window)) return false;
        if (Notification.permission === 'granted') return true;
        if (Notification.permission === 'denied') return false;
        const result = await Notification.requestPermission();
        return result === 'granted';
    },

    start(callerName) {
        this.stop();

        try {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            if (Ctx) {
                this.audioCtx = new Ctx();
                this.playRingPattern();
                this.intervalId = setInterval(() => this.playRingPattern(), 2000);
            }
        } catch {
            /* audio not allowed */
        }

        // Flash the tab title so the user notices when on another tab
        if (!this.originalTitle) {
            this.originalTitle = document.title;
        }
        let flash = false;
        this.titleFlashId = setInterval(() => {
            flash = !flash;
            document.title = flash ? `📞 Incoming call — ${callerName}` : this.originalTitle;
        }, 800);

        // Browser notification (works even if tab is not focused)
        if ('Notification' in window && Notification.permission === 'granted') {
            try {
                this.activeNotification = new Notification('Incoming video call', {
                    body: `${callerName} is calling you`,
                    tag: 'mini-instagram-call',
                    requireInteraction: true
                });
                this.activeNotification.onclick = () => {
                    window.focus();
                    if (this.activeNotification) this.activeNotification.close();
                };
            } catch {
                /* notifications may be disabled */
            }
        }
    },

    playRingPattern() {
        if (!this.audioCtx) return;

        const now = this.audioCtx.currentTime;
        this.playTone(now, 0.4, 480);
        this.playTone(now + 0.5, 0.4, 620);
    },

    playTone(startTime, duration, frequency) {
        if (!this.audioCtx) return;

        const osc = this.audioCtx.createOscillator();
        const gain = this.audioCtx.createGain();

        osc.type = 'sine';
        osc.frequency.value = frequency;

        gain.gain.setValueAtTime(0, startTime);
        gain.gain.linearRampToValueAtTime(0.18, startTime + 0.05);
        gain.gain.linearRampToValueAtTime(0, startTime + duration);

        osc.connect(gain);
        gain.connect(this.audioCtx.destination);
        osc.start(startTime);
        osc.stop(startTime + duration);
    },

    stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
        if (this.titleFlashId) {
            clearInterval(this.titleFlashId);
            this.titleFlashId = null;
            if (this.originalTitle) {
                document.title = this.originalTitle;
            }
        }
        if (this.audioCtx) {
            try { this.audioCtx.close(); } catch { /* ignore */ }
            this.audioCtx = null;
        }
        if (this.activeNotification) {
            try { this.activeNotification.close(); } catch { /* ignore */ }
            this.activeNotification = null;
        }
    }
};
