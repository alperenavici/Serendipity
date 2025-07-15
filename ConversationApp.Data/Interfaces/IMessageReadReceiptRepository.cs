using ConversationApp.Entity.Entites;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConversationApp.Data.Interfaces
{
    public interface IMessageReadReceiptRepository : IGenericRepository<MessageReadReceipt>
    {
        Task<List<MessageReadReceipt>> GetMessageReadReceiptsAsync(Guid messageId);
        Task<List<MessageReadReceipt>> GetUserReadReceiptsAsync(Guid userId);
        Task<MessageReadReceipt> GetUserMessageReadReceiptAsync(Guid messageId, Guid userId);
        Task<bool> HasUserReadMessageAsync(Guid messageId, Guid userId);
        Task<List<Message>> GetUnreadMessagesForUserAsync(Guid userId, Guid conversationId);
        Task MarkMessageAsReadAsync(Guid messageId, Guid userId);
        Task MarkConversationMessagesAsReadAsync(Guid conversationId, Guid userId);
    }
} 