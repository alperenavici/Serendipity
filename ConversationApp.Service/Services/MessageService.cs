using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using ConversationApp.Service.Interfaces;
using ConversationCore = Conversation.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Service.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MessageService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Message> SendMessageAsync(Guid conversationId, Guid senderId, string content)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = senderId,
                Content = content,
                SentDate = DateTime.UtcNow
            };

            _unitOfWork.Messages.Add(message);
            await _unitOfWork.SaveChangesAsync();

            return message;
        }

        public async Task<Message> SendSystemMessageAsync(Guid targetUserId, string title, string content)
        {
            // Sistem kullanıcısı için sabit GUID (00000000-0000-0000-0000-000000000001)
            var systemUserId = new Guid("00000000-0000-0000-0000-000000000001");
            
            // Sistem kullanıcısını kontrol et/oluştur
            var systemUser = await _unitOfWork.Users.GetByIdAsync(systemUserId);
            if (systemUser == null)
            {
                // Sistem kullanıcısını oluştur
                systemUser = new User
                {
                    Id = systemUserId,
                    UserName = "System",
                    NormalizedUserName = "SYSTEM",
                    Email = "system@serendipity.com",
                    NormalizedEmail = "SYSTEM@SERENDIPITY.COM",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAEAACcQAAAAEDummyPasswordHashForSystemUser", // Dummy hash for system user
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreationDate = DateTime.UtcNow,
                    Role = 1, // Admin
                    IsBanned = false,
                    IsDeleted = false
                };
                
                _unitOfWork.Users.Add(systemUser);
                await _unitOfWork.SaveChangesAsync();
            }

            // Sistem ile hedef kullanıcı arasında conversation bul/oluştur
            var conversation = await FindOrCreateSystemConversationAsync(systemUserId, targetUserId);
            
            // Başlık ve içeriği birleştir
            var fullContent = string.IsNullOrEmpty(title) ? content : $"📬 {title}\n\n{content}";
            
            // Mesajı gönder
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = systemUserId,
                Content = fullContent,
                SentDate = DateTime.UtcNow
            };

            _unitOfWork.Messages.Add(message);
            await _unitOfWork.SaveChangesAsync();

            return message;
        }

        private async Task<ConversationApp.Entity.Entites.Conversation> FindOrCreateSystemConversationAsync(Guid systemUserId, Guid targetUserId)
        {
            // Sistem kullanıcısı ile hedef kullanıcı arasında mevcut conversation'ı bul
            var existingConversation = await _unitOfWork.Conversations.GetPrivateConversationBetweenUsersAsync(systemUserId, targetUserId);
            
            if (existingConversation != null)
            {
                return existingConversation;
            }

            // Yeni conversation oluştur
            var conversation = new Entity.Entites.Conversation
            {
                Id = Guid.NewGuid(),
                Title = $"System - {await GetUserNameAsync(targetUserId)}",
                Type = 0, // 0: Private, 1: Group
                CreationDate = DateTime.UtcNow
            };

            _unitOfWork.Conversations.Add(conversation);

            // Sistem kullanıcısını participant olarak ekle
            var systemParticipant = new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = systemUserId,
                JoinedDate = DateTime.UtcNow
            };

            // Hedef kullanıcıyı participant olarak ekle
            var targetParticipant = new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = targetUserId,
                JoinedDate = DateTime.UtcNow
            };

            _unitOfWork.ConversationParticipants.Add(systemParticipant);
            _unitOfWork.ConversationParticipants.Add(targetParticipant);

            await _unitOfWork.SaveChangesAsync();

            return conversation;
        }

        private async Task<string> GetUserNameAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user?.UserName ?? "Unknown User";
        }

        public async Task<List<ConversationCore.MessageViewModel>> GetConversationMessagesAsync(Guid conversationId, Guid currentUserId)
        {
            var messages = await _unitOfWork.Messages.GetConversationMessagesAsync(conversationId);

            return messages.Select(m => new ConversationCore.MessageViewModel
            {
                SenderName = m.Sender.UserName,
                Content = m.Content,
                SentDate = m.SentDate,
                IsOutgoing = m.UserId == currentUserId
            }).ToList();
        }

        public async Task<List<ConversationCore.MessageViewModel>> GetConversationMessagesPagedAsync(Guid conversationId, Guid currentUserId, int page, int pageSize)
        {
            var messages = await _unitOfWork.Messages.GetConversationMessagesPagedAsync(conversationId, page, pageSize);

            return messages.Select(m => new ConversationCore.MessageViewModel
            {
                SenderName = m.Sender.UserName,
                Content = m.Content,
                SentDate = m.SentDate,
                IsOutgoing = m.UserId == currentUserId
            }).ToList();
        }

        public async Task<Message> GetLastMessageInConversationAsync(Guid conversationId)
        {
            return await _unitOfWork.Messages.GetLastMessageInConversationAsync(conversationId);
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId)
        {
            return await _unitOfWork.Messages.GetUnreadMessageCountAsync(conversationId, userId);
        }

        public async Task MarkConversationAsReadAsync(Guid conversationId, Guid userId)
        {
            await _unitOfWork.MessageReadReceipts.MarkConversationMessagesAsReadAsync(conversationId, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<Message>> SearchMessagesInConversationAsync(Guid conversationId, string searchTerm)
        {
            return await _unitOfWork.Messages.SearchMessagesInConversationAsync(conversationId, searchTerm);
        }

        // Admin Dashboard �statistikleri
        public async Task<List<int>> GetMonthlyMessageCountsAsync()
        {
            return await _unitOfWork.Messages.GetMonthlyMessageCountsAsync();
        }

        public async Task<int> GetTotalMessagesCountAsync()
        {
            return await _unitOfWork.Messages.GetTotalMessagesCountAsync();
        }

        public async Task<double> GetMessageGrowthPercentageAsync(int days = 30)
        {
            var currentPeriodCount = await _unitOfWork.Messages.GetMessagesCountAsync(days);
            var previousPeriodCount = await _unitOfWork.Messages.GetMessagesCountAsync(days * 2) - currentPeriodCount;

            if (previousPeriodCount == 0)
                return currentPeriodCount > 0 ? 100 : 0;

            return ((double)(currentPeriodCount - previousPeriodCount) / previousPeriodCount) * 100;
        }
    }
} 