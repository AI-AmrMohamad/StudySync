document.addEventListener("DOMContentLoaded", async () => {
    const roomId = document.getElementById('roomIdField')?.value;
    if (!roomId) return;

    // UI Elements
    const mainScreen = document.getElementById('mainScreen');
    const noStreamPlaceholder = document.getElementById('noStreamPlaceholder');
    const btnShareScreen = document.getElementById('btnShareScreen');
    const btnStopShare = document.getElementById('btnStopShare');
    const pingMetric = document.getElementById('pingMetric');

    // WebRTC Core
    let localStream = null;
    let peers = {}; // Connection ID -> RTCPeerConnection map
    const stunServers = {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    };

    // SignalR Setup
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    // 1. Peer Joined -> We create offer if we are sharing
    connection.on("PeerJoined", async (peerConnectionId) => {
        console.log("New peer joined:", peerConnectionId);
        if (localStream) {
            // We are streaming, so let's negotiate with the newborn peer
            const peerConnection = createPeerConnection(peerConnectionId);
            localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));
            
            const offer = await peerConnection.createOffer();
            await peerConnection.setLocalDescription(offer);
            
            connection.invoke("SendOffer", peerConnectionId, JSON.stringify(offer));
        }
    });

    // 2. Receive Offer -> We create answer
    connection.on("ReceiveOffer", async (senderConnectionId, offerStr) => {
        console.log("Offer received from:", senderConnectionId);
        const offer = JSON.parse(offerStr);
        const peerConnection = createPeerConnection(senderConnectionId);
        
        await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
        
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        
        connection.invoke("SendAnswer", senderConnectionId, JSON.stringify(answer));
    });

    // 3. Receive Answer -> Finalize handshake
    connection.on("ReceiveAnswer", async (senderConnectionId, answerStr) => {
        console.log("Answer received from:", senderConnectionId);
        const answer = JSON.parse(answerStr);
        if (peers[senderConnectionId]) {
            await peers[senderConnectionId].setRemoteDescription(new RTCSessionDescription(answer));
        }
    });

    // 4. Receive ICE Candidate -> Add to pipeline
    connection.on("ReceiveIceCandidate", async (senderConnectionId, candidateStr) => {
        const candidate = JSON.parse(candidateStr);
        if (peers[senderConnectionId]) {
            await peers[senderConnectionId].addIceCandidate(new RTCIceCandidate(candidate));
        }
    });

    // Start signaling connect
    try {
        await connection.start();
        console.log("SignalR Connected. Joining room:", roomId);
        await connection.invoke("JoinVideoRoom", roomId);
        pingMetric.innerText = "24ms"; // Simulated ping display once connected to signaling
    } catch (err) {
        console.error("SignalR Connection Error:", err);
    }

    // WebRTC Logic Wrapper
    function createPeerConnection(peerIdentifier) {
        if (peers[peerIdentifier]) return peers[peerIdentifier];

        const pc = new RTCPeerConnection(stunServers);
        peers[peerIdentifier] = pc;

        // Ice routing
        pc.onicecandidate = event => {
            if (event.candidate) {
                connection.invoke("SendIceCandidate", peerIdentifier, JSON.stringify(event.candidate));
            }
        };

        // Track receiver (When viewing someone else's stream)
        pc.ontrack = event => {
            mainScreen.srcObject = event.streams[0];
            mainScreen.style.display = "block";
            noStreamPlaceholder.style.display = "none";
            
            btnShareScreen.style.display = "none"; // Hide share button if someone else is sharing
        };

        // State changes
        pc.oniceconnectionstatechange = () => {
            if (pc.iceConnectionState === "disconnected" || pc.iceConnectionState === "failed" || pc.iceConnectionState === "closed") {
                delete peers[peerIdentifier];
                if (mainScreen.srcObject && Object.keys(peers).length === 0 && !localStream) {
                    mainScreen.style.display = "none";
                    noStreamPlaceholder.style.display = "block";
                    btnShareScreen.style.display = "inline-flex";
                }
            }
        };

        return pc;
    }

    // Share Screen Request
    if (btnShareScreen) {
        btnShareScreen.addEventListener("click", async () => {
            try {
                localStream = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true });
                
                mainScreen.srcObject = localStream;
                mainScreen.style.display = "block";
                noStreamPlaceholder.style.display = "none";
                
                btnShareScreen.style.display = "none";
                btnStopShare.style.display = "inline-flex";

                // Re-broadcast tracking logic stops when user hits browser 'Stop sharing'
                localStream.getVideoTracks()[0].onended = () => {
                    stopSharing();
                };

            } catch (err) {
                console.error("Error securing display media:", err);
                alert("Screen sharing requires permissions to execute P2P protocols.");
            }
        });
    }

    // Stop Sharing
    if (btnStopShare) {
        btnStopShare.addEventListener("click", stopSharing);
    }

    function stopSharing() {
        if (localStream) {
            localStream.getTracks().forEach(t => t.stop());
            localStream = null;
        }

        mainScreen.style.display = "none";
        noStreamPlaceholder.style.display = "block";
        
        btnStopShare.style.display = "none";
        btnShareScreen.style.display = "inline-flex";

        // Kill outgoing peer tracks (simplified cleanup for naive P2P mesh)
        for (let connId in peers) {
            peers[connId].close();
            delete peers[connId];
        }
        
        // Let others know we are done by basically not sending tracks anymore
        // For production, send an explicit "EndStream" signaling event
    }
});
