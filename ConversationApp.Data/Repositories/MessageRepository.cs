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
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Message>> GetConversationMessagesAsync(Guid conversationId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<List<Message>> GetConversationMessagesPagedAsync(Guid conversationId, int page, int pageSize)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<List<Message>> GetUserMessagesAsync(Guid userId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.Sender)
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<Message> GetLastMessageInConversationAsync(Guid conversationId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesAsync(Guid conversationId, Guid userId)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId && 
                           m.UserId != userId &&
                           !m.ReadReceipts.Any(rr => rr.UserId == userId))
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }

        public async Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && 
                           m.UserId != userId &&
                           !m.ReadReceipts.Any(rr => rr.UserId == userId))
                .CountAsync();
        }

        public async Task<List<Message>> SearchMessagesInConversationAsync(Guid conversationId, string searchTerm)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId && 
                           m.Content.Contains(searchTerm))
                .OrderBy(m => m.SentDate)
                .ToListAsync();
        }
    }
} 