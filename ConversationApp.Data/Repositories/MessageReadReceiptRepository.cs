using ConversationApp.Data.Context;
using ConversationApp.Data.Interfaces;
using ConversationApp.Entity.Entites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationApp.Data.Repositories
{
    public class MessageReadReceiptRepository : GenericRepository<MessageReadReceipt>, IMessageReadReceiptRepository
    {
        public MessageReadReceiptRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<MessageReadReceipt>> GetMessageReadReceiptsAsync(Guid messageId)
        {
            return await _context.MessageReadReceipts
                .Include(mrr => mrr.User)
                .Include(mrr => mrr.Message)
                .Where(mrr => mrr.MessageId == messageId)
                .ToListAsync();
        }

        public async Task<List<MessageReadReceipt>> GetUserReadReceiptsAsync(Guid userId)
        {
            return await _context.MessageReadReceipts
                .Include(mrr => mrr.Message)
                    .ThenInclude(m => m.Conversation)
                .Where(mrr => mrr.UserId == userId)
                .OrderByDescending(mrr => mrr.ReadDate)
                .ToListAsync();
        }

        public async Task<MessageReadReceipt> GetUserMessageReadReceiptAsync(Guid messageId, Guid userId)
        {
            return await _context.MessageReadReceipts
                .Include(mrr => mrr.Message)
                .Include(mrr => mrr.User)
                .FirstOrDefaultAsync(mrr => mrr.MessageId == messageId && mrr.UserId == userId);
        }

        public async Task<bool> HasUserReadMessageAsync(Guid messageId, Guid userId)
        {
            return await _context.MessageReadReceipts
                .AnyAsync(mrr => mrr.MessageId == messageId && mrr.UserId == userId);
        }

        public async Task<List<Message>> GetUnreadMessagesForUserAsync(Guid userId, Guid conversationId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId &&
                           m.UserId != userId &&
                           !m.ReadReceipts.Any(rr => rr.UserId == userId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task MarkMessageAsReadAsync(Guid messageId, Guid userId)
        {
            var existingReceipt = await GetUserMessageReadReceiptAsync(messageId, userId);
            
            if (existingReceipt == null)
            {
                var receipt = new MessageReadReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReadDate = DateTime.UtcNow
                };

                await _context.MessageReadReceipts.AddAsync(receipt);
            }
        }

        public async Task MarkConversationMessagesAsReadAsync(Guid conversationId, Guid userId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                           m.UserId != userId &&
                           !m.ReadReceipts.Any(rr => rr.UserId == userId))
                .ToListAsync();

            var receipts = unreadMessages.Select(m => new MessageReadReceipt
            {
                Id = Guid.NewGuid(),
                MessageId = m.Id,
                UserId = userId,
                ReadDate = DateTime.UtcNow
            }).ToList();

            await _context.MessageReadReceipts.AddRangeAsync(receipts);
        }
    }
} 