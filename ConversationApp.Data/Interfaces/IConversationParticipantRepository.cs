using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IConversationParticipantRepository : IGenericRepository<ConversationParticipant>
    {
        Task<List<ConversationParticipant>> GetUserParticipationsAsync(Guid userId);
        Task<List<ConversationParticipant>> GetConversationParticipantsAsync(Guid conversationId);
        Task<ConversationParticipant> GetParticipantAsync(Guid conversationId, Guid userId);
        Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId);
        Task<List<ConversationParticipant>> GetActiveParticipantsAsync(Guid conversationId);
        Task<List<User>> GetOtherParticipantsAsync(Guid conversationId, Guid currentUserId);
        Task<int> GetParticipantCountAsync(Guid conversationId);
    }
} 