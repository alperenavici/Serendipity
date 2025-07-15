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
    public class ScheduleMessageRepository : GenericRepository<ScheduleMessage>, IScheduleMessageRepository
    {
        public ScheduleMessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<ScheduleMessage>> GetUserScheduledMessagesAsync(Guid userId)
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.TargetUser)
                .Include(sm => sm.CreatedByAdmin)
                .Where(sm => sm.CreatedByUserId == userId)
                .OrderByDescending(sm => sm.CreationDate)
                .ToListAsync();
        }

        public async Task<List<ScheduleMessage>> GetScheduledMessagesForUserAsync(Guid targetUserId)
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.CreatedByAdmin)
                .Where(sm => sm.TargetUserId == targetUserId && sm.IsEnabled)
                .OrderBy(sm => sm.ScheduledSentTime)
                .ToListAsync();
        }

        public async Task<List<ScheduleMessage>> GetPendingScheduledMessagesAsync()
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.TargetUser)
                .Include(sm => sm.CreatedByAdmin)
                .Where(sm => sm.Status == 0 && sm.IsEnabled) // Status 0 = Pending
                .OrderBy(sm => sm.ScheduledSentTime)
                .ToListAsync();
        }

        public async Task<List<ScheduleMessage>> GetScheduledMessagesDueAsync(DateTime dateTime)
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.TargetUser)
                .Include(sm => sm.CreatedByAdmin)
                .Where(sm => sm.ScheduledSentTime <= dateTime && 
                            sm.Status == 0 && 
                            sm.IsEnabled)
                .ToListAsync();
        }

        public async Task<List<ScheduleMessage>> GetEnabledScheduledMessagesAsync()
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.TargetUser)
                .Include(sm => sm.CreatedByAdmin)
                .Where(sm => sm.IsEnabled)
                .OrderBy(sm => sm.ScheduledSentTime)
                .ToListAsync();
        }

        public async Task<ScheduleMessage> GetScheduledMessageByIdAsync(Guid scheduleMessageId)
        {
            return await _context.ScheduleMessages
                .Include(sm => sm.TargetUser)
                .Include(sm => sm.CreatedByAdmin)
                .FirstOrDefaultAsync(sm => sm.Id == scheduleMessageId);
        }

        public async Task UpdateScheduleMessageStatusAsync(Guid scheduleMessageId, int status)
        {
            var scheduleMessage = await _context.ScheduleMessages
                .FirstOrDefaultAsync(sm => sm.Id == scheduleMessageId);

            if (scheduleMessage != null)
            {
                scheduleMessage.Status = status;
                if (status == 1) // Sent status
                {
                    scheduleMessage.SentTime = DateTime.UtcNow;
                }
                _context.ScheduleMessages.Update(scheduleMessage);
            }
        }
    }
} 