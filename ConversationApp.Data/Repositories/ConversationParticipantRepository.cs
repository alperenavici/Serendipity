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
    public class ConversationParticipantRepository : GenericRepository<ConversationParticipant>, IConversationParticipantRepository
    {
        public ConversationParticipantRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<ConversationParticipant>> GetUserParticipationsAsync(Guid userId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.Conversation)
                .Include(cp => cp.User)
                .Where(cp => cp.UserId == userId && !cp.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<ConversationParticipant>> GetConversationParticipantsAsync(Guid conversationId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .Where(cp => cp.ConversationId == conversationId && !cp.IsDeleted)
                .ToListAsync();
        }

        public async Task<ConversationParticipant> GetParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .Include(cp => cp.Conversation)
                .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && 
                                           cp.UserId == userId);
        }

        public async Task<bool> IsUserParticipantAsync(Guid conversationId, Guid userId)
        {
            return await _context.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && 
                               cp.UserId == userId && 
                               !cp.IsDeleted);
        }

        public async Task<List<ConversationParticipant>> GetActiveParticipantsAsync(Guid conversationId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .Where(cp => cp.ConversationId == conversationId && 
                            !cp.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<User>> GetOtherParticipantsAsync(Guid conversationId, Guid currentUserId)
        {
            return await _context.ConversationParticipants
                .Include(cp => cp.User)
                .Where(cp => cp.ConversationId == conversationId && 
                            cp.UserId != currentUserId && 
                            !cp.IsDeleted)
                .Select(cp => cp.User)
                .ToListAsync();
        }

        public async Task<int> GetParticipantCountAsync(Guid conversationId)
        {
            return await _context.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId && !cp.IsDeleted)
                .CountAsync();
        }
    }
} 