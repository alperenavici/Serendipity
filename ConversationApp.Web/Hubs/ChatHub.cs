using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using System;
using System.Threading.Tasks;


namespace ConversationApp.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly UserManager<User> _userManager;
        private readonly IConversationService _conversationService;
        private readonly IMessageService _messageService;

        public ChatHub(
            UserManager<User> userManager,
            IConversationService conversationService,
            IMessageService messageService)
        {
            _userManager = userManager;
            _conversationService = conversationService;
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{user.Id}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task SendMessage(string conversationId, string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            if (!Guid.TryParse(conversationId, out var conversationGuid)) return;

            var isParticipant = await _conversationService.IsUserParticipantAsync(conversationGuid, user.Id);
            if (!isParticipant) return;

            var newMessage = await _messageService.SendMessageAsync(conversationGuid, user.Id, message);

            // Gruptaki DÝÐER kiþilere "gelen" mesaj olarak gönder
            await Clients.OthersInGroup($"conversation_{conversationId}").SendAsync("ReceiveMessage", new
            {
                senderName = user.UserName,
                content = newMessage.Content,
                sentDate = newMessage.SentDate.ToString("dd.MM.yyyy HH:mm"),
                isOutgoing = false
            });

            // Mesajý SADECE GÖNDEREN kiþiye "giden" mesaj olarak gönder
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                senderName = user.UserName,
                content = newMessage.Content,
                sentDate = newMessage.SentDate.ToString("dd.MM.yyyy HH:mm"),
                isOutgoing = true
            });
        }

        public async Task StartPrivateConversation(string targetUsername, string message)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            if (currentUser == null) return;

            try
            {
                var targetUser = await _userManager.FindByNameAsync(targetUsername);
                if (targetUser == null) return;

                var conversation = await _conversationService.CreatePrivateConversationAsync(currentUser.Id, targetUser.Id, message);

                await Clients.Group($"user_{currentUser.Id}").SendAsync("NewConversationStarted", new
                {
                    conversationId = conversation.Id.ToString(),
                    otherUserName = targetUser.UserName,
                    message = new
                    {
                        senderName = currentUser.UserName,
                        content = message,
                        sentDate = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                        isOutgoing = true
                    }
                });

                await Clients.Group($"user_{targetUser.Id}").SendAsync("NewConversationStarted", new
                {
                    conversationId = conversation.Id.ToString(),
                    otherUserName = currentUser.UserName,
                    message = new
                    {
                        senderName = currentUser.UserName,
                        content = message,
                        sentDate = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm"),
                        isOutgoing = false
                    }
                });
            }
            catch (Exception)
            {
            }
        }
    }
} 