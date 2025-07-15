using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<List<Conversation>> GetUserConversationsAsync(Guid userId);
        Task<List<Conversation>> GetUserConversationsWithMessagesAsync(Guid userId);
        Task<Conversation> GetConversationWithParticipantsAsync(Guid conversationId);
        Task<Conversation> GetConversationWithMessagesAsync(Guid conversationId);
        Task<Conversation> GetPrivateConversationBetweenUsersAsync(Guid user1Id, Guid user2Id);
        Task<List<Conversation>> SearchConversationsAsync(Guid userId, string searchTerm);
        Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId);
    }
} 