// Global SignalR + WebRTC for voice and video calls
window.MiniInstagramCallHub = {
    connection: null,
    peerConnection: null,
    localStream: null,
    currentCallId: null,
    isCaller: false,
    isVideoCall: true,
    audioOnly: false,
    pageRef: null,
    globalListenerStarted: false,
    pendingOffer: null,
    pendingIceCandidates: [],
    remoteDescriptionSet: false,

    rtcConfig: {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    },

    async getConnection() {
        if (!window.signalR) {
            throw new Error('SignalR not loaded. Refresh the page.');
        }

        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            return this.connection;
        }

        if (this.connection) {
            try { await this.connection.stop(); } catch { /* ignore */ }
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/call', { withCredentials: true })
            .withAutomaticReconnect()
            .build();

        this.connection.on('IncomingCall', (data) => this.onIncomingCall(data));
        this.connection.on('CallAccepted', (callId) => this.onCallAccepted(callId));
        this.connection.on('CallDeclined', (callId) => this.onCallDeclined(callId));
        this.connection.on('CallEnded', (callId) => this.onCallEnded(callId));
        this.connection.on('ReceiveOffer', (sdp) => this.onReceiveOffer(sdp));
        this.connection.on('ReceiveAnswer', (sdp) => this.onReceiveAnswer(sdp));
        this.connection.on('ReceiveIceCandidate', (c) => this.onReceiveIceCandidate(c));

        await this.connection.start();
        return this.connection;
    },

    async startGlobalListener() {
        if (this.globalListenerStarted) return;
        try {
            await this.getConnection();
            this.globalListenerStarted = true;
            console.info('[CallHub] Ready for incoming calls.');
            // Refresh online presence every 2 minutes so the Redis TTL never expires
            setInterval(() => this._heartbeat(), 2 * 60 * 1000);
        } catch (err) {
            console.warn('[CallHub]', err.message);
        }
    },

    async _heartbeat() {
        try {
            if (this.connection?.state === signalR.HubConnectionState.Connected) {
                await this.connection.invoke('KeepAlive');
            }
        } catch { /* ignore */ }
    },

    onIncomingCall(data) {
        const callId = data.callId || data.CallId;
        const callerUserName = data.callerUserName || data.CallerUserName || '';
        const callerDisplayName = data.callerDisplayName || data.CallerDisplayName || 'Someone';
        const isVideo = data.isVideoCall !== false && data.IsVideoCall !== false;

        this.showIncomingModal(callId, callerUserName, callerDisplayName, isVideo);

        if (window.MiniInstagramRing) {
            window.MiniInstagramRing.start(callerDisplayName);
        }
    },

    showIncomingModal(callId, callerUserName, callerDisplayName, isVideo) {
        this.hideIncomingModal();
        const typeLabel = isVideo ? 'video' : 'voice';
        const icon = isVideo ? '📹' : '📞';
        const voiceParam = isVideo ? '' : '&voice=true';

        const overlay = document.createElement('div');
        overlay.id = 'incoming-call-overlay';
        overlay.className = 'incoming-call-overlay';
        overlay.innerHTML = `
            <div class="incoming-call-modal card shadow-lg p-4 text-center">
                <div class="incoming-call-avatar mb-3"><span class="display-6">${icon}</span></div>
                <h5 class="mb-1">Incoming ${typeLabel} call</h5>
                <p class="mb-3 text-muted"><strong>${this.escapeHtml(callerDisplayName)}</strong> is calling…</p>
                <div class="d-flex gap-2 justify-content-center">
                    <button type="button" class="btn btn-success" id="btn-accept-call">Accept</button>
                    <button type="button" class="btn btn-danger" id="btn-decline-call">Decline</button>
                </div>
            </div>`;

        document.body.appendChild(overlay);

        document.getElementById('btn-accept-call').onclick = () => {
            this.hideIncomingModal();
            if (window.MiniInstagramRing) window.MiniInstagramRing.stop();
            window.location.href = `/call/${encodeURIComponent(callerUserName)}?callId=${encodeURIComponent(callId)}&incoming=true${voiceParam}`;
        };

        document.getElementById('btn-decline-call').onclick = async () => {
            this.hideIncomingModal();
            if (window.MiniInstagramRing) window.MiniInstagramRing.stop();
            try {
                await this.connection.invoke('DeclineCall', callId);
            } catch { /* ignore */ }
        };
    },

    hideIncomingModal() {
        document.getElementById('incoming-call-overlay')?.remove();
    },

    escapeHtml(text) {
        const d = document.createElement('div');
        d.textContent = text;
        return d.innerHTML;
    },

    async initCallPage(dotNetRef, callId, isCaller, targetUserId, audioOnly) {
        this.pageRef = dotNetRef;
        this.isCaller = isCaller === true || isCaller === 'true';
        this.audioOnly = audioOnly === true || audioOnly === 'true';
        this.isVideoCall = !this.audioOnly;
        this.currentCallId = callId || null;
        this.pendingOffer = null;
        this.pendingIceCandidates = [];
        this.remoteDescriptionSet = false;

        try {
            await this.setStatus('Connecting…');
            await this.getConnection();

            await this.setStatus(this.audioOnly ? 'Starting microphone…' : 'Starting camera…');
            await this.startLocalMedia();

            await this.setupPeerConnection();

            if (this.isCaller && !this.currentCallId) {
                await this.setStatus('Ringing…');
                try {
                    this.currentCallId = await this.connection.invoke('StartCall', targetUserId, this.isVideoCall);
                    await this.notifyPage('OnCallStarted', this.currentCallId);
                    await this.setStatus('Ringing… waiting for them to accept');
                } catch (err) {
                    const msg = err.message || String(err);
                    if (msg.includes('offline') || msg.includes('not available')) {
                        throw new Error('User is offline. They must be logged in with the app open on any page.');
                    }
                    throw err;
                }
            } else if (this.currentCallId && !this.isCaller) {
                await this.setStatus('Joining call…');
                await this.connection.invoke('AcceptCall', this.currentCallId);
                await this.flushPendingSignaling();
            }
        } catch (err) {
            const msg = err.message || String(err);
            await this.setStatus('Error: ' + msg);
            await this.notifyPage('OnCallError', msg);
            throw err;
        }
    },

    async setupPeerConnection() {
        this.peerConnection = new RTCPeerConnection(this.rtcConfig);

        this.localStream.getTracks().forEach((track) => {
            this.peerConnection.addTrack(track, this.localStream);
        });

        this.peerConnection.ontrack = (event) => {
            const stream = event.streams[0];
            const remoteVideo = document.getElementById('remoteVideo');
            const remoteAudio = document.getElementById('remoteAudio');
            if (remoteVideo) {
                remoteVideo.srcObject = stream;
                remoteVideo.play().catch(() => { /* ignore */ });
            }
            if (remoteAudio) {
                remoteAudio.srcObject = stream;
                remoteAudio.play().catch(() => { /* ignore */ });
            }
            this.setStatus('Connected — media active');
        };

        this.peerConnection.onicecandidate = (event) => {
            if (event.candidate && this.currentCallId) {
                this.connection.invoke('SendIceCandidate', this.currentCallId, JSON.stringify(event.candidate));
            }
        };

        this.peerConnection.onconnectionstatechange = () => {
            const state = this.peerConnection?.connectionState;
            if (state === 'connected') {
                this.setStatus('Connected');
            } else if (state === 'failed') {
                this.setStatus('Connection failed — try two different browsers or devices');
            } else if (state === 'disconnected') {
                this.setStatus('Reconnecting…');
            }
        };
    },

    async startLocalMedia() {
        if (!navigator.mediaDevices?.getUserMedia) {
            throw new Error('Microphone not available. Use HTTPS or localhost.');
        }

        const constraints = {
            audio: true,
            video: this.isVideoCall ? { facingMode: 'user' } : false
        };

        try {
            this.localStream = await navigator.mediaDevices.getUserMedia(constraints);
        } catch (err) {
            if (this.isVideoCall && err.name === 'NotReadableError') {
                throw new Error('Camera is in use (close the other call window) or denied. Try a voice call instead.');
            }
            throw err;
        }

        const localVideo = document.getElementById('localVideo');
        const localAudio = document.getElementById('localAudio');
        if (localVideo && this.isVideoCall) {
            localVideo.srcObject = this.localStream;
            localVideo.style.display = 'block';
        } else if (localVideo) {
            localVideo.style.display = 'none';
        }
        if (localAudio) {
            localAudio.srcObject = this.localStream;
            localAudio.muted = true;
        }
    },

    async onCallAccepted(callId) {
        this.currentCallId = callId;
        await this.notifyPage('OnCallAccepted', callId);

        if (this.isCaller) {
            await this.setStatus('Connected — starting video link…');
            await this.createAndSendOffer();
        } else {
            await this.setStatus('Connected — waiting for media…');
        }

        await this.flushPendingSignaling();
    },

    async onCallDeclined(callId) {
        this.cleanupPeer();
        await this.setStatus('Call declined');
        await this.notifyPage('OnCallEnded', callId, 'declined');
    },

    async onCallEnded(callId) {
        this.hideIncomingModal();
        if (window.MiniInstagramRing) window.MiniInstagramRing.stop();
        this.cleanupPeer();
        await this.setStatus('Call ended');
        await this.notifyPage('OnCallEnded', callId, 'ended');
    },

    async onReceiveOffer(sdpJson) {
        if (!this.peerConnection) {
            this.pendingOffer = sdpJson;
            return;
        }
        await this.handleOffer(sdpJson);
    },

    async onReceiveAnswer(sdpJson) {
        if (!this.peerConnection) return;
        await this.handleAnswer(sdpJson);
    },

    async onReceiveIceCandidate(candidateJson) {
        if (!this.peerConnection || !this.remoteDescriptionSet) {
            this.pendingIceCandidates.push(candidateJson);
            return;
        }
        await this.addIceCandidate(candidateJson);
    },

    async createAndSendOffer() {
        if (!this.peerConnection || !this.currentCallId) return;

        const offer = await this.peerConnection.createOffer({
            offerToReceiveAudio: true,
            offerToReceiveVideo: this.isVideoCall
        });
        await this.peerConnection.setLocalDescription(offer);
        await this.connection.invoke('SendOffer', this.currentCallId, JSON.stringify(offer));
    },

    async handleOffer(sdpJson) {
        const offer = JSON.parse(sdpJson);
        await this.peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
        this.remoteDescriptionSet = true;

        const answer = await this.peerConnection.createAnswer();
        await this.peerConnection.setLocalDescription(answer);
        await this.connection.invoke('SendAnswer', this.currentCallId, JSON.stringify(answer));

        await this.drainIceCandidates();
    },

    async handleAnswer(sdpJson) {
        const answer = JSON.parse(sdpJson);
        await this.peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
        this.remoteDescriptionSet = true;
        await this.drainIceCandidates();
        await this.setStatus('Connected');
    },

    async addIceCandidate(candidateJson) {
        try {
            const candidate = JSON.parse(candidateJson);
            await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
        } catch (e) {
            console.warn('[CallHub] ICE candidate:', e);
        }
    },

    async drainIceCandidates() {
        for (const c of this.pendingIceCandidates) {
            await this.addIceCandidate(c);
        }
        this.pendingIceCandidates = [];
    },

    async flushPendingSignaling() {
        if (this.pendingOffer) {
            const offer = this.pendingOffer;
            this.pendingOffer = null;
            await this.handleOffer(offer);
        }
    },

    async endCall() {
        if (this.currentCallId && this.connection) {
            try { await this.connection.invoke('EndCall', this.currentCallId); } catch { /* ignore */ }
        }
        this.cleanupPeer();
        if (window.MiniInstagramRing) window.MiniInstagramRing.stop();
    },

    cleanupPeer() {
        if (this.peerConnection) {
            this.peerConnection.close();
            this.peerConnection = null;
        }
        if (this.localStream) {
            this.localStream.getTracks().forEach((t) => t.stop());
            this.localStream = null;
        }
        ['localVideo', 'remoteVideo'].forEach((id) => {
            const el = document.getElementById(id);
            if (el) { el.srcObject = null; }
        });
        this.currentCallId = null;
        this.pageRef = null;
        this.isCaller = false;
        this.pendingOffer = null;
        this.pendingIceCandidates = [];
        this.remoteDescriptionSet = false;
    },

    async setStatus(text) {
        const el = document.getElementById('call-status-text');
        if (el) el.textContent = text;
        await this.notifyPage('OnStatus', text);
    },

    async notifyPage(method, ...args) {
        if (!this.pageRef) return;
        try {
            await this.pageRef.invokeMethodAsync(method, ...args);
        } catch { /* page gone */ }
    }
};

(function () {
    function tryStart() {
        window.MiniInstagramCallHub?.startGlobalListener();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryStart);
    } else {
        tryStart();
    }
})();
