using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudySync.Data;
using StudySync.Models;
using System.Security.Claims;

namespace StudySync.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinChannel(string channelId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            if (int.TryParse(channelId, out int cId))
            {
                var channel = await _context.CommunityChannels.FindAsync(cId);
                // Check Authorization: If it's a University tier, user must belong to that university
                if (channel != null)
                {
                    bool authorized = true;
                    if (channel.Tier == ChannelTier.University && !string.Equals(channel.AssociatedEntityName, user.UniversityName, StringComparison.OrdinalIgnoreCase))
                        authorized = false;
                    if (channel.Tier == ChannelTier.Major && !string.Equals(channel.AssociatedEntityName, user.Major, StringComparison.OrdinalIgnoreCase))
                        authorized = false;
                    
                    if (authorized)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
                    }
                }
            }
        }

        public async Task LeaveChannel(string channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        }

        public async Task SendMessage(string channelId, string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || string.IsNullOrWhiteSpace(message)) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBanned) return;

            if (int.TryParse(channelId, out int cId))
            {
                var channel = await _context.CommunityChannels.FindAsync(cId);
                if (channel != null)
                {
                    // Create and save message
                    var chatMessage = new ChatMessage
                    {
                        ApplicationUserId = userId,
                        CommunityChannelId = cId,
                        Text = message,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ChatMessages.Add(chatMessage);
                    await _context.SaveChangesAsync();

                    // Broadcast to group
                    var senderInitial = user.FullName.Substring(0, 1).ToUpper();
                    await Clients.Group(channelId).SendAsync("ReceiveMessage", user.FullName, senderInitial, message, chatMessage.CreatedAt.ToString("t"));
                }
            }
        }

        // --- WebRTC Signaling Methods ---

        public async Task JoinVideoRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "video_" + roomId);
            // Notify others in room that a new peer joined
            await Clients.OthersInGroup("video_" + roomId).SendAsync("PeerJoined", Context.ConnectionId);
        }

        public async Task SendOffer(string targetConnectionId, string sdpOffer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, sdpOffer);
        }

        public async Task SendAnswer(string targetConnectionId, string sdpAnswer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, sdpAnswer);
        }

        public async Task SendIceCandidate(string targetConnectionId, string candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        // --- Room Session Chat (ephemeral, no persistence) ---

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "room_" + roomId);
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                    await Clients.OthersInGroup("room_" + roomId)
                        .SendAsync("RoomUserJoined", user.FullName);
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "room_" + roomId);
        }

        public async Task SendRoomMessage(string roomId, string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || string.IsNullOrWhiteSpace(message)) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            var initial = user.FullName.Substring(0, 1).ToUpper();
            var time = DateTime.Now.ToString("t");

            await Clients.Group("room_" + roomId)
                .SendAsync("ReceiveRoomMessage", user.FullName, initial, message, time);
        }

        // --- Live Session Methods ---

        public async Task JoinSession(string sessionId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, "session_" + sessionId);

            // Notify others that someone joined the session
            var initial = user.FullName.Substring(0, 1).ToUpper();
            await Clients.OthersInGroup("session_" + sessionId)
                .SendAsync("SessionUserJoined", user.FullName, initial);
        }

        public async Task LeaveSession(string sessionId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    await Clients.OthersInGroup("session_" + sessionId)
                        .SendAsync("SessionUserLeft", user.FullName);
                }
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "session_" + sessionId);
        }

        public async Task SendSessionMessage(string sessionId, string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || string.IsNullOrWhiteSpace(message)) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBanned) return;

            var initial = user.FullName.Substring(0, 1).ToUpper();
            var time = DateTime.Now.ToString("t");

            await Clients.Group("session_" + sessionId)
                .SendAsync("ReceiveSessionMessage", user.FullName, initial, message, time);
        }

        // --- Session Screen Share Signaling ---

        public async Task JoinSessionVideo(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "session_video_" + sessionId);
            // Notify others (especially the tutor) that a new viewer joined
            await Clients.OthersInGroup("session_video_" + sessionId).SendAsync("SessionPeerJoined", Context.ConnectionId);
        }

        public async Task LeaveSessionVideo(string sessionId)
        {
            await Clients.OthersInGroup("session_video_" + sessionId).SendAsync("SessionPeerLeft", Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "session_video_" + sessionId);
        }

        public async Task SessionSendOffer(string targetConnectionId, string sdpOffer)
        {
            await Clients.Client(targetConnectionId).SendAsync("SessionReceiveOffer", Context.ConnectionId, sdpOffer);
        }

        public async Task SessionSendAnswer(string targetConnectionId, string sdpAnswer)
        {
            await Clients.Client(targetConnectionId).SendAsync("SessionReceiveAnswer", Context.ConnectionId, sdpAnswer);
        }

        public async Task SessionSendIceCandidate(string targetConnectionId, string candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("SessionReceiveIceCandidate", Context.ConnectionId, candidate);
        }

        // Tutor notifies all attendees that screen sharing has started
        public async Task SessionScreenShareStarted(string sessionId)
        {
            await Clients.OthersInGroup("session_video_" + sessionId).SendAsync("ScreenShareStarted", Context.ConnectionId);
        }

        // Viewer requests video from the tutor when they join late or when tutor starts sharing
        public async Task SessionViewerRequestVideo(string tutorConnectionId)
        {
            await Clients.Client(tutorConnectionId).SendAsync("SessionPeerJoined", Context.ConnectionId);
        }

        // Tutor notifies all attendees that screen sharing has stopped
        public async Task SessionScreenShareStopped(string sessionId)
        {
            await Clients.OthersInGroup("session_video_" + sessionId).SendAsync("ScreenShareStopped");
        }
    }
}
