using ConversationApp.Entity.Entites;
using Conversation.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Interfaces
{
    public interface IMessageService
    {
        Task<Message> SendMessageAsync(Guid conversationId, Guid senderId, string content);
        Task<List<MessageViewModel>> GetConversationMessagesAsync(Guid conversationId, Guid currentUserId);
        Task<List<MessageViewModel>> GetConversationMessagesPagedAsync(Guid conversationId, Guid currentUserId, int page, int pageSize);
        Task<Message> GetLastMessageInConversationAsync(Guid conversationId);
        Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId);
        Task MarkConversationAsReadAsync(Guid conversationId, Guid userId);
        Task<List<Message>> SearchMessagesInConversationAsync(Guid conversationId, string searchTerm);

        // Admin Dashboard Ýstatistikleri
        Task<List<int>> GetMonthlyMessageCountsAsync();
        Task<int> GetTotalMessagesCountAsync();
        Task<double> GetMessageGrowthPercentageAsync(int days = 30);
    }
} 