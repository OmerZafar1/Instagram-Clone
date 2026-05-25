// Browser audio recording using MediaRecorder
window.MiniInstagramVoice = {
    mediaRecorder: null,
    chunks: [],
    stream: null,
    startedAt: 0,

    async start() {
        if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
            throw new Error('Already recording.');
        }

        this.chunks = [];
        this.stream = await navigator.mediaDevices.getUserMedia({ audio: true });

        const mime = this.pickMimeType();
        this.mediaRecorder = mime
            ? new MediaRecorder(this.stream, { mimeType: mime })
            : new MediaRecorder(this.stream);

        this.mediaRecorder.ondataavailable = (e) => {
            if (e.data && e.data.size > 0) {
                this.chunks.push(e.data);
            }
        };

        this.startedAt = Date.now();
        this.mediaRecorder.start();
    },

    pickMimeType() {
        const candidates = [
            'audio/webm;codecs=opus',
            'audio/webm',
            'audio/mp4',
            'audio/ogg'
        ];
        for (const m of candidates) {
            if (window.MediaRecorder && MediaRecorder.isTypeSupported(m)) {
                return m;
            }
        }
        return null;
    },

    async stopAndUpload(conversationId, antiforgeryToken) {
        if (!this.mediaRecorder) {
            throw new Error('No active recording.');
        }

        const durationSeconds = Math.max(1, Math.round((Date.now() - this.startedAt) / 1000));

        const blob = await new Promise((resolve) => {
            this.mediaRecorder.onstop = () => {
                const type = this.mediaRecorder.mimeType || 'audio/webm';
                resolve(new Blob(this.chunks, { type }));
            };
            this.mediaRecorder.stop();
        });

        this.stopTracks();

        const extension = this.extensionFor(blob.type);
        const form = new FormData();
        form.append('conversationId', String(conversationId));
        form.append('durationSeconds', String(durationSeconds));
        form.append('audio', blob, `voice-${Date.now()}.${extension}`);

        const headers = {};
        if (antiforgeryToken) {
            headers['RequestVerificationToken'] = antiforgeryToken;
        }

        const response = await fetch('/api/chat/voice', {
            method: 'POST',
            body: form,
            headers,
            credentials: 'same-origin'
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || 'Failed to upload voice message.');
        }

        return await response.json();
    },

    cancel() {
        if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
            try { this.mediaRecorder.stop(); } catch { /* ignore */ }
        }
        this.stopTracks();
        this.chunks = [];
    },

    stopTracks() {
        if (this.stream) {
            this.stream.getTracks().forEach((t) => t.stop());
            this.stream = null;
        }
    },

    extensionFor(mime) {
        if (!mime) return 'webm';
        if (mime.includes('mp4')) return 'm4a';
        if (mime.includes('ogg')) return 'ogg';
        if (mime.includes('wav')) return 'wav';
        if (mime.includes('mpeg')) return 'mp3';
        return 'webm';
    }
};
