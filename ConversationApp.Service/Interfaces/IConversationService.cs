using ConversationApp.Entity.Entites;
using Conversation.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Service.Interfaces
{
    public interface IConversationService
    {
        Task<List<ConversationListItemViewModel>> GetUserConversationsAsync(Guid userId, string searchQuery = null);
        Task<ConversationViewModel> GetConversationViewModelAsync(Guid userId, Guid? conversationId, string searchQuery = null);
        Task<ConversationApp.Entity.Entites.Conversation> CreatePrivateConversationAsync(Guid currentUserId, Guid targetUserId, string initialMessage);
        Task<ConversationApp.Entity.Entites.Conversation> GetOrCreatePrivateConversationAsync(Guid user1Id, Guid user2Id);
        Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId);
        Task<List<User>> GetConversationParticipantsAsync(Guid conversationId, Guid excludeUserId);
        Task<Message> SendMessageAsync(Guid conversationId, Guid senderId, string content);
        Task<List<MessageViewModel>> GetConversationMessagesAsync(Guid conversationId, Guid currentUserId);
    }
} 