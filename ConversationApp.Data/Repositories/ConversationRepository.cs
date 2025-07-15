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
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(Guid userId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                .Where(cp => cp.UserId == userId && !cp.IsDeleted)
                .Select(cp => cp.Conversation)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetUserConversationsWithMessagesAsync(Guid userId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Messages.OrderByDescending(m => m.SentDate).Take(1))
                        .ThenInclude(m => m.Sender)
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Where(cp => cp.UserId == userId && !cp.IsDeleted)
                .Select(cp => cp.Conversation)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Conversation> GetConversationWithParticipantsAsync(Guid conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation> GetConversationWithMessagesAsync(Guid conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.SentDate))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<Conversation> GetPrivateConversationBetweenUsersAsync(Guid user1Id, Guid user2Id)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Participants)
                .Where(cp => cp.UserId == user1Id)
                .Select(cp => cp.Conversation)
                .Where(c => c.Type == 0 && 
                            c.Participants.Count == 2 &&
                            c.Participants.Any(p => p.UserId == user2Id))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Conversation>> SearchConversationsAsync(Guid userId, string searchTerm)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                    .ThenInclude(c => c.Participants)
                        .ThenInclude(p => p.User)
                .Where(cp => cp.UserId == userId && !cp.IsDeleted)
                .Where(cp => cp.Conversation.Title.Contains(searchTerm) ||
                             cp.Conversation.Participants.Any(p => p.User.UserName.Contains(searchTerm)))
                .Select(cp => cp.Conversation)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && 
                               cp.UserId == userId && 
                               !cp.IsDeleted);
        }
    }
} 