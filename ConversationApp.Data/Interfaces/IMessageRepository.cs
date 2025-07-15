using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<List<Message>> GetConversationMessagesAsync(Guid conversationId);
        Task<List<Message>> GetConversationMessagesPagedAsync(Guid conversationId, int page, int pageSize);
        Task<List<Message>> GetUserMessagesAsync(Guid userId);
        Task<Message> GetLastMessageInConversationAsync(Guid conversationId);
        Task<List<Message>> GetUnreadMessagesAsync(Guid conversationId, Guid userId);
        Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId);
        Task<List<Message>> SearchMessagesInConversationAsync(Guid conversationId, string searchTerm);
    }
} 