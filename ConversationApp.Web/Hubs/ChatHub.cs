using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ConversationApp.Entity.Entites;
using ConversationApp.Data.Context;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ConversationApp.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            if (user != null)
            {
                // Veritabanına kaydet
                var newMessage = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = Guid.Parse(conversationId),
                    UserId = user.Id,
                    Content = message,
                    SentDate = DateTime.UtcNow
                };

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();

                // Tüm grup üyelerine gönder
                await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", new
                {
                    senderName = user.UserName,
                    content = message,
                    sentDate = newMessage.SentDate.ToString("dd.MM.yyyy HH:mm"),
                    isOutgoing = false
                });
            }
        }

        public async Task StartPrivateConversation(string targetUsername, string message)
        {
            var currentUser = await _userManager.GetUserAsync(Context.User);
            var targetUser = await _userManager.FindByNameAsync(targetUsername);

            if (currentUser != null && targetUser != null)
            {
                // Mevcut konuşma var mı kontrol et
                var existingConversation = await _context.ConversationParticipants
                    .Include(cp => cp.Conversation)
                        .ThenInclude(c => c.Participants)
                    .Where(cp => cp.UserId == currentUser.Id)
                    .Select(cp => cp.Conversation)
                    .Where(c => c.Type == 0 && // Private conversation
                                c.Participants.Count == 2 &&
                                c.Participants.Any(p => p.UserId == targetUser.Id))
                    .FirstOrDefaultAsync();

                ConversationApp.Entity.Entites.Conversation conversation;

                if (existingConversation == null)
                {
                    // Yeni konuşma oluştur
                    conversation = new ConversationApp.Entity.Entites.Conversation
                    {
                        Id = Guid.NewGuid(),
                        Title = $"{currentUser.UserName} - {targetUser.UserName}",
                        Type = 0, // Private
                        CreationDate = DateTime.UtcNow
                    };

                    _context.Conversations.Add(conversation);

                    // Katılımcıları ekle
                    _context.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = currentUser.Id,
                        JoinedDate = DateTime.UtcNow
                    });

                    _context.ConversationParticipants.Add(new ConversationParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = targetUser.Id,
                        JoinedDate = DateTime.UtcNow
                    });
                }
                else
                {
                    conversation = existingConversation;
                }

                // Mesajı kaydet
                var newMessage = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = currentUser.Id,
                    Content = message,
                    SentDate = DateTime.UtcNow
                };

                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();

                // Her iki kullanıcıya da bildir
                await Clients.Group($"user_{currentUser.Id}").SendAsync("NewConversationStarted", new
                {
                    conversationId = conversation.Id.ToString(),
                    otherUserName = targetUser.UserName,
                    message = new
                    {
                        senderName = currentUser.UserName,
                        content = message,
                        sentDate = newMessage.SentDate.ToString("dd.MM.yyyy HH:mm"),
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
                        sentDate = newMessage.SentDate.ToString("dd.MM.yyyy HH:mm"),
                        isOutgoing = false
                    }
                });
            }
        }
    }
} 