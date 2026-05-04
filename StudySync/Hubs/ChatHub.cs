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
    }
}
